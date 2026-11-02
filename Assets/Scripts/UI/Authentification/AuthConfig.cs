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
        public string SignInPath   = "/api/v1/auth/sign-in";
        public string SignUpPath   = "/api/v1/auth/sign-up";
        public string ResetPassPath = "/api/v1/auth/reset-password";

        // ───── Derived helpers ─────
        public string SignInUrl    => BaseUrl.TrimEnd('/') + SignInPath;
        public string SignUpUrl    => BaseUrl.TrimEnd('/') + SignUpPath;
        public string ResetPassUrl => BaseUrl.TrimEnd('/') + ResetPassPath;

        [Header("PlayerPrefs Keys")]
        public string PrefKeyJwt      = "auth_jwt";
        public string PrefKeyEmail    = "auth_email";
        public string PrefKeyRefresh  = "auth_refresh";

        [Header("Request Settings")]
        [Tooltip("Timeout cho mỗi HTTP request (giây)")]
        public int TimeoutSeconds = 15;
    }
}
