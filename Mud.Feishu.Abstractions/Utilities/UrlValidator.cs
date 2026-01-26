// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Sockets;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// URL 验证工具类，用于防止 SSRF（服务端请求伪造）攻击
/// </summary>
internal static class UrlValidator
{
    /// <summary>
    /// 飞书官方域名白名单
    /// </summary>
    private static readonly HashSet<string> FeishuDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "open.feishu.cn",
        "open.larksuite.com",
        "larksuite.com",
        "feishu.cn"
    };

    /// <summary>
    /// 私有 IP 地址范围（CIDR 表示法）
    /// </summary>
    private static readonly HashSet<string> PrivateIpRanges = new()
    {
        "10.0.0.0/8",         // 10.0.0.0 - 10.255.255.255
        "172.16.0.0/12",      // 172.16.0.0 - 172.31.255.255
        "192.168.0.0/16",     // 192.168.0.0 - 192.168.255.255
        "127.0.0.0/8",        // 127.0.0.0 - 127.255.255.255 (本地回环)
        "169.254.0.0/16",     // 169.254.0.0 - 169.254.255.255 (链路本地)
        "0.0.0.0/8",          // 0.0.0.0 - 0.255.255.255 (当前网络)
        "::1/128",            // IPv6 本地回环
        "fc00::/7",           // IPv6 私有地址
        "fe80::/10"           // IPv6 链路本地
    };

    /// <summary>
    /// 验证 URL 是否为飞书官方域名且不包含私有 IP 地址
    /// </summary>
    /// <param name="url">要验证的 URL</param>
    /// <param name="allowCustomBaseUrls">是否允许自定义基础 URL（默认为 false）</param>
    /// <exception cref="InvalidOperationException">当 URL 不在白名单或包含私有 IP 时抛出</exception>
    public static void ValidateUrl(string? url, bool allowCustomBaseUrls = false)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url), "URL 不能为空");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"URL 格式无效: {url}", nameof(url));

        // 检查协议是否为 HTTPS
        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"仅允许 HTTPS 协议，当前 URL: {uri.Scheme}://{uri.Host}");

        // 检查域名是否在飞书官方白名单中
        if (!FeishuDomains.Contains(uri.Host))
        {
            if (!allowCustomBaseUrls)
            {
                throw new InvalidOperationException(
                    $"域名 '{uri.Host}' 不在飞书官方白名单中。允许的域名: {string.Join(", ", FeishuDomains)}。" +
                    "如需使用自定义域名，请设置 allowCustomBaseUrls=true（注意安全风险）。");
            }
            else
            {
                // 即使允许自定义域名，也必须检查是否为私有 IP
                if (IsPrivateIpAddress(uri.Host))
                {
                    throw new InvalidOperationException($"不允许访问私有 IP 地址: {uri.Host}");
                }
            }
        }
    }

    /// <summary>
    /// 验证基础 URL 配置
    /// </summary>
    /// <param name="baseUrl">基础 URL</param>
    /// <param name="allowCustomBaseUrls">是否允许自定义基础 URL</param>
    /// <exception cref="InvalidOperationException">当基础 URL 不安全时抛出</exception>
    public static void ValidateBaseUrl(string? baseUrl, bool allowCustomBaseUrls = false)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return; // 使用默认值

        ValidateUrl(baseUrl, allowCustomBaseUrls);
    }

    /// <summary>
    /// 检查主机名是否为私有 IP 地址
    /// </summary>
    /// <param name="host">主机名或 IP 地址</param>
    /// <returns>如果是私有 IP 地址返回 true</returns>
    private static bool IsPrivateIpAddress(string host)
    {
        // 尝试解析为 IP 地址
        if (IPAddress.TryParse(host, out var ipAddress))
        {
            return IsPrivateIpAddress(ipAddress);
        }

        // 如果不是 IP 地址，尝试 DNS 解析并检查所有 IP
        try
        {
            var hostEntries = Dns.GetHostAddresses(host);
            return hostEntries.Any(IsPrivateIpAddress);
        }
        catch
        {
            // DNS 解析失败时，默认认为不是私有 IP（由连接超时等机制处理）
            return false;
        }
    }

    /// <summary>
    /// 检查 IP 地址是否为私有地址
    /// </summary>
    /// <param name="ipAddress">IP 地址</param>
    /// <returns>如果是私有 IP 地址返回 true</returns>
    private static bool IsPrivateIpAddress(IPAddress ipAddress)
    {
        if (ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal)
            return true;

        if (IPAddress.IsLoopback(ipAddress))
            return true;

        var addressBytes = ipAddress.GetAddressBytes();
        if (addressBytes.Length != 4)
            return false;

        // 检查 IPv4 私有地址范围
        var ip = BitConverter.ToUInt32(addressBytes.Reverse().ToArray(), 0);

        // 10.0.0.0 - 10.255.255.255 (10.0.0.0/8)
        if ((ip & 0xFF000000) == 0x0A000000)
            return true;

        // 172.16.0.0 - 172.31.255.255 (172.16.0.0/12)
        if ((ip & 0xFFF00000) == 0xAC100000)
            return true;

        // 192.168.0.0 - 192.168.255.255 (192.168.0.0/16)
        if ((ip & 0xFFFF0000) == 0xC0A80000)
            return true;

        // 127.0.0.0 - 127.255.255.255 (127.0.0.0/8)
        if ((ip & 0xFF000000) == 0x7F000000)
            return true;

        // 169.254.0.0 - 169.254.255.255 (169.254.0.0/16)
        if ((ip & 0xFFFF0000) == 0xA9FE0000)
            return true;

        return false;
    }

    /// <summary>
    /// 获取飞书官方域名白名单
    /// </summary>
    /// <returns>域名集合</returns>
    public static IReadOnlyCollection<string> GetAllowedDomains()
    {
        return FeishuDomains.ToArray();
    }
}
