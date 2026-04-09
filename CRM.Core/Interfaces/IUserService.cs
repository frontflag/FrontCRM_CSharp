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

        /// <summary>管理端按 Id 加载（含冻结等 IsActive=false 的账号）。</summary>
        Task<User?> GetByIdForAdminAsync(string id);

        /// <summary>员工管理列表：含正常/停用/冻结，不含已软删（IsActive=false 且非冻结）。</summary>
        Task<IEnumerable<User>> GetAllForAdminAsync();
        
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

        /// <summary>管理员重置密码（含已禁用账号，按 Id 查找）。</summary>
        Task ResetPasswordAsync(string id, string newPassword);

        /// <summary>冻结：不可登录、不可执行业务；列表中可恢复。</summary>
        Task FreezeUserAsync(string id);

        /// <summary>恢复冻结账号为正常。</summary>
        Task UnfreezeUserAsync(string id);
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
