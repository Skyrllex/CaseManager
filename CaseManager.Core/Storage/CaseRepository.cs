using System.Text.Json;
using System.Text.Json.Serialization;
using CaseManager.Core.Crypto;
using CaseManager.Core.Models;

namespace CaseManager.Core.Storage;
public sealed class CaseRepository
{
    private readonly string _root;
    private readonly IEncryptor _encryptor;
    private readonly string _password;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CaseRepository(string root, IEncryptor encryptor, string password)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Путь пуст", nameof(root));
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Пароль пуст", nameof(password));

        _root = root;
        _encryptor = encryptor;
        _password = password;
        Directory.CreateDirectory(_root);
    }

    private string CaseDir(Guid id) => Path.Combine(_root, id.ToString());
    private string MetaFile(Guid id) => Path.Combine(CaseDir(id), "case.json.enc");
    public string DocsDir(Guid id) => Path.Combine(CaseDir(id), "docs");

    public void Save(Case c)
    {
        var dir = CaseDir(c.Id);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(DocsDir(c.Id));

        c.UpdatedAt = DateTime.UtcNow;

        var json = JsonSerializer.SerializeToUtf8Bytes(c, JsonOpts);
        var enc = _encryptor.Encrypt(json, _password);
        File.WriteAllBytes(MetaFile(c.Id), enc);
    }

    public Case? Load(Guid id)
    {
        var path = MetaFile(id);
        if (!File.Exists(path)) return null;

        var bytes = File.ReadAllBytes(path);
        var json = _encryptor.Decrypt(bytes, _password);
        return JsonSerializer.Deserialize<Case>(json, JsonOpts);
    }

    public IEnumerable<Case> LoadAll()
    {
        foreach (var dir in Directory.GetDirectories(_root))
        {
            if (!Guid.TryParse(Path.GetFileName(dir), out var id)) continue;
            Case? c = null;
            try { c = Load(id); }
            catch { /* битый файл — пропускаем; можно логировать */ }
            if (c != null) yield return c;
        }
    }

    public void Delete(Guid id)
    {
        var dir = CaseDir(id);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    public CaseDocument AddDocument(Guid caseId, string sourceFile, string? comment = null)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException("Файл не найден", sourceFile);

        var docsDir = DocsDir(caseId);
        Directory.CreateDirectory(docsDir);

        var fileName = Path.GetFileName(sourceFile);
        var dest = Path.Combine(docsDir, fileName);

        int i = 1;
        while (File.Exists(dest))
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            dest = Path.Combine(docsDir, $"{name}_{i++}{ext}");
        }

        try
        {
            File.Copy(sourceFile, dest, overwrite: false);
        }
        catch (Exception ex)
        {
            // Частично скопированный файл — удалить
            try { if (File.Exists(dest)) File.Delete(dest); } catch { }
            throw new IOException($"Не удалось добавить файл «{fileName}»: {ex.Message}", ex);
        }

        return new CaseDocument
        {
            FileName = Path.GetFileName(dest),
            RelativePath = Path.Combine("docs", Path.GetFileName(dest)),
            Comment = comment
        };
    }
    public string? GetDocumentFullPath(Guid caseId, CaseDocument doc)
    {
        var full = Path.Combine(CaseDir(caseId), doc.RelativePath);
        return File.Exists(full) ? full : null;
    }
}
