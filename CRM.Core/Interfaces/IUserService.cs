using CRM.Core.Models;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 创建用户
        /// </summary>
        Task<User> CreateAsync(CreateUserRequest request);
        
        /// <summary>
        /// 根据ID获取用户
        /// </summary>
        Task<User?> GetByIdAsync(string id);
        
        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        Task<User?> GetByUserNameAsync(string userName);
        
        /// <summary>
        /// 更新用户
        /// </summary>
        Task<User> UpdateAsync(string id, UpdateUserRequest request);
        
        /// <summary>
        /// 删除用户
        /// </summary>
        Task DeleteAsync(string id);
        
        /// <summary>
        /// 获取所有用户
        /// </summary>
        Task<IEnumerable<User>> GetAllAsync();
        
        /// <summary>
        /// 验证用户登录
        /// </summary>
        Task<bool> ValidateCredentialsAsync(string userName, string password);
        
        /// <summary>
        /// 更新用户状态
        /// </summary>
        Task UpdateStatusAsync(string id, short status);
        
        /// <summary>
        /// 更新用户角色
        /// </summary>
        Task UpdateRoleAsync(string id, string roleId);
        
        /// <summary>
        /// 更新用户部门
        /// </summary>
        Task UpdateDepartmentAsync(string id, string departmentId);
        
        /// <summary>
        /// 搜索用户
        /// </summary>
        Task<IEnumerable<User>> SearchAsync(string keyword);
        
        /// <summary>
        /// 检查用户名是否存在
        /// </summary>
        Task<bool> IsUserNameExistsAsync(string userName);
    }
    
    /// <summary>
    /// 创建用户请求
    /// </summary>
    public class CreateUserRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? RealName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? RoleId { get; set; }
        public string? DepartmentId { get; set; }
    }
    
    /// <summary>
    /// 更新用户请求
    /// </summary>
    public class UpdateUserRequest
    {
        public string? RealName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? RoleId { get; set; }
        public string? DepartmentId { get; set; }
    }
}
