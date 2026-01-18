// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceShifts;

namespace Mud.Feishu;

/// <summary>
/// 考勤班次是描述一次考勤任务时间规则的统称，比如一天打多少次卡，每次卡的上下班时间，晚到多长时间算迟到，晚到多长时间算缺卡等。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/shift/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Attendance")]
[Header(Consts.Authorization)]
public interface IFeishuTenantV1AttendanceShifts
{
    /// <summary>
    /// 创建考勤班次
    /// </summary>
    /// <param name="createAttendanceShiftsRequest">创建班次请求体</param>
    /// <param name="employee_type">请求体中的 user_ids 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/shifts")]
    Task<FeishuApiResult<CreateAttendanceShiftsResult>?> CreateShiftsAsync(
          [Body] CreateAttendanceShiftsRequest createAttendanceShiftsRequest,
          [Query("employee_type")] string employee_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>通过班次 ID 删除班次。对应功能为假勤设置-班次设置班次列表中操作栏的删除按钮。</para>
    /// </summary>
    /// <param name="shift_id">班次 ID，获取方式：1）按名称查询班次 2）创建班次 示例值："6919358778597097404"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/attendance/v1/shifts/{shift_id}")]
    Task<FeishuNullDataApiResult?> DeleteShiftsByIdAsync(
       [Path] string shift_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>通过班次 ID 获取班次详情。对应功能为假勤设置-班次设置班次列表中的具体班次，班次信息可以点击班次名称查看</para>
    /// </summary>
    /// <param name="shift_id">班次 ID，获取方式：1）按名称查询班次 2）创建班次 示例值："6919358778597097404"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/shifts/{shift_id}")]
    Task<FeishuApiResult<GetAttendanceShiftsResult>?> GetShiftsByIdAsync(
      [Path] string shift_id,
      CancellationToken cancellationToken = default);
}
