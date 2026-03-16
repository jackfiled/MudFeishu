// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用管理器接口
/// </summary>
/// <remarks>
/// 提供多个飞书应用的统一管理功能，包括：
/// - 获取应用上下文
/// - 检查应用是否存在
/// - 运行时添加/移除应用
/// - 遍历所有应用
/// 
/// 通过此接口可以方便地在多个飞书应用之间切换。
/// </remarks>
public interface IFeishuAppManager : IAppManager<IFeishuAppContext>
{
    /// <summary>
    /// 运行时添加应用
    /// </summary>
    /// <param name="config">应用配置</param>
    /// <returns>新创建的应用上下文</returns>
    /// <exception cref="InvalidOperationException">当应用已存在或配置无效时抛出</exception>
    /// <remarks>
    /// 在运行时动态添加一个新的飞书应用。
    /// 应用配置会自动验证，验证通过后会创建对应的应用上下文。
    /// 如果应用键已存在，会抛出异常。
    /// </remarks>
    IFeishuAppContext AddApp(FeishuAppConfig config);

}
