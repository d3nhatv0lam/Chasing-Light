using System;

namespace ChasingLight.Network.DTOs
{
    #region Requests

    [Serializable]
    public class RegisterRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class LoginRequest
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
        public string accessToken;
        public string refreshToken;
        public int expiresIn;
        public string userId;
        public string email;
        public bool requiresEmailConfirmation;
    }

    #endregion
}
