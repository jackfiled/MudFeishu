namespace FeishuFileServer.Models.DTOs;

public class FileUploadRequest
{
    public IFormFile? File { get; set; }
    public string? FolderToken { get; set; }
}

public class FileUploadResponse
{
    public string FileToken { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? FileMD5 { get; set; }
    public DateTime UploadTime { get; set; }
}

public class FileInfoResponse
{
    public int Id { get; set; }
    public string FileToken { get; set; } = string.Empty;
    public string? FolderToken { get; set; }
    public string? VersionToken { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? FileMD5 { get; set; }
    public DateTime UploadTime { get; set; }
    public bool IsDeleted { get; set; }
}

public class FileListResponse
{
    public List<FileInfoResponse> Files { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
