using CRM.API.Models.DTOs;

namespace CRM.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        string GenerateJwtToken(string email, string userName, string userId);
    }
}
