// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive.Files;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 文件是云空间内各种类型的文件的统称，泛指云空间内所有的文件。包括在云空间创建的在线文档、电子表格、多维表格、思维笔记、知识库中的文档等，也包括从本地环境上传的各类文件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/drive-v1/file/file-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV1DriveFiles : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 用于根据文件 token 获取其元数据，包括标题、所有者、创建时间、密级、访问链接等数据。
    /// </summary>
    /// <param name="metasBatchQueryRequest">获取文件元数据请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/metas/batch_query")]
    Task<FeishuApiResult<MetasBatchQueryResult>?> BatchQueryMetasAsync(
        [Body] MetasBatchQueryRequest metasBatchQueryRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
