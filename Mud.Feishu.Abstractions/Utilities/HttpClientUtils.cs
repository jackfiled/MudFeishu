// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Metrics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// 为HttpClient提供扩展方法的工具类
/// </summary>
internal static class HttpClientExtensions
{
    // 默认缓冲区大小（80KB）- 比默认的80K稍大，适合文件下载
    private const int DefaultBufferSize = 81920;

    /// <summary>
    /// 验证URL的有效性并检查HttpClient配置是否支持该URL
    /// </summary>
    /// <param name="url">要验证的URL，可以是绝对URL或相对URL</param>
    /// <param name="client">用于发送请求的HttpClient实例</param>
    /// <exception cref="ArgumentException">当URL格式无效时抛出</exception>
    /// <exception cref="InvalidOperationException">当使用相对URL但HttpClient未配置BaseAddress时抛出</exception>
    private static void ValidateUrl(string? url, HttpClient client)
    {
        if (url is null)
            throw new ArgumentNullException(nameof(url), "URL不能为空");

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException("URL不能为空", nameof(url));
        ExceptionUtils.ThrowIfNull(client, nameof(client));

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            // 验证绝对 URL 是否安全（SSRF 防护）
            UrlValidator.ValidateUrl(url, allowCustomBaseUrls: false);
            return;
        }

        if (Uri.IsWellFormedUriString(url, UriKind.Relative))
        {
            if (client.BaseAddress is null)
            {
                throw new InvalidOperationException(
                    "HttpClient未配置BaseAddress，无法使用相对URL");
            }
            // 验证 BaseAddress 是否安全
            UrlValidator.ValidateBaseUrl(client.BaseAddress?.ToString(), allowCustomBaseUrls: false);
            return;
        }

