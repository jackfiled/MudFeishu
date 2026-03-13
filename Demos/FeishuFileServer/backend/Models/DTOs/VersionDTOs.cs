namespace FeishuFileServer.Models.DTOs;

public class VersionResponse
{
    public int Id { get; set; }
    public string FileToken { get; set; } = string.Empty;
    public string VersionToken { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FileMD5 { get; set; }
    public DateTime CreatedTime { get; set; }
    public bool IsCurrentVersion { get; set; }
}

public class VersionListResponse
{
    public List<VersionResponse> Versions { get; set; } = new();
    public int TotalCount { get; set; }
}

public class VersionCreateResponse
{
    public string VersionToken { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public DateTime CreatedTime { get; set; }
}
