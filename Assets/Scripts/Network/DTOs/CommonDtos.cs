using System;

namespace ChasingLight.Network.DTOs
{
    [Serializable]
    public class ErrorResponse
    {
        public int statusCode;
        public string errorCode;
        public string message;
    }
}
