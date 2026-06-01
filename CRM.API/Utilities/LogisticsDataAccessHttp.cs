using System.Security.Claims;
using CRM.Core.Interfaces;

namespace CRM.API.Utilities;

/// <summary>物流数据访问：范围禁止或只读时拒绝写操作（入库/出库/库存/报关）。</summary>
public static class LogisticsDataAccessHttp
{
    public static async Task<bool> CanWriteAsync(IRbacService rbac, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var summary = await rbac.GetUserPermissionSummaryAsync(userId.Trim());
        if (summary.IsSysAdmin)
            return true;
        if (summary.LogisticsDataScope == 4)
            return false;
        return summary.LogisticsDataAccess != 1;
    }

    public static async Task<bool> CanViewLogisticsMenusAsync(IRbacService rbac, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var summary = await rbac.GetUserPermissionSummaryAsync(userId.Trim());
        if (summary.IsSysAdmin)
            return true;
        return summary.LogisticsDataScope != 4;
    }
}
