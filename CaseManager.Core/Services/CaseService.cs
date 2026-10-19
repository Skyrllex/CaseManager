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
    public void AddDocument(Guid caseId, string filePath, string? comment = null)
    {
        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        var doc = _repo.AddDocument(caseId, filePath, comment);
        c.Documents.Add(doc);
        _repo.Save(c);
    }
    public void UpdateDocumentMeta(Guid caseId, Guid docId,
                               string? displayName = null,
                               string? description = null,
                               DocumentKind? kind = null,
                               string? group = null)
    {
        var c = _repo.Load(caseId) ?? throw new InvalidOperationException("Дело не найдено");
        var d = c.Documents.FirstOrDefault(x => x.Id == docId);
        if (d == null) return;

        if (displayName != null) d.DisplayName = displayName.Trim();
        if (description != null) d.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (kind != null) d.Kind = kind.Value;
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
}