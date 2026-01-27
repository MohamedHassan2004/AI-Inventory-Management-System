namespace Inventory.Infrastructure.Services;

public class FileServiceOptions
{
    public const string SectionName = "FileUpload";
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx"
    };

    public int MaxFileSizeInMB { get; set; } = 10;
    public string UploadFolder { get; set; } = "uploads";
    public List<string> AllowedMimeTypes { get; set; } = new()
    {
        "image/jpeg", "image/png", "image/gif",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };
}
