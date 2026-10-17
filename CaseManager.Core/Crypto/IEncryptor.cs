namespace CaseManager.Core.Crypto;

public interface IEncryptor
{
    byte[] Encrypt(byte[] plaintext, string password);
    byte[] Decrypt(byte[] ciphertext, string password);
}