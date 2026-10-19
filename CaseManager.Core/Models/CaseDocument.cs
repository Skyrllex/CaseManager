namespace CaseManager.Core.Models;

public class CaseDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = "";       // имя файла в папке docs

    public string DisplayName { get; set; } = "";
    public string RelativePath { get; set; } = "";   // "docs/xxx.pdf"

    public DocumentKind Kind { get; set; } = DocumentKind.Other; //тип

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public long SizeBytes { get; set; }
    public string? Description { get; set; }

    public string? Group { get; set; }

}