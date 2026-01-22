// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 应用-处理器注册表
/// </summary>
public class FeishuWebhookHandlerRegistry
{
    private readonly Dictionary<string, List<Type>> _registry = new();

    /// <summary>
    /// 注册处理器
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <param name="handlerType">处理器类型</param>
    public void Register(string appKey, Type handlerType)
    {
        if (string.IsNullOrEmpty(appKey))
            throw new ArgumentException("应用键不能为空", nameof(appKey));

        if (!_registry.ContainsKey(appKey))
        {
            _registry[appKey] = new List<Type>();
        }

        _registry[appKey].Add(handlerType);
    }

    /// <summary>
    /// 获取应用的所有处理器类型
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <returns>处理器类型列表</returns>
    public IReadOnlyList<Type> GetHandlers(string appKey)
    {
        if (_registry.TryGetValue(appKey, out var handlers))
        {
            return handlers.AsReadOnly();
        }
        return Array.Empty<Type>();
    }

    /// <summary>
    /// 获取所有已注册的应用键
    /// </summary>
    /// <returns>应用键列表</returns>
    public IReadOnlyList<string> GetAllAppKeys()
    {
        return _registry.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 检查应用是否已注册处理器
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <returns>是否已注册</returns>
    public bool HasHandlers(string appKey)
    {
        return _registry.ContainsKey(appKey) && _registry[appKey].Count > 0;
    }
}
