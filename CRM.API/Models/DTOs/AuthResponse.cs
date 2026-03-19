namespace CRM.API.Models.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public bool IsSysAdmin { get; set; }
        public IReadOnlyList<string> RoleCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> DepartmentIds { get; set; } = Array.Empty<string>();
    }
}
