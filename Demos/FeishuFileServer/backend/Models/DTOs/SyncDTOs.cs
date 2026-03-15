namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 同步结果
/// </summary>
public class SyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 同步的文件夹数量
    /// </summary>
    public int SyncedFolders { get; set; }

    /// <summary>
    /// 同步的文件数量
    /// </summary>
    public int SyncedFiles { get; set; }

    /// <summary>
    /// 新增的文件夹数量
    /// </summary>
    public int AddedFolders { get; set; }

    /// <summary>
    /// 新增的文件数量
    /// </summary>
    public int AddedFiles { get; set; }

    /// <summary>
    /// 更新的文件夹数量
    /// </summary>
    public int UpdatedFolders { get; set; }

    /// <summary>
    /// 更新的文件数量
    /// </summary>
    public int UpdatedFiles { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 同步时间
    /// </summary>
    public DateTime SyncTime { get; set; }
}

/// <summary>
/// 同步状态
/// </summary>
public class SyncStatus
{
    /// <summary>
    /// 是否正在同步
    /// </summary>
    public bool IsSyncing { get; set; }

    /// <summary>
    /// 最后同步时间
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// 同步进度 (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 当前同步的项目
    /// </summary>
    public string? CurrentItem { get; set; }

    /// <summary>
    /// 总文件夹数
    /// </summary>
    public int TotalFolders { get; set; }

    /// <summary>
    /// 总文件数
    /// </summary>
    public int TotalFiles { get; set; }
}
