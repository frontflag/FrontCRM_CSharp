using CRM.Core.Models.Rbac;

namespace CRM.Core.Interfaces
{
    public interface IRbacService
    {
        Task<UserPermissionSummaryDto> GetUserPermissionSummaryAsync(string userId);

        Task<IReadOnlyList<RbacRole>> GetRolesAsync();
        Task<IReadOnlyList<RbacPermission>> GetPermissionsAsync();
        Task<IReadOnlyList<RbacDepartment>> GetDepartmentsAsync();

        Task AssignUserRolesAsync(string userId, IReadOnlyList<string> roleIds);
        Task AssignUserDepartmentsAsync(string userId, IReadOnlyList<string> departmentIds, string? primaryDepartmentId);
        Task AssignRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionIds);
    }

    public class UserPermissionSummaryDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsSysAdmin { get; set; }
        public IReadOnlyList<string> RoleCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> DepartmentIds { get; set; } = Array.Empty<string>();
        public string? PrimaryDepartmentId { get; set; }
        public short IdentityType { get; set; } = 0;
        public short SaleDataScope { get; set; } = 1;
        public short PurchaseDataScope { get; set; } = 1;
    }
}
