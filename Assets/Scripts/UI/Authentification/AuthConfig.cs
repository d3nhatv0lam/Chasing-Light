using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Cấu hình URL và endpoint của Supabase REST API.
    /// Thay đổi <see cref="BaseUrl"/> và <see cref="ApiKey"/> trong Inspector
    /// hoặc trong file này trước khi build.
    /// </summary>
    [CreateAssetMenu(fileName = "AuthConfig", menuName = "ChasingLight/Auth Config")]
    public class AuthConfig : ScriptableObject
    {
        [Header("Supabase REST API")]
        [Tooltip("URL gốc của backend, ví dụ: http://localhost:5013")]
        public string BaseUrl = "http://localhost:5013";

        [Tooltip("Supabase anon key (hoặc service key nếu dùng service)")]
        public string ApiKey = "";

        [Header("Endpoint Paths")]
        public string SignInPath      = "/api/v1/auth/login";
        public string SignUpPath      = "/api/v1/auth/register";
        public string ResetPassPath   = "/api/v1/auth/reset-password";
        public string GoogleOAuthPath = "/api/v1/auth/oauth";

        // ───── Derived helpers ─────
        public string SignInUrl      => BaseUrl.TrimEnd('/') + SignInPath;
        public string SignUpUrl      => BaseUrl.TrimEnd('/') + SignUpPath;
        public string ResetPassUrl   => BaseUrl.TrimEnd('/') + ResetPassPath;
        public string GoogleOAuthUrl => BaseUrl.TrimEnd('/') + GoogleOAuthPath;

        [Header("Google OAuth – PC / Editor (Local Browser Flow)")]
        [Tooltip("OAuth 2.0 Client ID loại 'Web application' từ Google Cloud Console.")]
        public string GoogleClientId = "";

        [Tooltip("Port HTTP cục bộ để bắt callback từ trình duyệt (phải khớp với Authorized redirect URI trên Google Console: http://localhost:{port}/callback/).")]
        public int LocalCallbackPort = 8580;

        [Tooltip("Thời gian tối đa (giây) chờ người dùng hoàn tất đăng nhập trên trình duyệt.")]
        public int OAuthTimeoutSeconds = 120;

        [Header("PlayerPrefs Keys")]
        public string PrefKeyJwt      = "auth_jwt";
        public string PrefKeyEmail    = "auth_email";
        public string PrefKeyRefresh  = "auth_refresh";

        [Header("Request Settings")]
        [Tooltip("Timeout cho mỗi HTTP request (giây)")]
        public int TimeoutSeconds = 15;
    }
}
