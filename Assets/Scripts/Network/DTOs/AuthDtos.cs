using System;

namespace ChasingLight
{
    #region Requests

    [Serializable]
    public class SignInRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class SignUpRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class OAuthLoginRequest
    {
        public string provider = "google";
        public string idToken;
        public string nonce;
    }

    [Serializable]
    public class ForgotPasswordRequest
    {
        public string email;
    }

    [Serializable]
    public class VerifyRecoveryOtpRequest
    {
        public string email;
        public string otp;
    }

    [Serializable]
    public class ResetPasswordRequest
    {
        public string recoveryToken;
        public string newPassword;
    }

    [Serializable]
    public class RefreshTokenRequest
    {
        public string refreshToken;
    }

    #endregion

    #region Responses

    [Serializable]
    public class VerifyRecoveryOtpResponse
    {
        public string recoveryToken;
        public int expiresIn;
        public string email;
    }

    [Serializable]
    public class AuthResponse
    {
        // ── camelCase fields (backend ASP.NET Core default) ───────────────────
        public string accessToken;
        public string refreshToken;
        public int expiresIn;
        public string userId;
        public string email;
        public bool requiresEmailConfirmation;
    }

    #endregion
}
