// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.TokenManager;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 带应用键前缀的用户令牌缓存
/// </summary>
/// <remarks>
/// 包装原始的用户令牌缓存实现，为每个缓存键自动添加应用前缀。
/// 确保不同应用的用户令牌缓存互不干扰，实现缓存隔离。
/// </remarks>
internal class PrefixedUserTokenCache : IUserTokenCache
{
    private readonly IUserTokenCache _baseCache;
    private readonly string _prefix;

    public PrefixedUserTokenCache(IUserTokenCache baseCache, string prefix)
    {
        _baseCache = baseCache ?? throw new ArgumentNullException(nameof(baseCache));
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
    }

    private string PrefixKey(string key) => $"{_prefix}:{key}";

    public Task<UserTokenInfo?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return _baseCache.GetAsync(PrefixKey(key), cancellationToken);
    }

    public Task SetAsync(string key, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default)
    {
        return _baseCache.SetAsync(PrefixKey(key), tokenInfo, cancellationToken);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _baseCache.RemoveAsync(PrefixKey(key), cancellationToken);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return _baseCache.ExistsAsync(PrefixKey(key), cancellationToken);
    }

    public Task CleanExpiredAsync(CancellationToken cancellationToken = default)
    {
        return _baseCache.CleanExpiredAsync(cancellationToken);
    }

    public Task<(int Total, int Expired)> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _baseCache.GetStatisticsAsync(cancellationToken);
    }
}
