namespace CaseManager.Core.Models;

public class Case
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Goal { get; set; } = "";

    public CaseType Type { get; set; } = CaseType.Civil;
    public CaseStage Stage { get; set; } = CaseStage.Started;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Party> Parties { get; set; } = new();
    public List<CaseDocument> Documents { get; set; } = new();

    public List<string> DocumentGroups { get; set; } = new();
}