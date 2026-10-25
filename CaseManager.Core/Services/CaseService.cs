using CaseManager.Core.Models;
using CaseManager.Core.Storage;

namespace CaseManager.Core.Services;

public sealed class CaseService
{
    private readonly CaseRepository _repo;
    public CaseService(CaseRepository repo) => _repo = repo;
    public Case CreateCase(
        string title,
        string description,
        string goal,
        CaseType type,
        IEnumerable<Party> parties)
    {
        var c = new Case
        {
            Title = title,
            ShortDescription = description,
            Goal = goal,
            Type = type,
            Parties = parties.ToList()
        };
        _repo.Save(c);
        return c;
    }

    public void Save(Case c) => _repo.Save(c);

    public void Delete(Guid id) => _repo.Delete(id);

    public Case? Get(Guid id) => _repo.Load(id);

    public IEnumerable<Case> ListAll() => _repo.LoadAll();

    //Document
    public CaseDocument AddDocument(Guid caseId, string filePath, string? comment = null)
    {
        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        var doc = _repo.AddDocument(caseId, filePath, comment);
        c.Documents.Add(doc);
        _repo.Save(c); 
        return doc;
    }
    public void UpdateDocumentMeta(Guid caseId, Guid docId,
                                   string? displayName = null,
                                   string? description = null,
                                   string? group = null)
    {
        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        var d = c.Documents.FirstOrDefault(x => x.Id == docId);
        if (d == null) return;

        if (displayName != null) d.DisplayName = displayName.Trim();
        if (description != null) d.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (group != null) d.Group = string.IsNullOrWhiteSpace(group) ? null : group.Trim();

        _repo.Save(c);
    }
    /// Удалить документ
    public void DeleteDocument(Guid caseId, Guid docId)
    {
        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        var d = c.Documents.FirstOrDefault(x => x.Id == docId);
        if (d == null) return;

        try { _repo.DeleteDocumentFile(caseId, d); }
        catch { /* файл занят */ }

        c.Documents.Remove(d);
        _repo.Save(c);
    }

    public string? GetDocumentFullPath(Guid caseId, CaseDocument doc)
        => _repo.GetDocumentFullPath(caseId, doc);


    // группы документов 


    public IReadOnlyList<string> GetDocumentGroups(Guid caseId)
    {
        var c = _repo.Load(caseId);
        if (c?.DocumentGroups == null) return Array.Empty<string>();

        return c.DocumentGroups
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public bool AddDocumentGroup(Guid caseId, string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return false;

        var name = groupName.Trim();
        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        c.DocumentGroups ??= new List<string>();

        if (c.DocumentGroups.Contains(name, StringComparer.OrdinalIgnoreCase))
            return false; // уже есть

        c.DocumentGroups.Add(name);
        _repo.Save(c);
        return true;
    }

    public bool DeleteDocumentGroup(Guid caseId, string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return false;

        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        if (c.DocumentGroups == null) return false;

        var removed = c.DocumentGroups.RemoveAll(
            g => g.Equals(groupName, StringComparison.OrdinalIgnoreCase)) > 0;

        if (!removed) return false;

  
        foreach (var d in c.Documents)
            if (d.Group != null && d.Group.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                d.Group = null;

        _repo.Save(c);
        return true;
    }


    public bool RenameDocumentGroup(Guid caseId, string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;

        var newTrim = newName.Trim();
        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        c.DocumentGroups ??= new List<string>();

        var idx = c.DocumentGroups.FindIndex(g => g.Equals(oldName, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;

        if (c.DocumentGroups.Any(g => g.Equals(newTrim, StringComparison.OrdinalIgnoreCase)))
            return false;

        c.DocumentGroups[idx] = newTrim;

        foreach (var d in c.Documents)
            if (d.Group != null && d.Group.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                d.Group = newTrim;

        _repo.Save(c);
        return true;
    }

}