using CRM.Core.Models.Rbac;

namespace CRM.Core.Interfaces
{
    public interface IRbacService
    {
        Task<UserPermissionSummaryDto> GetUserPermissionSummaryAsync(string userId);

        Task<IReadOnlyList<RbacRole>> GetRolesAsync();
        Task<IReadOnlyList<RbacPermission>> GetPermissionsAsync();
        Task<IReadOnlyList<RbacDepartment>> GetDepartmentsAsync();

        Task<RbacDepartment> CreateDepartmentAsync(
            string departmentName,
            string? parentId,
            short saleDataScope,
            short saleDataAccess,
            bool hideCustomerManagement,
            short purchaseDataScope,
            short purchaseDataAccess,
            bool hideVendorManagement,
            short logisticsDataScope,
            short logisticsDataAccess,
            short financeDataScope,
            short financeDataAccess,
            short identityType,
            short status);

        Task<RbacDepartment?> UpdateDepartmentAsync(
            string departmentId,
            string departmentName,
            string? parentId,
            short saleDataScope,
            short saleDataAccess,
            bool hideCustomerManagement,
            short purchaseDataScope,
            short purchaseDataAccess,
            bool hideVendorManagement,
            short logisticsDataScope,
            short logisticsDataAccess,
            short financeDataScope,
            short financeDataAccess,
            short identityType,
            short status);

        Task AssignUserRolesAsync(string userId, IReadOnlyList<string> roleIds);
        Task AssignUserDepartmentsAsync(string userId, IReadOnlyList<string> departmentIds, string? primaryDepartmentId);
        Task AssignRolePermissionsAsync(string roleId, IReadOnlyList<string> permissionIds);
    }

    public class UserPermissionSummaryDto
    {
        public string UserId { get; set; } = string.Empty;
        /// <summary>产品 SuperAdmin（RoleCode=SYS_ADMIN）</summary>
        public bool IsSysAdmin { get; set; }
        /// <summary>产品 Admin（RoleCode=SYS_MANAGER）</summary>
        public bool IsSysManager { get; set; }
        /// <summary>产品 Manager（RoleCode=SYS_BIZ_MANAGER）</summary>
        public bool IsBizManager { get; set; }
        /// <summary>强制删除：SuperAdmin 或产品 Admin（SYS_MANAGER）。</summary>
        public bool CanForceDelete => IsSysAdmin || IsSysManager;
        /// <summary>可访问系统管理门禁（SuperAdmin / Admin / Manager）</summary>
        public bool HasManagementAccess { get; set; }
        /// <summary>业务数据行级全量 bypass</summary>
        public bool HasBizDataBypass { get; set; }
        public IReadOnlyList<string> RoleCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> DepartmentIds { get; set; } = Array.Empty<string>();
        public string? PrimaryDepartmentId { get; set; }
        public short IdentityType { get; set; } = 0;
        public short SaleDataScope { get; set; } = 1;
        /// <summary>0=读写,1=只读（主部门 SaleDataAccess）</summary>
        public short SaleDataAccess { get; set; } = 0;
        /// <summary>主部门隐藏客户管理（与 SaleDataScope 独立）</summary>
        public bool HideCustomerManagement { get; set; }
        public short PurchaseDataScope { get; set; } = 1;
        /// <summary>0=读写,1=只读（主部门 PurchaseDataAccess）</summary>
        public short PurchaseDataAccess { get; set; } = 0;
        /// <summary>主部门隐藏供应商管理（与 PurchaseDataScope 独立）</summary>
        public bool HideVendorManagement { get; set; }
        public short LogisticsDataScope { get; set; } = 0;
        /// <summary>0=读写,1=只读（主部门 LogisticsDataAccess）</summary>
        public short LogisticsDataAccess { get; set; } = 0;
        public short FinanceDataScope { get; set; } = 0;
        /// <summary>0=读写,1=只读（主部门 FinanceDataAccess）</summary>
        public short FinanceDataAccess { get; set; } = 0;

        /// <summary>是否隶属采购侧部门（主部门 IdentityType 2/3、兼任采购部门、或主部门名称含采购等兜底），与权限汇总逻辑一致。</summary>
        public bool BelongsToPurchaseDept { get; set; }
    }
}
