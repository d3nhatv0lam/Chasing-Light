using System;

namespace ChasingLight.Network.DTOs
{
    /// <summary>
    /// Model lỗi trả về từ API backend.
    /// </summary>
    [Serializable]
    public class ApiErrorResponse
    {
        public string message;
        public string error;
        public int statusCode;
        public string errorCode;
    }

    /// <summary>
    /// DTO lỗi chuẩn tương thích với ErrorResponse của ASP.NET Core backend.
    /// </summary>
    [Serializable]
    public class ErrorResponse
    {
        public int statusCode;
        public string errorCode;
        public string message;
    }

    /// <summary>
    /// Kết quả trả về từ mọi lời gọi API. Dùng <see cref="IsSuccess"/> để kiểm tra.
    /// </summary>
    public class ApiResult<T>
    {
        public bool IsSuccess;
        public T Data;
        public string ErrorMessage;
        public int HttpStatusCode;

        public static ApiResult<T> Success(T data, int code = 200) =>
            new ApiResult<T> { IsSuccess = true, Data = data, HttpStatusCode = code };

        public static ApiResult<T> Failure(string message, int code = 0) =>
            new ApiResult<T> { IsSuccess = false, ErrorMessage = message, HttpStatusCode = code };
    }
}
