/*using CaseManager.Core.Crypto;

var enc = new AesGcmEncryptor();
var data = System.Text.Encoding.UTF8.GetBytes("привет, это тест");
var packed = enc.Encrypt(data, "мой-пароль-123");
var back = enc.Decrypt(packed, "мой-пароль-123");

Console.WriteLine(System.Text.Encoding.UTF8.GetString(back));

// Проверка на неверный пароль — должно кинуть CryptographicException
try
{
    enc.Decrypt(packed, "неверный");
}
catch (Exception ex)
{
    Console.WriteLine("Ожидаемо упало: " + ex.GetType().Name);
}*/