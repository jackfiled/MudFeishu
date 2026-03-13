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
/// 用于测试消息相关接口（租户访问令牌）
/// </summary>
public class IFeishuTenantV1MessageTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 发送消息
    [Fact]
    public void TestSendMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<SendMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.MsgType);
        Assert.NotNull(requestBody.ReceiveId);
    }

    [Fact]
    public void TestSendMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageDataResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 回复消息
    [Fact]
    public void TestReplyMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<ReplyMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestReplyMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageDataResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 编辑消息
    [Fact]
    public void TestEditMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<EditMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestEditMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageDataResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 转发消息
    [Fact]
    public void TestReceiveMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<ReceiveMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestReceiveMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ReceiveMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 合并转发消息
    [Fact]
    public void TestMergeReceiveMessageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<MergeReceiveMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestMergeReceiveMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MergeReceiveMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 转发话题
    [Fact]
    public void TestReceiveThreadsAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ThreadResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 创建消息跟随气泡
    [Fact]
    public void TestCreateMessageFollowUpAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<MessageFollowUpRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestCreateMessageFollowUpAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }
    #endregion

    #region 查询消息已读用户
    [Fact]
    public void TestGetMessageReadUsesAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<ReadMessageUser>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }
    #endregion

    #region 获取历史消息
    [Fact]
    public void TestGetHistoryMessageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<HistoryMessageData>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }
    #endregion

    #region 获取消息文件（小文件）
    [Fact]
    public void TestGetMessageFileResult()
    {
        // 这是一个返回字节数组的函数，无需测试反序列化
    }
    #endregion

    #region 获取消息文件（大文件）
    [Fact]
    public void TestGetMessageLargeFileResult()
    {
        // 这是一个保存到本地文件的函数，无需测试反序列化
    }
    #endregion

    #region 通过消息ID获取内容
    [Fact]
    public void TestGetContentListByMessageIdAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiListResult<MessageContentData>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }
    #endregion

    #region 下载文件（小文件）
    [Fact]
    public void TestDownFileAsyncResult()
    {
        // 这是一个返回字节数组的函数，无需测试反序列化
    }
    #endregion

    #region 下载文件（大文件）
    [Fact]
    public void TestDownLargeFileAsyncResult()
    {
        // 这是一个保存到本地文件的函数，无需测试反序列化
    }
    #endregion

    #region 下载图片（小文件）
    [Fact]
    public void TestDownImageAsyncResult()
    {
        // 这是一个返回字节数组的函数，无需测试反序列化
    }
    #endregion

    #region 下载图片（大文件）
    [Fact]
    public void TestDownLargeImageAsyncResult()
    {
        // 这是一个保存到本地文件的函数，无需测试反序列化
    }
    #endregion

    #region 上传文件
    [Fact]
    public void TestUploadFileAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<UploadMessageFileRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUploadFileAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<FileUploadResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.FileKey!);
    }
    #endregion

    #region 上传图片
    [Fact]
    public void TestUploadImageAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<UploadImageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUploadImageAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ImageUpdateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ImageKey!);
    }
    #endregion

    #region 消息加急（应用内）
    [Fact]
    public void TestMessageUrgentAppAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<MessageUrgentRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestMessageUrgentAppAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageUrgentResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 消息加急（短信）
    [Fact]
    public void TestMessageUrgentSMSAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageUrgentResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 消息加急（电话）
    [Fact]
    public void TestMessageUrgentPhoneAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageUrgentResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 更新URL预览
    [Fact]
    public void TestUpdateUrlPreviewAsyncRequestBody()
    {
        string bodyStr = "";
        var requestBody = JsonSerializer.Deserialize<UrlPreviewRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUpdateUrlPreviewAsyncResult()
    {
        string resultStr = "";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }
    #endregion
}
