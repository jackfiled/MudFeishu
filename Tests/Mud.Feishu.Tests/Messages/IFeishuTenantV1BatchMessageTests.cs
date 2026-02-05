// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Messages;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试批量消息相关接口
/// </summary>
public class IFeishuTenantV1BatchMessageTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 批量发送文本消息
    [Fact]
    public void TestBatchSendTextMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<BatchSenderTextMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendTextMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 批量发送富文本消息
    [Fact]
    public void TestBatchSendRichTextMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<BatchSenderRichTextMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendRichTextMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 批量发送图片消息
    [Fact]
    public void TestBatchSendImageMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<BatchSenderMessageImageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendImageMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 批量发送群分享消息
    [Fact]
    public void TestBatchSendGroupShareMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<BatchSenderMessageGroupShareRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendGroupShareMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 撤回批量消息
    [Fact]
    public void TestRevokeMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }
    #endregion

    #region 查询批量消息已读状态
    [Fact]
    public void TestGetUserReadMessageInfosAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageReadStatusResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 查询批量消息进度
    [Fact]
    public void TestGetBatchMessageProgressAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageProgressResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion
}
