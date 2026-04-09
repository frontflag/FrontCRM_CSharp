using System.Text;

namespace CRM.Core.Utilities;

/// <summary>
/// 用户密码存储与校验（与 <c>AuthService</c> 登录一致；兼容历史错误实现 Base64(UTF8(明文))）。
/// </summary>
public static class UserPasswordHasher
{
    public static (string PasswordHash, string Salt) HashPassword(string plainText)
    {
        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var hash = BCrypt.Net.BCrypt.HashPassword(plainText);
        return (hash, salt);
    }

    public static bool Verify(string plainText, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;
        if (storedHash.StartsWith("$2", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(plainText, storedHash);
            }
            catch
            {
                return false;
            }
        }

        // 历史：UserService.CreateAsync 曾错误地使用 Base64(UTF8(明文))
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText)) == storedHash;
    }
}
