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

        c.DocumentGroups ??= new List<string>();
        c.UpdatedAt = DateTime.UtcNow;

        foreach (var d in c.Documents)
        {
            if (string.IsNullOrWhiteSpace(d.FileName)) continue;

            var detected = DocumentKindHelper.FromFileName(d.FileName);
            if (detected != DocumentKind.Other)
                d.Kind = detected;

            try
            {
                var full = Path.Combine(dir, d.RelativePath ?? Path.Combine("docs", d.FileName));
                if (File.Exists(full))
                    d.SizeBytes = new FileInfo(full).Length;
            }
            catch { }
        }

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
        var c = JsonSerializer.Deserialize<Case>(json, JsonOpts);
        if (c == null) return null;

        c.Parties ??= new();
        c.Parties.RemoveAll(p => p == null);
        foreach (var p in c.Parties)
        {
            p.Name ??= "";
            p.Role ??= "";
        }

        c.Documents ??= new();
        c.Documents.RemoveAll(d => d == null);
        foreach (var d in c.Documents)
        {
            d.FileName ??= "";
            d.RelativePath ??= "";
            if (string.IsNullOrWhiteSpace(d.DisplayName))
                d.DisplayName = string.IsNullOrWhiteSpace(d.FileName)
                    ? "(без имени)"
                    : Path.GetFileNameWithoutExtension(d.FileName);

            if (d.UploadedAt == default) d.UploadedAt = DateTime.UtcNow;
        }

        c.DocumentGroups ??= new List<string>();
        c.DocumentGroups.RemoveAll(string.IsNullOrWhiteSpace);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cleaned = new List<string>();
        foreach (var g in c.DocumentGroups)
        {
            var t = g.Trim();
            if (seen.Add(t)) cleaned.Add(t);
        }
        c.DocumentGroups = cleaned;

        return c;
    }
    public IEnumerable<Case> LoadAll()
    {
        foreach (var dir in Directory.GetDirectories(_root))
        {
            if (!Guid.TryParse(Path.GetFileName(dir), out var id)) continue;
            Case? c = null;
            try { c = Load(id); }
            catch { /* битый файл - пропускаем */ }
            if (c != null) yield return c;
        }
    }

    public void Delete(Guid id)
    {
        var dir = CaseDir(id);
        if (!Directory.Exists(dir)) return;

        foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            try { File.SetAttributes(f, FileAttributes.Normal); } catch { }
        }
        try { Directory.Delete(dir, true); }
        catch (IOException)
        {
            System.Threading.Thread.Sleep(100);
            try { Directory.Delete(dir, true); } catch { }
        }
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

        try { File.Copy(sourceFile, dest, overwrite: false); }
        catch (Exception ex) { };


        var fi = new FileInfo(dest);
        var kind = DocumentKindHelper.FromFile(dest);

        return new CaseDocument
        {
            FileName = Path.GetFileName(dest),
            DisplayName = Path.GetFileNameWithoutExtension(dest),
            RelativePath = Path.Combine("docs", Path.GetFileName(dest)),
            Kind = kind,
            Description = comment,
            SizeBytes = fi.Length,   // ← ЭТО
            UploadedAt = DateTime.UtcNow
        };
    }
    /// Удалить файл с диска.
    public void DeleteDocumentFile(Guid caseId, CaseDocument doc)
    {
        if (string.IsNullOrWhiteSpace(doc.RelativePath)) return;
        var full = Path.Combine(CaseDir(caseId), doc.RelativePath);
        if (File.Exists(full))
        {
            try { File.SetAttributes(full, FileAttributes.Normal); } catch { }
            File.Delete(full);
        }
    }

    public string? GetDocumentFullPath(Guid caseId, CaseDocument doc)
    {
        if (doc == null || string.IsNullOrWhiteSpace(doc.RelativePath)) return null;
        var full = Path.Combine(CaseDir(caseId), doc.RelativePath);
        return File.Exists(full) ? full : null;
    }

}