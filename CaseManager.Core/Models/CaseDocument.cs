namespace CaseManager.Core.Models;

public class CaseDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "";       // имя файла в папке docs
    public string RelativePath { get; set; } = "";   // "docs/xxx.pdf"
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
}