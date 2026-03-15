using System.Net;
using System.Text.Json;

namespace FeishuFileServer.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理的异常发生: {ExceptionType} - {Message}\n堆栈跟踪: {StackTrace}",
                ex.GetType().Name,
                ex.Message,
                ex.StackTrace);

            if (ex.InnerException != null)
            {
                _logger.LogError("内部异常: {InnerExceptionType} - {InnerMessage}\n内部堆栈: {InnerStackTrace}",
                    ex.InnerException.GetType().Name,
                    ex.InnerException.Message,
                    ex.InnerException.StackTrace);
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "未授权访问"),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            NotImplementedException => (HttpStatusCode.NotImplemented, "功能尚未实现"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "请求超时"),
            _ => (HttpStatusCode.InternalServerError, "服务器内部错误")
        };

        context.Response.StatusCode = (int)statusCode;

        object response;

        if (_environment.IsDevelopment())
        {
            response = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = message,
                detail = exception.Message,
                exceptionType = exception.GetType().Name,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message,
                innerExceptionType = exception.InnerException?.GetType().Name,
                innerStackTrace = exception.InnerException?.StackTrace,
                path = context.Request.Path.Value,
                method = context.Request.Method,
                timestamp = DateTime.UtcNow
            };
        }
        else
        {
            response = new
            {
                success = false,
                statusCode = (int)statusCode,
                message = message
            };
        }

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });
        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
