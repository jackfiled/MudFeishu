// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 日志脱敏工具类
/// 用于自动脱敏敏感字段，防止敏感信息泄露到日志
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// 需要脱敏的敏感字段列表
    /// </summary>
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "encrypt",
        "signature",
        "token",
        "password",
        "secret",
        "key",
        "credential",
        "auth"
    };

    /// <summary>
    /// 脱敏标记
    /// </summary>
    private const string SanitizedMarker = "***";

    /// <summary>
    /// 脱敏日志消息，移除敏感信息
    /// </summary>
    /// <param name="logMessage">原始日志消息</param>
    /// <returns>脱敏后的日志消息</returns>
    public static string Sanitize(string? logMessage)
    {
        if (string.IsNullOrEmpty(logMessage))
            return string.Empty;

        var sanitized = logMessage!;

        // 遍历所有敏感字段进行脱敏
        foreach (var field in SensitiveFields)
        {
            // 匹配 JSON 格式: "field":"value"
            var jsonPattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                jsonPattern,
                $"\"{field}\":\"{SanitizedMarker}\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 匹配 URL 参数格式: field=value
            var urlPattern = $"(?:^|&){field}=[^&]*";
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                urlPattern,
                $"{field}={SanitizedMarker}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    /// <summary>
    /// 脱敏日志消息（仅限 JSON 格式）
    /// </summary>
    /// <param name="jsonMessage">JSON 格式的日志消息</param>
    /// <returns>脱敏后的 JSON 日志消息</returns>
    public static string SanitizeJson(string? jsonMessage)
    {
        if (string.IsNullOrEmpty(jsonMessage))
            return string.Empty;

        var sanitized = jsonMessage!;

        // 遍历所有敏感字段进行脱敏
        foreach (var field in SensitiveFields)
        {
            var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]*\"";
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                pattern,
                $"\"{field}\":\"{SanitizedMarker}\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return sanitized;
    }

    /// <summary>
    /// 添加自定义敏感字段
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    public static void AddSensitiveField(string fieldName)
    {
        if (!string.IsNullOrEmpty(fieldName))
        {
            SensitiveFields.Add(fieldName);
        }
    }

    /// <summary>
    /// 批量添加自定义敏感字段
    /// </summary>
    /// <param name="fieldNames">字段名称集合</param>
    public static void AddSensitiveFields(IEnumerable<string> fieldNames)
    {
        foreach (var field in fieldNames)
        {
            AddSensitiveField(field);
        }
    }

    /// <summary>
    /// 移除敏感字段
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    /// <returns>是否成功移除</returns>
    public static bool RemoveSensitiveField(string fieldName)
    {
        return SensitiveFields.Remove(fieldName);
    }

    /// <summary>
    /// 获取所有敏感字段列表
    /// </summary>
    /// <returns>敏感字段列表</returns>
    public static IEnumerable<string> GetSensitiveFields()
    {
        return SensitiveFields;
    }
}
