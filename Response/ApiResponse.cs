namespace auth_service.Response;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public int StatusCode { get; set; }

    public static ApiResponse<T> SuccessResponse(
        T? data,
        string message = "Success",
        int statusCode = 200
    ) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode,
        };

    public static ApiResponse<T> ErrorResponse(
        string message,
        int statusCode = 400,
        List<string>? errors = null
    ) =>
        new()
        {
            Success = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode,
        };
}
