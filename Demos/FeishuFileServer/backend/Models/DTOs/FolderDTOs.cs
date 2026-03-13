namespace FeishuFileServer.Models.DTOs;

public class FolderCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ParentFolderToken { get; set; }
}

public class FolderUpdateRequest
{
    public string? Name { get; set; }
    public string? ParentFolderToken { get; set; }
}

public class FolderResponse
{
    public int Id { get; set; }
    public string FolderToken { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string? ParentFolderToken { get; set; }
    public DateTime CreatedTime { get; set; }
    public bool IsDeleted { get; set; }
}

public class FolderListResponse
{
    public List<FolderResponse> Folders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class FolderContentsResponse
{
    public List<FolderResponse> Folders { get; set; } = new();
    public List<FileInfoResponse> Files { get; set; } = new();
}
