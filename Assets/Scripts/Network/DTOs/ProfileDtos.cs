using System;

namespace ChasingLight.Network.DTOs
{
    [Serializable]
    public class UserProfileResponse
    {
        public string id;
        public string displayName;
        public string avatarUrl;
        public int totalStars;
        public string createdAt;
    }
}
