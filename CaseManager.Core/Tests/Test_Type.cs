/*using System.Text;
using CaseManager.Core.Crypto;
using CaseManager.Core.Storage;

var root = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CaseManager");
var pwd = "1234"; 

var repo = new CaseRepository(root, new AesGcmEncryptor(), pwd);

foreach (var dir in Directory.GetDirectories(root))
{
    if (!Guid.TryParse(Path.GetFileName(dir), out var id)) continue;
    var meta = Path.Combine(dir, "case.json.enc");
    if (!File.Exists(meta)) continue;

    var bytes = File.ReadAllBytes(meta);
    var json = new AesGcmEncryptor().Decrypt(bytes, pwd);
    Console.WriteLine("=== " + Path.GetFileName(dir) + " ===");
    Console.WriteLine(Encoding.UTF8.GetString(json));
    Console.WriteLine();
}*/