using System.Security.Cryptography;
using System.Text;
using CRM.API.Services.Interfaces;

namespace CRM.API.Services.Implementations;

/// <summary>AES-256-CBC 对称加密；密文格式 Base64(IV + ciphertext)。</summary>
public sealed class MailboxPasswordCipher : IMailboxPasswordCipher
{
    public const short Version = 1;
    private readonly byte[] _key;

    public MailboxPasswordCipher(IConfiguration configuration)
    {
        var keyB64 = configuration["MailboxCrypto:Key"]?.Trim();
        if (string.IsNullOrEmpty(keyB64))
        {
            // 开发缺省：由固定种子派生 32 字节（生产务必配置 MailboxCrypto:Key）
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("FrontCRM.MailboxCrypto.DevFallback.v1"));
        }
        else
        {
            try
            {
                _key = Convert.FromBase64String(keyB64);
            }
            catch (FormatException)
            {
                _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyB64));
            }

            if (_key.Length != 32)
                _key = SHA256.HashData(_key);
        }
    }

    public short CurrentVersion => Version;

    public string Encrypt(string plainText)
    {
        if (plainText == null) throw new ArgumentNullException(nameof(plainText));
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        var packed = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, packed, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, packed, aes.IV.Length, cipher.Length);
        return Convert.ToBase64String(packed);
    }

    public string Decrypt(string cipherText, short cryptoVersion = 1)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return string.Empty;
        if (cryptoVersion != Version)
            throw new InvalidOperationException($"不支持的邮箱密码加密版本: {cryptoVersion}");

        var packed = Convert.FromBase64String(cipherText.Trim());
        if (packed.Length < 17)
            throw new InvalidOperationException("邮箱密码密文无效");

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        var iv = new byte[16];
        Buffer.BlockCopy(packed, 0, iv, 0, 16);
        aes.IV = iv;
        var cipher = new byte[packed.Length - 16];
        Buffer.BlockCopy(packed, 16, cipher, 0, cipher.Length);
        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
