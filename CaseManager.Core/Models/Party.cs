namespace CaseManager.Core.Models;

public class Party
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Contacts { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}