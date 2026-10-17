/*using CaseManager.Core.Crypto;

var enc = new AesGcmEncryptor();
var data = System.Text.Encoding.UTF8.GetBytes("тест");
var packed = enc.Encrypt(data, "пароль123");
var back = enc.Decrypt(packed, "пароль123");

Console.WriteLine(System.Text.Encoding.UTF8.GetString(back));

try
{
    enc.Decrypt(packed, "неверный");
}
catch (Exception ex)
{
    Console.WriteLine("Ожидаемо упало: " + ex.GetType().Name);
}*/