        throw new ArgumentException(
            $"URL格式不正确: '{url}'。必须是有效的绝对URL或相对URL。",
            nameof(url));
    }

    /// <summary>
    /// 发送HTTP请求并反序列化响应结果
    /// </summary>
    /// <typeparam name="TResult">响应结果的类型，必须是类类型并具有无参构造函数</typeparam>
    /// <param name="client">HTTP客户端实例</param>
    /// <param name="httpRequestMessage">HTTP请求消息</param>
    /// <param name="jsonSerializerOptions">JSON序列化选项，可选</param>
    /// <param name="logger">日志记录器，可选</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>反序列化后的响应结果，如果响应内容为空则返回默认值</returns>
    /// <exception cref="ArgumentNullException">当client或httpRequestMessage为null时抛出</exception>
    /// <exception cref="HttpRequestException">当HTTP请求失败时抛出</exception>
    public static async Task<TResult?> SendRequestAsync<TResult>(
        this HttpClient client,
        HttpRequestMessage httpRequestMessage,
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(client);
        ExceptionUtils.ThrowIfNull(httpRequestMessage);

        string? requestUri = httpRequestMessage.RequestUri?.ToString();

        ValidateUrl(requestUri, client);

        // 记录 HTTP 请求指标
        using var metrics = FeishuMetricsHelper.RecordHttpRequest(
            httpRequestMessage.Method.Method,
            requestUri ?? "unknown");

        try
        {
            using var response = await client.SendAsync(httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, logger, cancellationToken);

            // 记录请求成功
            FeishuMetricsHelper.RecordHttpRequestSuccess(
                httpRequestMessage.Method.Method,
                (int)response.StatusCode);

            // 直接检查Content-Length头，避免不必要的流操作
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength == 0)
            {
                logger?.LogDebug("响应内容为空，返回默认值: {Url}", requestUri);
                return default;
            }

            // 使用StreamReader包装流以提高性能
#if NETSTANDARD2_0
            using var stream = await response.Content.ReadAsStreamAsync();
#else
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#endif

            // 使用默认的序列化选项如果未提供
            var options = jsonSerializerOptions ?? GetDefaultJsonSerializerOptions();
            // 添加调试：记录原始响应内容
            string? rawResponse = null;
            if (logger?.IsEnabled(LogLevel.Debug) == true)
            {
                // 复制流以便可以重新读取
                var memoryStream = new MemoryStream();
#if NETSTANDARD2_0
                await stream.CopyToAsync(memoryStream);
#else
                await stream.CopyToAsync(memoryStream, cancellationToken);
#endif
                memoryStream.Position = 0;
#if NETSTANDARD2_0
                using var reader = new StreamReader(memoryStream, Encoding.UTF8);
#else
                using var reader = new StreamReader(memoryStream, Encoding.UTF8, leaveOpen: true);
#endif
                rawResponse = await reader.ReadToEndAsync();
                logger?.LogDebug("原始响应内容: {Url}\n{Response}", requestUri, rawResponse);

                // 重置流位置供反序列化使用
                memoryStream.Position = 0;

                try
                {
                    var result = await JsonSerializer.DeserializeAsync<TResult>(memoryStream, options, cancellationToken);
                    logger?.LogDebug("反序列化成功: {Url}, 类型: {Type}", requestUri, typeof(TResult).Name);
                    return result;
                }
                catch (JsonException jsonEx)
                {
                    logger?.LogError(jsonEx,
                        "JSON反序列化失败: {Url}\n" +
                        "期望类型: {ExpectedType}\n" +
                        "原始响应: {RawResponse}\n" +
                        "错误位置: {Path}",
                        requestUri, typeof(TResult).Name, rawResponse, jsonEx.Path);
                    throw new JsonException($"反序列化到类型 {typeof(TResult).Name} 失败: {jsonEx.Message}", jsonEx);
                }
            }
            else
            {
                try
                {
                    return await JsonSerializer.DeserializeAsync<TResult>(stream, options, cancellationToken);
                }
                catch (JsonException jsonEx)
                {
                    logger?.LogError(jsonEx,
                        "JSON反序列化失败: {Url}\n" +
                        "期望类型: {ExpectedType}\n" +
                        "原始响应: {RawResponse}\n" +
                        "错误位置: {Path}",
                        requestUri, typeof(TResult).Name, rawResponse, jsonEx.Path);
                    throw new JsonException($"反序列化到类型 {typeof(TResult).Name} 失败: {jsonEx.Message}", jsonEx);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            // 记录请求失败
#if NETSTANDARD2_0
            var statusCode = 0;
#else
            var statusCode = ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : 0;
#endif
            FeishuMetricsHelper.RecordHttpRequestFailure(
                httpRequestMessage.Method.Method,
                statusCode,
                ex.GetType().Name);

            logger?.LogError(ex, "HTTP请求处理异常: {Url}, StatusCode: {StatusCode}, InnerException: {InnerException}",
                requestUri, statusCode, ex.InnerException?.Message);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // 请求超时
            logger?.LogError(ex, "HTTP请求超时: {Url}, Timeout: {Timeout}秒",
                requestUri, client.Timeout.TotalSeconds);
            throw new HttpRequestException($"请求超时: {requestUri}", ex);
        }
        catch (TaskCanceledException ex)
        {
            // 请求被取消
            logger?.LogWarning(ex, "HTTP请求被取消: {Url}", requestUri);
            throw;
        }
        catch (System.Threading.ThreadAbortException ex)
        {
            // 线程被中止 - 通常是连接中断或服务器关闭
            logger?.LogError(ex, "HTTP请求连接中断: {Url}, 可能原因: 网络中断、服务器关闭连接或SSL握手失败",
                requestUri);
            throw new HttpRequestException($"请求连接中断: {requestUri}", ex);
        }
        catch (Exception ex) when (ex is not HttpRequestException && ex is not TaskCanceledException && ex is not System.Threading.ThreadAbortException)
        {
            // 其他异常
            FeishuMetricsHelper.RecordHttpRequestFailure(
                httpRequestMessage.Method.Method,
                0,
                ex.GetType().Name);

            logger?.LogError(ex, "HTTP请求处理异常: {Url}, ExceptionType: {ExceptionType}, Message: {Message}",
                requestUri, ex.GetType().Name, ex.Message);
            throw new HttpRequestException($"HTTP请求处理失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 下载文件内容并以字节数组形式返回
    /// </summary>
    /// <param name="client">HttpClient实例</param>
    /// <param name="httpRequestMessage">HTTP请求消息</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容的字节数组，如果请求失败则返回null</returns>
    /// <exception cref="ArgumentNullException">当client或httpRequestMessage为null时抛出</exception>
    /// <exception cref="HttpRequestException">当HTTP请求失败时抛出</exception>
    public static async Task<byte[]?> DownloadFileAsync(
       this HttpClient client,
       HttpRequestMessage httpRequestMessage,
       ILogger? logger = null,
       CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(client);
        ExceptionUtils.ThrowIfNull(httpRequestMessage);

        string? requestUri = httpRequestMessage.RequestUri?.ToString();
        ValidateUrl(requestUri, client);

        // 记录 HTTP 请求指标
        using var metrics = FeishuMetricsHelper.RecordHttpRequest(
            httpRequestMessage.Method.Method,
            requestUri ?? "unknown");

        try
        {
            using var response = await client.SendAsync(httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, logger, cancellationToken);

            // 记录请求成功
            FeishuMetricsHelper.RecordHttpRequestSuccess(
                httpRequestMessage.Method.Method,
                (int)response.StatusCode);

            // 检查Content-Length头，如果太大可以考虑使用流式处理
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength > 10 * 1024 * 1024) // 10MB警告
            {
                logger?.LogWarning("下载文件较大: {Url}, 大小: {Size}MB",
                    requestUri, contentLength / (1024.0 * 1024.0));
            }
#if NETSTANDARD2_0
            return await response.Content.ReadAsByteArrayAsync();
#else
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
#endif
        }
        catch (HttpRequestException ex)
        {
            // 记录请求失败
#if NETSTANDARD2_0
            var statusCode = 0;
#else
            var statusCode = ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : 0;
#endif
            FeishuMetricsHelper.RecordHttpRequestFailure(
                httpRequestMessage.Method.Method,
                statusCode,
                ex.GetType().Name);

            logger?.LogError(ex, "文件下载异常: {Url}", requestUri);
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException)
        {
            // 记录请求失败
            FeishuMetricsHelper.RecordHttpRequestFailure(
                httpRequestMessage.Method.Method,
                0,
                ex.GetType().Name);

            logger?.LogError(ex, "文件下载异常: {Url}", requestUri);
            throw new HttpRequestException($"文件下载失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 下载大文件并保存到指定路径
    /// </summary>
    /// <param name="client">HttpClient实例</param>
    /// <param name="httpRequestMessage">HTTP请求消息</param>
    /// <param name="filePath">保存文件的完整路径</param>
    /// <param name="bufferSize">缓冲区大小（字节），可选</param>
    /// <param name="overwrite">是否覆盖已存在的文件，默认为true</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>下载的文件信息</returns>
    /// <exception cref="ArgumentNullException">当client或httpRequestMessage为null时抛出</exception>
    /// <exception cref="ArgumentException">当filePath无效时抛出</exception>
    /// <exception cref="HttpRequestException">当HTTP请求失败时抛出</exception>
    /// <exception cref="IOException">当文件操作失败时抛出</exception>
    public static async Task<FileInfo> DownloadLargeFileAsync(
      this HttpClient client,
      HttpRequestMessage httpRequestMessage,
      string filePath,
      int bufferSize = DefaultBufferSize,
      bool overwrite = true,
      ILogger? logger = null,
      CancellationToken cancellationToken = default)
    {
        ExceptionUtils.ThrowIfNull(client);
        ExceptionUtils.ThrowIfNull(httpRequestMessage);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));

        if (bufferSize <= 0)
            throw new ArgumentException("缓冲区大小必须大于0", nameof(bufferSize));

        string? requestUri = httpRequestMessage.RequestUri?.ToString();
        string directoryPath = Path.GetDirectoryName(filePath)!;
        ValidateUrl(requestUri, client);

        // 记录 HTTP 请求指标
        using var metrics = FeishuMetricsHelper.RecordHttpRequest(
            httpRequestMessage.Method.Method,
            requestUri ?? "unknown");

        try
        {
            // 确保目录存在
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            // 检查文件是否已存在
            if (File.Exists(filePath))
            {
                if (overwrite)
                {
                    logger?.LogInformation("文件已存在，将被覆盖: {FilePath}", filePath);
                }
                else
                {
                    throw new IOException($"文件已存在: {filePath}");
                }
            }

            using var response = await client.SendAsync(httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            await EnsureSuccessStatusCodeAsync(response, logger, cancellationToken);

            // 记录请求成功
            FeishuMetricsHelper.RecordHttpRequestSuccess(
                httpRequestMessage.Method.Method,
                (int)response.StatusCode);

            // 获取文件大小信息
            var contentLength = response.Content.Headers.ContentLength;
            logger?.LogInformation("开始下载文件: {Url}, 大小: {Size}MB, 保存到: {FilePath}",
                requestUri,
                contentLength.HasValue ? contentLength.Value / (1024.0 * 1024.0) : "未知",
                filePath);


#if NETSTANDARD2_0
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: bufferSize,
                useAsync: true);
#else
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            // 使用FileStream异步模式，指定缓冲区大小
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: bufferSize,
                useAsync: true);
#endif
            // 使用CopyToAsync并指定缓冲区大小
            await contentStream.CopyToAsync(fileStream, bufferSize, cancellationToken);

            await fileStream.FlushAsync(cancellationToken);

            var fileInfo = new FileInfo(filePath);
            logger?.LogInformation("文件下载完成: {FilePath}, 大小: {Size}MB",
                filePath, fileInfo.Length / (1024.0 * 1024.0));

            return fileInfo;
        }
        catch (HttpRequestException ex)
        {
            // 记录请求失败
#if NETSTANDARD2_0
            var statusCode = 0;
#else
            var statusCode = ex.StatusCode.HasValue ? (int)ex.StatusCode.Value : 0;
#endif
            FeishuMetricsHelper.RecordHttpRequestFailure(
                httpRequestMessage.Method.Method,
                statusCode,
                ex.GetType().Name);

            // 清理部分下载的文件
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception cleanupEx)
            {
                logger?.LogWarning(cleanupEx, "清理部分下载的文件失败: {FilePath}", filePath);
            }

            logger?.LogError(ex, "大文件下载异常: {Url}, 文件路径: {FilePath}", requestUri, filePath);
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not ArgumentException)
        {
            // 记录请求失败
            FeishuMetricsHelper.RecordHttpRequestFailure(
                httpRequestMessage.Method.Method,
                0,
                ex.GetType().Name);

            // 清理部分下载的文件
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception cleanupEx)
            {
                logger?.LogWarning(cleanupEx, "清理部分下载的文件失败: {FilePath}", filePath);
            }

            logger?.LogError(ex, "大文件下载异常: {Url}, 文件路径: {FilePath}", requestUri, filePath);
            throw new HttpRequestException($"大文件下载失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 确保HTTP响应状态码表示成功，否则抛出异常
    /// </summary>
    private static async Task EnsureSuccessStatusCodeAsync(
        HttpResponseMessage response,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var statusCode = (int)response.StatusCode;
        string errorContent = string.Empty;

        try
        {
#if NETSTANDARD2_0
            errorContent = await response.Content.ReadAsStringAsync();
#else
            errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "读取错误响应内容失败");
            errorContent = "[无法读取错误内容]";
        }

        string errorMessage = $"HTTP请求失败: {statusCode} {response.StatusCode} - {errorContent}";

        // 对错误响应内容进行脱敏处理，防止敏感信息泄露
        var sanitizedContent = logger != null
            ? MessageSanitizer.Sanitize(errorContent, maxLength: 200)
            : "[日志未启用]";
        logger?.LogError("HTTP请求失败: {StatusCode}, 响应（已脱敏）: {Response}", statusCode, sanitizedContent);

        // 尝试释放响应内容
        response.Content.Dispose();
#if NETSTANDARD2_0
        throw new HttpRequestException(errorMessage, null);
#else
        throw new HttpRequestException(errorMessage, null, response.StatusCode);
#endif
    }

    /// <summary>
    /// 获取默认的JSON序列化选项
    /// </summary>
    internal static JsonSerializerOptions GetDefaultJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// 发送简单的GET请求并反序列化响应
    /// </summary>
    public static async Task<TResult?> GetAsync<TResult>(
        this HttpClient client,
        string requestUri,
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        return await client.SendRequestAsync<TResult>(request, jsonSerializerOptions, logger, cancellationToken);
    }

    /// <summary>
    /// 发送JSON POST请求并反序列化响应
    /// </summary>
    public static async Task<TResult?> PostAsJsonAsync<TRequest, TResult>(
        this HttpClient client,
        string requestUri,
        TRequest requestData,
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.Serialize(requestData, jsonSerializerOptions ?? GetDefaultJsonSerializerOptions());
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        return await client.SendRequestAsync<TResult>(request, jsonSerializerOptions, logger, cancellationToken);
    }

    /// <summary>
    /// 根据请求对象异步构建MultipartFormDataContent，支持文件路径属性自动添加文件内容
    /// </summary>
    public static async Task<MultipartFormDataContent> GetFormDataContentAsync(object requestBoey, CancellationToken cancellationToken = default)
    {
        var formData = new MultipartFormDataContent();
        var properties = requestBoey.GetType().GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(requestBoey);
            if (value == null) continue;

            var jsonPropertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
            var filePathAttr = property.GetCustomAttribute<FilePathAttribute>();
            var fieldName = jsonPropertyName ?? property.Name.ToLower();

            string stringValue = value switch
            {
                string s => s,
                int i => i.ToString(),
                long l => l.ToString(),
                double d => d.ToString(CultureInfo.InvariantCulture),
                decimal m => m.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString(CultureInfo.InvariantCulture),
                bool b => b.ToString().ToLower(),
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value.ToString() ?? string.Empty
            };

            if (filePathAttr != null)
            {
                if (!File.Exists(stringValue))
                    throw new FileNotFoundException($"文件未找到: {stringValue}");


#if NETSTANDARD2_0
                var fileBytes =  File.ReadAllBytes(stringValue);
#else
                // 异步读取文件
                var fileBytes = await File.ReadAllBytesAsync(stringValue, cancellationToken);
#endif
                var fileContent = new ByteArrayContent(fileBytes);
                var contentType = GetContentType(stringValue);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                formData.Add(fileContent, fieldName, Path.GetFileName(stringValue));
            }
            else
            {
                if (!string.IsNullOrEmpty(stringValue))
                    formData.Add(new StringContent(stringValue), fieldName);
            }
        }

        return formData;
    }

    private static readonly Dictionary<string, string> ContentTypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // 图片
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".webp"] = "image/webp",
        [".ico"] = "image/x-icon",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
        [".heic"] = "image/heic",
        [".svg"] = "image/svg+xml",

        // 文档
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".md"] = "text/markdown",

        // 音频/视频
        [".mp3"] = "audio/mpeg",
        [".mp4"] = "video/mp4",
        [".wav"] = "audio/wav",
        [".avi"] = "video/x-msvideo",
        [".mov"] = "video/quicktime",

        // 压缩文件
        [".zip"] = "application/zip",
        [".rar"] = "application/vnd.rar",
        [".7z"] = "application/x-7z-compressed",
        [".tar"] = "application/x-tar",
        [".gz"] = "application/gzip",

        // JSON/XML
        [".json"] = "application/json",
        [".xml"] = "application/xml",
    };

    // 根据文件扩展名获取对应的 Content-Type
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return ContentTypeMappings.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }
}