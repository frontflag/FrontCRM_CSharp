using CRM.Core.Interfaces;
using CRM.Core.Models;

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

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.UserName.Trim(),
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Password)),
                RealName = request.RealName?.Trim(),
                Email = request.Email?.Trim(),
                Mobile = request.Mobile?.Trim(),
                Status = 1,
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

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return null;
            var users = await _repository.FindAsync(u => u.UserName == userName.Trim() && u.IsActive);
            return users.FirstOrDefault();
        }

        public async Task<User> UpdateAsync(string id, UpdateUserRequest request)
        {
            var user = await GetByIdAsync(id);
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
            var user = await GetByIdAsync(id);
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
            var hashed = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            return hashed == user.PasswordHash;
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            var user = await GetByIdAsync(id);
            if (user == null) throw new KeyNotFoundException($"用户不存在: {id}");
            user.Status = status;
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
    }
}
