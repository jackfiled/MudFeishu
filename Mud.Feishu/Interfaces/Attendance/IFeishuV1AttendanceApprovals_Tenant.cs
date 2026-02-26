// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceApprovals;

namespace Mud.Feishu;

/// <summary>
/// 用于管理假勤审批的请假、加班、外出和出差四种审批数据。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_approval/query"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Header(Consts.Authorization)]
[Token(TokenType.TenantAccessToken)]
public interface IFeishuTenantV1AttendanceApprovals : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询考勤统计支持的日度统计或月度统计的统计表头。报表的表头信息可以在考勤统计-报表中查询到具体的报表信息，此接口专门用于查询表头数据。
    /// </summary>
    /// <param name="queryAttendanceApprovalsRequest">查询统计设置请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_approvals/query")]
    Task<FeishuApiResult<QueryAttendanceApprovalsResult>?> QueryApprovalsDataAsync(
      [Body] QueryAttendanceApprovalsRequest queryAttendanceApprovalsRequest,
      [Query("employee_type")] string employee_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
