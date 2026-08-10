namespace CRM.API.Models.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool IsSysAdmin { get; set; }
        /// <summary>是否为管理员模拟登录会话（不弹系统公告、不记已读）。</summary>
        public bool IsImpersonating { get; set; }
        public IReadOnlyList<string> RoleCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> DepartmentIds { get; set; } = Array.Empty<string>();
    }
}
