namespace CRM.API.Models.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public int ErrorCode { get; set; }

        /// <summary>系统错误编号，如 E-123；与 sys_error_log 对应。</summary>
        public string? ErrorId { get; set; }

        public static ApiResponse<T> Ok(T? data, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                ErrorCode = 0
            };
        }

        public static ApiResponse<T> Fail(string message, int errorCode = 1, string? errorId = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                ErrorId = errorId
            };
        }
    }
}
