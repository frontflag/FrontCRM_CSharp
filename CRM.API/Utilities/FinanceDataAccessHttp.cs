using System.Security.Claims;
using CRM.Core.Interfaces;

namespace CRM.API.Utilities;

/// <summary>财务数据访问：范围禁止或只读时拒绝写操作（付款管理/收款管理）。</summary>
public static class FinanceDataAccessHttp
{
    public static async Task<bool> CanWriteAsync(IRbacService rbac, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var summary = await rbac.GetUserPermissionSummaryAsync(userId.Trim());
        if (summary.IsSysAdmin)
            return true;
        if (summary.FinanceDataScope == 4)
            return false;
        return summary.FinanceDataAccess != 1;
    }

    public static async Task<bool> CanViewFinanceMenusAsync(IRbacService rbac, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var summary = await rbac.GetUserPermissionSummaryAsync(userId.Trim());
        if (summary.IsSysAdmin)
            return true;
        return summary.FinanceDataScope != 4;
    }
}
