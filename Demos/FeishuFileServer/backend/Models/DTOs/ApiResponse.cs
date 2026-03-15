namespace FeishuFileServer.Models.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int Code { get; set; }

    public static ApiResponse<T> Ok(T? data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Code = 200
        };
    }

    public static ApiResponse<T> Fail(string message, int code = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Code = code
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Code = 200
        };
    }

    public new static ApiResponse Fail(string message, int code = 400)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Code = code
        };
    }
}
