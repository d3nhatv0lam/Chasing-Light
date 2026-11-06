using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Thực hiện Google OAuth 2.0 Implicit Flow cho Unity PC / Editor.<br/>
    /// Luồng hoạt động:
    /// <list type="number">
    ///   <item>Khởi tạo <see cref="HttpListener"/> tại <c>http://localhost:{port}/callback/</c>.</item>
    ///   <item>Mở trình duyệt hệ thống với Google Authorization URL.</item>
    ///   <item>Google redirect về <c>http://localhost:{port}/callback/#id_token=...</c>.</item>
    ///   <item>Trình duyệt nhận trang HTML trung gian, trang đó dùng JS bóc fragment và
    ///         gửi <c>id_token</c> + <c>nonce</c> trở lại listener qua query string.</item>
    ///   <item>Listener callback được gọi trên Unity main thread.</item>
    /// </list>
    /// </summary>
    public sealed class GoogleOAuthLocalListener : IDisposable
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Config
        // ─────────────────────────────────────────────────────────────────────

        private const string GoogleAuthEndpoint =
            "https://accounts.google.com/o/oauth2/v2/auth";

        private const string OAuthScopes =
            "openid%20email%20profile";

        /// <summary>Trang HTML trả về cho browser để bóc URL fragment và forward id_token.</summary>
        private const string CallbackPageHtml = @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8""/>
  <title>Chasing Light – Đang xác thực...</title>
  <style>
    body { font-family: sans-serif; display:flex; align-items:center;
           justify-content:center; height:100vh; margin:0; background:#111; color:#eee; }
    .box { text-align:center; }
    .spinner { width:40px; height:40px; border:4px solid #444;
               border-top-color:#4fc3f7; border-radius:50%;
               animation:spin 0.8s linear infinite; margin:0 auto 16px; }
    @keyframes spin { to { transform:rotate(360deg); } }
  </style>
</head>
<body>
  <div class=""box"">
    <div class=""spinner""></div>
    <p>Đang hoàn tất đăng nhập, vui lòng đợi...</p>
  </div>
  <script>
    (function() {
      var fragment = window.location.hash.substring(1);
      var params   = {};
      fragment.split('&').forEach(function(p) {
        var kv = p.split('=');
        if (kv.length === 2) params[kv[0]] = kv[1];
      });

      var idToken = params['id_token']  || '';
      var error   = params['error']     || '';
      var state   = params['state']     || '';

      var query = '?id_token=' + encodeURIComponent(idToken)
                + '&error='   + encodeURIComponent(error)
                + '&state='   + encodeURIComponent(state);

      // Gửi token về listener qua sub-path /token
      fetch('/callback/token' + query)
        .then(function(r) { return r.text(); })
        .then(function(msg) {
          document.querySelector('p').textContent = msg;
        })
        .catch(function(e) {
          document.querySelector('p').textContent = 'Lỗi kết nối: ' + e;
        });
    })();
  </script>
</body>
</html>";

        private const string SuccessPage =
            "Đăng nhập thành công! Bạn có thể đóng tab này và quay lại game.";

        private const string ErrorPage =
            "Đăng nhập thất bại. Vui lòng quay lại game và thử lại.";

        // ─────────────────────────────────────────────────────────────────────
        //  State
        // ─────────────────────────────────────────────────────────────────────

        private readonly string   _clientId;
        private readonly int      _port;
        private readonly string   _nonce;
        private readonly string   _state;

        private HttpListener      _listener;
        private CancellationTokenSource _cts;

        /// <summary>Nonce hiện tại (gửi lên backend cùng id_token).</summary>
        public string Nonce => _nonce;

        // ─────────────────────────────────────────────────────────────────────
        //  Constructor
        // ─────────────────────────────────────────────────────────────────────

        public GoogleOAuthLocalListener(string googleClientId, int localPort = 8580)
        {
            if (string.IsNullOrWhiteSpace(googleClientId))
                throw new ArgumentException("Google Client ID không được để trống.", nameof(googleClientId));

            _clientId = googleClientId;
            _port     = localPort;
            _nonce    = GenerateSecureRandom(16);
            _state    = GenerateSecureRandom(16);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mở trình duyệt Google và đợi callback.<br/>
        /// Kết quả trả qua <paramref name="onComplete"/> trên luồng gọi (thường là ThreadPool);<br/>
        /// cần marshal về Unity main thread nếu cập nhật UI.
        /// </summary>
        /// <param name="onComplete">Callback: (idToken, error). Một trong hai là null.</param>
        /// <param name="timeoutSeconds">Hết thời gian nếu user không hoàn tất đăng nhập.</param>
        public void StartAndOpenBrowser(Action<string, string> onComplete, int timeoutSeconds = 120)
        {
            if (_listener != null)
            {
                Debug.LogWarning("[GoogleOAuthLocalListener] Listener đang chạy, bỏ qua lần gọi thứ hai.");
                return;
            }

            _cts = new CancellationTokenSource();

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_port}/callback/");
                _listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GoogleOAuthLocalListener] Không thể khởi động HttpListener tại port {_port}: {ex.Message}");
                onComplete?.Invoke(null, $"Không thể mở local server (port {_port} có thể đang bị dùng).");
                return;
            }

            string authUrl = BuildGoogleAuthUrl();
            Debug.Log($"[GoogleOAuthLocalListener] Mở browser: {authUrl}");
            Application.OpenURL(authUrl);

            // Chạy listener trên background thread
            Task.Run(() => ListenAsync(onComplete, timeoutSeconds, _cts.Token));
        }

        /// <summary>Dừng listener sớm (ví dụ khi panel bị đóng).</summary>
        public void Cancel()
        {
            _cts?.Cancel();
            StopListener();
        }

        public void Dispose() => Cancel();

        // ─────────────────────────────────────────────────────────────────────
        //  Internal
        // ─────────────────────────────────────────────────────────────────────

        private string BuildGoogleAuthUrl()
        {
            string redirectUri = $"http://localhost:{_port}/callback/";

            return $"{GoogleAuthEndpoint}" +
                   $"?client_id={Uri.EscapeDataString(_clientId)}" +
                   $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                   $"&response_type=id_token" +
                   $"&scope={OAuthScopes}" +
                   $"&nonce={Uri.EscapeDataString(HashNonce(_nonce))}" +
                   $"&state={Uri.EscapeDataString(_state)}" +
                   $"&prompt=select_account";
        }

        private async Task ListenAsync(
            Action<string, string> onComplete,
            int timeoutSeconds,
            CancellationToken token)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linked     = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);

            try
            {
                // Giữ MỘT task GetContextAsync duy nhất đang chờ; tạo task mới mỗi vòng
                // sẽ làm request bị "nuốt" bởi task cũ bị bỏ rơi.
                var cancelTcs = new TaskCompletionSource<bool>();
                using var reg = linked.Token.Register(() => cancelTcs.TrySetResult(true));

                while (!linked.Token.IsCancellationRequested)
                {
                    var contextTask = _listener.GetContextAsync();

                    var completed = await Task.WhenAny(contextTask, cancelTcs.Task);

                    if (completed != contextTask || linked.Token.IsCancellationRequested)
                        break;

                    HttpListenerContext ctx;
                    try { ctx = await contextTask; }
                    catch { break; }

                    string path = ctx.Request.Url?.AbsolutePath ?? "";

                    if (path == "/callback/" || path == "/callback")
                    {
                        // Bước 1: Trả trang HTML bóc fragment
                        await RespondHtmlAsync(ctx, CallbackPageHtml);
                    }
                    else if (path == "/callback/token")
                    {
                        // Bước 2: Nhận id_token từ JS
                        string idToken = ctx.Request.QueryString["id_token"] ?? "";
                        string error   = ctx.Request.QueryString["error"]    ?? "";
                        string state   = ctx.Request.QueryString["state"]    ?? "";

                        // Kiểm tra state để chống CSRF
                        bool stateOk = state == _state;

                        if (!stateOk)
                        {
                            await RespondTextAsync(ctx, ErrorPage);
                            StopListener();
                            onComplete?.Invoke(null, "State mismatch – có thể bị tấn công CSRF.");
                            return;
                        }

                        if (!string.IsNullOrEmpty(error))
                        {
                            await RespondTextAsync(ctx, ErrorPage);
                            StopListener();
                            onComplete?.Invoke(null, $"Google trả về lỗi: {Uri.UnescapeDataString(error)}");
                            return;
                        }

                        if (string.IsNullOrEmpty(idToken))
                        {
                            await RespondTextAsync(ctx, ErrorPage);
                            StopListener();
                            onComplete?.Invoke(null, "Không nhận được id_token từ Google.");
                            return;
                        }

                        await RespondTextAsync(ctx, SuccessPage);
                        StopListener();
                        onComplete?.Invoke(Uri.UnescapeDataString(idToken), null);
                        return;
                    }
                    else
                    {
                        // Path không rõ – trả 404
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                    }
                }

                // Hết timeout hoặc bị cancel
                StopListener();
                if (timeoutCts.Token.IsCancellationRequested)
                    onComplete?.Invoke(null, $"Hết thời gian chờ ({timeoutSeconds}s). Vui lòng thử lại.");
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Debug.LogError($"[GoogleOAuthLocalListener] Lỗi nội bộ: {ex}");
                StopListener();
                onComplete?.Invoke(null, "Lỗi nội bộ khi xử lý OAuth callback.");
            }
        }

        private static async Task RespondHtmlAsync(HttpListenerContext ctx, string html)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode  = 200;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private static async Task RespondTextAsync(HttpListenerContext ctx, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            ctx.Response.StatusCode  = 200;
            ctx.Response.ContentType = "text/plain; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }

        private void StopListener()
        {
            try { _listener?.Stop(); }  catch { /* ignore */ }
            try { _listener?.Close(); } catch { /* ignore */ }
            _listener = null;
        }

        /// <summary>SHA-256 hex (chữ thường) của raw nonce – gửi cho Google; raw nonce gửi cho backend/Supabase.</summary>
        private static string HashNonce(string raw)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>Tạo chuỗi ngẫu nhiên an toàn dùng làm nonce / state.</summary>
        private static string GenerateSecureRandom(int byteLength)
        {
            var bytes = new byte[byteLength];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                          .TrimEnd('=')
                          .Replace('+', '-')
                          .Replace('/', '_');
        }
    }
}

