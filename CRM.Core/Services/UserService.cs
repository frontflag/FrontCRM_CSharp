using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _repository;

        public UserService(IRepository<User> repository)
        {
            _repository = repository;
        }

        public async Task<User> CreateAsync(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                throw new ArgumentException("用户名不能为空");
            if (string.IsNullOrEmpty(request.Password))
                throw new ArgumentException("密码不能为空");

            var (pwdHash, salt) = UserPasswordHasher.HashPassword(request.Password);
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.UserName.Trim(),
                PasswordHash = pwdHash,
                Salt = salt,
                RealName = request.RealName?.Trim(),
                Email = request.Email?.Trim(),
                Mobile = request.Mobile?.Trim(),
                Status = 1,
                IsActive = true,
                CreateTime = DateTime.UtcNow
            };

            await _repository.AddAsync(user);
            return user;
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var users = await _repository.FindAsync(u => u.Id == id && u.IsActive);
            return users.FirstOrDefault();
        }

        public async Task<User?> GetByIdForAdminAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            var users = await _repository.FindAsync(u => u.Id == id);
            return users.FirstOrDefault();
        }

        public async Task<IEnumerable<User>> GetAllForAdminAsync()
        {
            return await _repository.FindAsync(u => u.IsActive || u.Status == UserAccountStatus.Frozen);
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            var users = await _repository.FindAsync(u => u.UserName == userName.Trim() && u.IsActive);
            return users.FirstOrDefault();
        }

        public async Task<User> UpdateAsync(string id, UpdateUserRequest request)
        {
            var user = await GetByIdForAdminAsync(id);
            if (user == null) throw new KeyNotFoundException($"用户不存在: {id}");

            if (request.RealName != null) user.RealName = request.RealName.Trim();
            if (request.Email != null) user.Email = request.Email.Trim();
            if (request.Mobile != null) user.Mobile = request.Mobile.Trim();
            user.ModifyTime = DateTime.UtcNow;

            await _repository.UpdateAsync(user);
            return user;
        }

        public async Task DeleteAsync(string id)
        {
            var user = await GetByIdForAdminAsync(id);
            if (user == null) throw new KeyNotFoundException($"用户不存在: {id}");
            // 软删除：保留历史数据，不物理删除
            user.IsActive = false;
            user.Status = 0;
            user.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(user);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _repository.FindAsync(u => u.IsActive);
        }

        public async Task<bool> ValidateCredentialsAsync(string userName, string password)
        {
            var user = await GetByUserNameAsync(userName);
            if (user == null) return false;
            return UserPasswordHasher.Verify(password, user.PasswordHash);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            var user = await GetByIdForAdminAsync(id);
            if (user == null) throw new KeyNotFoundException($"用户不存在: {id}");
            user.Status = status;
            if (status == UserAccountStatus.Active)
                user.IsActive = true;
            else if (status == UserAccountStatus.Frozen)
                user.IsActive = false;
            user.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(user);
        }

        public async Task UpdateRoleAsync(string id, string roleId)
        {
            // 简化实现
            await Task.CompletedTask;
        }

        public async Task UpdateDepartmentAsync(string id, string departmentId)
        {
            // 简化实现
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<User>> SearchAsync(string keyword)
        {
            var all = await _repository.FindAsync(u => u.IsActive);
            if (string.IsNullOrWhiteSpace(keyword)) return all;
            
            var key = keyword.Trim().ToLower();
            return all.Where(u => 
                (u.UserName?.ToLower().Contains(key) == true) ||
                (u.RealName?.ToLower().Contains(key) == true) ||
                (u.Email?.ToLower().Contains(key) == true));
        }

        public async Task<bool> IsUserNameExistsAsync(string userName)
        {
            var user = await GetByUserNameAsync(userName);
            return user != null;
        }

        public async Task ResetPasswordAsync(string id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("用户 Id 不能为空");
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                throw new ArgumentException("新密码长度至少 6 位");

            var list = await _repository.FindAsync(u => u.Id == id);
            var user = list.FirstOrDefault() ?? throw new KeyNotFoundException($"用户不存在: {id}");

            var (pwdHash, salt) = UserPasswordHasher.HashPassword(newPassword);
            user.PasswordHash = pwdHash;
            user.Salt = salt;
            user.PasswordChangeTime = DateTime.UtcNow;
            user.ModifyTime = DateTime.UtcNow;
            // 与登录校验一致（AuthService：IsActive 且 Status==1）。仅改密码时同步解锁，避免「重置后仍提示停用/无法登录」
            user.IsActive = true;
            user.Status = 1;
            await _repository.UpdateAsync(user);
        }

        public async Task FreezeUserAsync(string id)
        {
            var user = await GetByIdForAdminAsync(id) ?? throw new KeyNotFoundException($"用户不存在: {id}");
            if (user.Status == UserAccountStatus.Frozen)
                return;
            user.Status = UserAccountStatus.Frozen;
            user.IsActive = false;
            user.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(user);
        }

        public async Task UnfreezeUserAsync(string id)
        {
            var user = await GetByIdForAdminAsync(id) ?? throw new KeyNotFoundException($"用户不存在: {id}");
            if (user.Status != UserAccountStatus.Frozen)
                throw new InvalidOperationException("该用户未处于冻结状态");
            user.Status = UserAccountStatus.Active;
            user.IsActive = true;
            user.ModifyTime = DateTime.UtcNow;
            await _repository.UpdateAsync(user);
        }
    }
}
