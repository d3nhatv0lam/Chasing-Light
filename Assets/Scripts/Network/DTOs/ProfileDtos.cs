using System;

namespace ChasingLight
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
