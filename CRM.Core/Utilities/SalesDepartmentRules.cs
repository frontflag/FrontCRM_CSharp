using CRM.Core.Models.Rbac;

namespace CRM.Core.Utilities;

/// <summary>销售相关部门判定（与 AuthController 销售业务员树一致）。</summary>
public static class SalesDepartmentRules
{
    public static bool IsSalesDepartment(RbacDepartment d)
    {
        if (d.IdentityType == 1) return true;
        var name = d.DepartmentName ?? string.Empty;
        return name.Contains("销售", StringComparison.OrdinalIgnoreCase)
               || name.Contains("sales", StringComparison.OrdinalIgnoreCase);
    }
}
