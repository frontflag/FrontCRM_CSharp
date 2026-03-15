using CRM.Core.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace CRM.Core.Services
{
    /// <summary>
    /// 用户注册服务
    /// </summary>
    public class UserRegistrationService
    {
        /// <summary>
        /// 用户注册
        /// 同时保存密码哈希和密码明文（测试用途）
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">明文密码</param>
        /// <param name="email">邮箱</param>
        /// <returns>创建的用户对象</returns>
        public User RegisterUser(string userName, string password, string email)
        {
            // 1. 生成随机盐值
            string salt = GenerateSalt();

            // 2. 计算密码哈希值（使用 SHA256 + Salt）
            string passwordHash = ComputeHash(password, salt);

            // 3. 创建用户对象
            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = passwordHash,
                Salt = salt,
                // 记录密码明文（仅用于测试阶段）
                PasswordPlain = password,
                IsActive = true,
                CreateTime = DateTime.UtcNow
            };

            // 4. 保存到数据库（这里调用仓储层保存）
            // await _userRepository.AddAsync(user);

            return user;
        }

        /// <summary>
        /// 生成随机盐值
        /// </summary>
        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// 计算密码哈希（SHA256 + Salt）
        /// </summary>
        private string ComputeHash(string password, string salt)
        {
            string saltedPassword = password + salt;
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// 验证用户登录（使用 PasswordHash 字段）
        /// </summary>
        public bool ValidateUser(User user, string inputPassword)
        {
            // 使用 Salt 计算输入密码的哈希值
            string inputHash = ComputeHash(inputPassword, user.Salt);
            
            // 与数据库中的 PasswordHash 比较
            return inputHash == user.PasswordHash;
        }

        /// <summary>
        /// 修改密码
        /// 同时更新密码哈希和密码明文
        /// </summary>
        public void ChangePassword(User user, string newPassword)
        {
            // 1. 生成新盐值
            string newSalt = GenerateSalt();

            // 2. 计算新密码哈希
            string newPasswordHash = ComputeHash(newPassword, newSalt);

            // 3. 更新用户信息
            user.PasswordHash = newPasswordHash;
            user.Salt = newSalt;
            // 更新明文密码（测试用途）
            user.PasswordPlain = newPassword;
            user.PasswordChangeTime = DateTime.UtcNow;
            user.ModifyTime = DateTime.UtcNow;

            // 4. 保存到数据库
            // await _userRepository.UpdateAsync(user);
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        public string ResetPassword(User user)
        {
            // 1. 生成随机新密码
            string newPassword = GenerateRandomPassword();

            // 2. 修改密码
            ChangePassword(user, newPassword);

            // 3. 返回新密码（用于发送邮件等）
            return newPassword;
        }

        /// <summary>
        /// 生成随机密码
        /// </summary>
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var sb = new StringBuilder();
            var random = new Random();

            for (int i = 0; i < 12; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }

            return sb.ToString();
        }
    }
}
