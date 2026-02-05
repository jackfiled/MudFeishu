// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ChatTabs;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV1ChatTabs"/>接口的相关函数。
/// </summary>
public class IFeishuV1ChatTabsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV1ChatTabsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.CreateChatTabsByIdAsync(string, CreateChatTabsRequest, string, bool?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateChatTabsByIdAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<CreateChatTabsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.CreateChatTabsByIdAsync(string, CreateChatTabsRequest, string, bool?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateChatTabsByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatTabsCreateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.UpdateChatTabsByIdAsync(string, UpdateChatTabsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatTabsByIdAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<UpdateChatTabsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.UpdateChatTabsByIdAsync(string, UpdateChatTabsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatTabsByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatTabsUpdateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.DeleteChatTabsByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteChatTabsByIdAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<ChatTabsIdsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.DeleteChatTabsByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteChatTabsByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<DeleteTabsResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.ChatTabsSortByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_ChatTabsSortByIdAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<ChatTabsIdsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.ChatTabsSortByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_ChatTabsSortByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatTabsSortResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.GetChatTabsListByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatTabsListByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetChatTabsResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
}
