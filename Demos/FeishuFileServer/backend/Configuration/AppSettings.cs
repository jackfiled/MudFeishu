namespace FeishuFileServer.Configuration;

public class FileUploadSettings
{
    public List<string> AllowedExtensions { get; set; } = new() 
    { 
        ".docx", ".xlsx", ".pptx", ".png", ".jpg", ".jpeg", ".tiff", ".pdf" 
    };
    public int MaxFileSizeMB { get; set; } = 100;
    public bool EnableChunkUpload { get; set; } = true;
    public int ChunkSizeMB { get; set; } = 5;
    public bool EnableDeduplication { get; set; } = true;
}

public class VersionManagementSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxVersionsPerFile { get; set; } = 50;
}

public class CorsSettings
{
    public List<string> AllowedOrigins { get; set; } = new() { "http://localhost:3000" };
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    public List<string> AllowedHeaders { get; set; } = new() { "*" };
}
