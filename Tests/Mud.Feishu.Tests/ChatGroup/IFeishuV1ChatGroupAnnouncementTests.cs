// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ChatGroupNotice;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement"/>接口的相关函数。
/// </summary>
public class IFeishuV1ChatGroupAnnouncementTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV1ChatGroupAnnouncementTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetNoticeInfoByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetNoticeInfoByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetAnnouncementResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetNoticeBlocksListByIdAsync(string, int?, string?, int?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetNoticeBlocksListByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<AnnouncementBlock>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.CreateNoticeBlockAsync(string, string, CreateBlockRequest, int, string?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateNoticeBlockAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<CreateBlockRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.CreateNoticeBlockAsync(string, string, CreateBlockRequest, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateNoticeBlockAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateBlockResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.UpdateNoticeBlockAsync(string, BlocksBatchUpdateRequest, int, string?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateNoticeBlockAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<BlocksBatchUpdateRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.UpdateNoticeBlockAsync(string, BlocksBatchUpdateRequest, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateNoticeBlockAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchUpdateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetBlockContentByIdAsync(string, string, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetBlockContentByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetBlockContentListResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetBlockContentPageListByIdAsync(string, string, int?, string?, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetBlockContentPageListByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<AnnouncementBlock>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.DeleteBlockByIdAsync(string, string, DeleteAnnouncementBlockRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteBlockByIdAsync_RequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<DeleteAnnouncementBlockRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.DeleteBlockByIdAsync(string, string, DeleteAnnouncementBlockRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteBlockByIdAsync_Result()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<DeleteAnnouncementBlockResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
}
