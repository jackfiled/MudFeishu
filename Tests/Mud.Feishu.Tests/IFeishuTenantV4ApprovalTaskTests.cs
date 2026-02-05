// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ApprovalTask;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

public class IFeishuTenantV4ApprovalTaskTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV4ApprovalTaskTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.AgreeApprovalAsync(AgreeApprovalTasksRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AgreeApprovalAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<AgreeApprovalTasksRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.ApprovalCode);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.AgreeApprovalAsync(AgreeApprovalTasksRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AgreeApprovalAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RejectApprovalAsync(RejectApprovalTaskRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RejectApprovalAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<RejectApprovalTaskRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RejectApprovalAsync(RejectApprovalTaskRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RejectApprovalAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.TransferApprovalAsync(TransferApprovalTasksRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_TransferApprovalAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<TransferApprovalTasksRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.TransferApprovalAsync(TransferApprovalTasksRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_TransferApprovalAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RollbackApprovalAsync(RollbackApprovalInstancesRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RollbackApprovalAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<RollbackApprovalInstancesRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RollbackApprovalAsync(RollbackApprovalInstancesRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RollbackApprovalAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.InstancesAddSignAsync(InstancesAddSignRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_InstancesAddSignAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<InstancesAddSignRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.InstancesAddSignAsync(InstancesAddSignRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_InstancesAddSignAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.ResubmitApprovalAsync(ResubmitApprovalRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_ResubmitApprovalAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<ResubmitApprovalRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.ResubmitApprovalAsync(ResubmitApprovalRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_ResubmitApprovalAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }
}
