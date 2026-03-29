using CRM.API.Models.DTOs;

namespace CRM.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        /// <summary>系统管理员模拟登录为目标员工（不发密码，仅服务端校验 SYS_ADMIN）。</summary>
        Task<ApiResponse<AuthResponse>> ImpersonateAsync(string actorUserId, string targetUserId);
        string GenerateJwtToken(string email, string userName, string userId);
    }
}
