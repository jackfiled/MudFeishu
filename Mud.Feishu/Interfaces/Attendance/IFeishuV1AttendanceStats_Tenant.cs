// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceStats;

namespace Mud.Feishu;

/// <summary>
/// 考勤统计接口支持开发者定制接口返回数据，让开发者可以只获取自己所关注的数据内容。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_stats_data/attendance-statistic-reference"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Header(Consts.Authorization)]
[Token(TokenType.TenantAccessToken)]
public interface IFeishuTenantV1AttendanceStats : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 更新开发者定制的日度统计或月度统计的统计报表表头设置信息。
    /// </summary>
    /// <param name="userStatsViewsRequest">更新统计设置请求体</param>
    /// <param name="user_stats_view_id">用户视图 ID,示例值："TmpZNU5qTTJORFF6T1RnNU5UTTNOakV6TWl0dGIyNTBhQT09"</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("/open-apis/attendance/v1/user_stats_views/{user_stats_view_id}")]
    Task<FeishuApiResult<UserStatsViewsResult>?> UpdateStatsSettingsAsync(
        [Body] UserStatsViewsRequest userStatsViewsRequest,
        [Path] string user_stats_view_id,
        [Query("employee_type")] string employee_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
