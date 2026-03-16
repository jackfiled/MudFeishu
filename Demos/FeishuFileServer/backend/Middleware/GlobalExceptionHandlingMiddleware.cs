using System.Net;
using System.Text.Json;

namespace FeishuFileServer.Middleware;

/// <summary>
/// 全局异常处理中间件
/// 捕获应用程序中所有未处理的异常，并返回统一的错误响应格式
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// 初始化全局异常处理中间件
    /// </summary>
    /// <param name="next">下一个中间件委托</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="environment">主机环境信息</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// 执行中间件逻辑
    /// 捕获请求处理过程中的所有异常，并统一处理
    /// </summary>
    /// <param name="context">HTTP上下文</param>
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

    /// <summary>
    /// 处理异常并返回统一的错误响应
    /// 根据异常类型设置相应的HTTP状态码和错误消息
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="exception">捕获的异常</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        /// <summary>
        /// 根据异常类型映射HTTP状态码和错误消息
        /// </summary>
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

        /// <summary>
        /// 开发环境下返回详细的错误信息，生产环境只返回基本错误信息
        /// </summary>
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

/// <summary>
/// 全局异常处理中间件扩展方法
/// 提供简洁的中间件注册方式
/// </summary>
public static class GlobalExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// 注册全局异常处理中间件
    /// </summary>
    /// <param name="builder">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
