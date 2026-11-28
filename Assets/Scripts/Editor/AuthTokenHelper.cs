using UnityEditor;
using UnityEngine;

namespace ChasingLight
{
    public static class AuthTokenHelper
    {
        [MenuItem("Tools/Auth/In AccessToken ra Console")]
        public static void PrintToken()
        {
            string token = PlayerPrefs.GetString("auth_jwt", "");
            string email = PlayerPrefs.GetString("auth_email", "");
            string refresh = PlayerPrefs.GetString("auth_refresh", "");

            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[AuthHelper] Không tìm thấy Token nào trong PlayerPrefs!");
            }
            else
            {
                Debug.Log($"[AuthHelper] === THÔNG TIN ĐĂNG NHẬP ===\nEmail: {email}\nAccessToken:\n{token}");
                // Copy tự động vào Clipboard máy tính để dán luôn
                EditorGUIUtility.systemCopyBuffer = token;
                Debug.Log(">> ĐÃ TỰ ĐỘNG COPY TOKEN VÀO CLIPBOARD (BỘ NHỚ TẠM)! Bạn chỉ cần sang Scalar và bấm Ctrl+V dán vào.");
            }
        }

        [MenuItem("Tools/Auth/Đăng xuất (Xoá Session)")]
        public static void ClearSession()
        {
            PlayerPrefs.DeleteKey("auth_jwt");
            PlayerPrefs.DeleteKey("auth_email");
            PlayerPrefs.DeleteKey("auth_user_id");
            PlayerPrefs.DeleteKey("auth_refresh");
            PlayerPrefs.Save();
            Debug.Log(">> [AuthHelper] Đã xoá toàn bộ Session đã lưu! Khi Play game sẽ hiện lại màn hình Đăng nhập.");
        }
    }
}
