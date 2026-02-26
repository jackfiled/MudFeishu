// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;

/// <summary>
/// <para>加班信息</para>
/// </summary>
public class UserAttendanceOvertimeWork
{
    /// <summary>
    /// <para>审批实例 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6737202939523236113</para>
    /// </summary>
    [JsonPropertyName("approval_id")]
    public string? ApprovalId { get; set; }

    /// <summary>
    /// <para>加班时长</para>
    /// <para>必填：是</para>
    /// <para>示例值：1.5</para>
    /// </summary>
    [JsonPropertyName("duration")]
    public float Duration { get; set; }

    /// <summary>
    /// <para>加班时长单位</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：天</item>
    /// <item>2：小时</item>
    /// <item>3：半天</item>
    /// <item>4：半小时</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("unit")]
    public int Unit { get; set; }

    /// <summary>
    /// <para>加班日期类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：工作日</item>
    /// <item>2：休息日</item>
    /// <item>3：节假日</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("category")]
    public int Category { get; set; }

    /// <summary>
    /// <para>加班规则类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：不关联加班规则</item>
    /// <item>1：调休</item>
    /// <item>2：加班费</item>
    /// <item>3：关联加班规则，没有调休或加班费</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public int Type { get; set; }

    /// <summary>
    /// <para>开始时间，时间格式为 yyyy-MM-dd HH:mm:ss。</para>
    /// <para>时间按照审批发起人当前考勤组的时区进行取值，如果发起人已离职，则默认为 0 时区。</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-09 09:00:00</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>结束时间，时间格式为 yyyy-MM-dd HH:mm:ss。</para>
    /// <para>时间按照审批发起人当前考勤组的时区进行取值，如果发起人已离职，则默认为 0 时区。</para>
    /// <para>必填：是</para>
    /// <para>示例值：2021-01-10 13:00:00</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>加班事由</para>
    /// <para>必填：否</para>
    /// <para>示例值：推进项目进度</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// <para>唯一幂等键</para>
    /// <para>必填：否</para>
    /// <para>示例值：1233432312</para>
    /// </summary>
    [JsonPropertyName("idempotent_id")]
    public string? IdempotentId { get; set; }

    /// <summary>
    /// <para>更正流程实例 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("correct_process_id")]
    public string[]? CorrectProcessId { get; set; }

    /// <summary>
    /// <para>撤销流程实例 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cancel_process_id")]
    public string[]? CancelProcessId { get; set; }

    /// <summary>
    /// <para>发起流程实例 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string[]? ProcessId { get; set; }
}