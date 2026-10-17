//[MAGIC(4)][VERSION(1)][SALT(16)][NONCE(12)][TAG(16)][CIPHERTEXT(N)]

using System.Security.Cryptography;
using System.Text;

namespace CaseManager.Core.Crypto;

public sealed class AesGcmEncryptor : IEncryptor
{
    // "CSM1" — CaseManager v1 (чтобы потом понимать формат)
    private static readonly byte[] Magic = { 0x43, 0x53, 0x4D, 0x31 };
    private const byte Version = 1;

    private const int SaltSize = 16;
    private const int NonceSize = 12;   // стандарт для AES-GCM
    private const int TagSize = 16;
    private const int KeySize = 32;   // AES-256
    private const int Pbkdf2Iter = 200_000; // современный минимум; можно 600k, но старт будет медленнее

    public byte[] Encrypt(byte[] plaintext, string password)
    {
        if (plaintext is null) throw new ArgumentNullException(nameof(plaintext));
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Пароль пуст", nameof(password));
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var key = DeriveKey(password, salt);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        // Сборка итогового файла
        var output = new byte[Magic.Length + 1 + SaltSize + NonceSize + TagSize + ciphertext.Length];
        int p = 0;

        Buffer.BlockCopy(Magic, 0, output, p, Magic.Length); p += Magic.Length;
        output[p++] = Version;
        Buffer.BlockCopy(salt, 0, output, p, SaltSize); p += SaltSize;
        Buffer.BlockCopy(nonce, 0, output, p, NonceSize); p += NonceSize;
        Buffer.BlockCopy(tag, 0, output, p, TagSize); p += TagSize;
        Buffer.BlockCopy(ciphertext, 0, output, p, ciphertext.Length);

        CryptographicOperations.ZeroMemory(key);
        return output;
    }

    public byte[] Decrypt(byte[] fileBytes, string password)
    {
        if (fileBytes is null) throw new ArgumentNullException(nameof(fileBytes));
        if (fileBytes.Length < Magic.Length + 1 + SaltSize + NonceSize + TagSize)
            throw new InvalidDataException("Файл слишком короткий — повреждён или не наш формат.");
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Пароль пуст", nameof(password));

        int p = 0;

        // Проверка magic
        for (int i = 0; i < Magic.Length; i++)
            if (fileBytes[p + i] != Magic[i])
                throw new InvalidDataException("Неверный формат файла (магия не совпала).");

        p += Magic.Length;
        var version = fileBytes[p++];
        if (version != Version)
            throw new InvalidDataException($"Неподдерживаемая версия файла: {version}");

        var salt = new byte[SaltSize]; Buffer.BlockCopy(fileBytes, p, salt, 0, SaltSize); p += SaltSize;
        var nonce = new byte[NonceSize]; Buffer.BlockCopy(fileBytes, p, nonce, 0, NonceSize); p += NonceSize;
        var tag = new byte[TagSize]; Buffer.BlockCopy(fileBytes, p, tag, 0, TagSize); p += TagSize;

        var ciphertext = new byte[fileBytes.Length - p];
        Buffer.BlockCopy(fileBytes, p, ciphertext, 0, ciphertext.Length);

        var key = DeriveKey(password, salt);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            // Сюда попадаем и при неверном пароле, и при подмене файла
            throw new CryptographicException(
                "Не удалось расшифровать: неверный пароль или файл повреждён.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return plaintext;
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        // Rfc2898DeriveBytes — это PBKDF2 в .NET
        return Rfc2898DeriveBytes.Pbkdf2(
            password: Encoding.UTF8.GetBytes(password),
            salt: salt,
            iterations: Pbkdf2Iter,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: KeySize);
    }
}