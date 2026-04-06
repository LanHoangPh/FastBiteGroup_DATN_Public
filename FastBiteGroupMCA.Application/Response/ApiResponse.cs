using System.Text.Json.Serialization;

namespace FastBiteGroupMCA.Application.Response;

public class ApiResponse<T> : IApiResponse
{
    /// <summary>
    /// cho biết request có thành công hay không.
    /// </summary>

    public bool Success { get; set; } = true;

    /// <summary>
    /// Một thông điệp tóm tắt chung cho response (tùy chọn).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    /// <summary>
    /// Dữ liệu trả về (nếu thành công).
    /// </summary>
    /// 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    /// <summary>
    /// Danh sách các lỗi chi tiết (nếu thất bại).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ApiError>? Errors { get; set; }

    public int StatusCode { get; set; }


    /// <summary>
    /// Tạo một response thành công.
    /// </summary>
    public static ApiResponse<T> Ok(T? data, string? message = "Thành công")
    {
        return new() { Success = true, Data = data, Message = message };
    }
    //public static ApiResponse<T> Ok(string? message)
    //{
    //    return new() { Success = true, Message = message, Data = default };
    //}

    /// <summary>
    /// Tạo một response thất bại với một lỗi duy nhất.
    /// </summary>
    public static ApiResponse<T> Fail(string errorCode, string errorMessage, int statusCode = 400)
    {
        return new()
        {
            Success = false,
            Errors = new List<ApiError> { new ApiError(errorCode, errorMessage) },
            Message = "Đã có lỗi xảy ra.",
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Tạo một response thất bại với nhiều lỗi (dùng cho validation).
    /// </summary>
    public static ApiResponse<T> Fail(List<ApiError> errors)
    {
        return new()
        {
            Success = false,
            Errors = errors,
            Message = "Dữ liệu đầu vào không hợp lệ."
        };
    }
    public static ApiResponse<T> Created(T? data = default, string? message = "Tạo mới thành công.")
    {
        return new()
        {
            Success = true,
            Data = data,
            Message = message
        };
    }
}
public class ApiError
{
    /// <summary>
    /// Một mã lỗi duy nhất, ổn định để client có thể xử lý logic.
    /// Ví dụ: "USER_NOT_FOUND", "GroupName" (tên trường bị lỗi validation)
    /// </summary>
    public string ErrorCode { get; set; }
    public string Message { get; set; }

    public ApiError(string errorCode, string message)
    {
        ErrorCode = errorCode;
        Message = message;
    }
}