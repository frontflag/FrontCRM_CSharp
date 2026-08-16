using System.Security.Claims;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.API.Utilities;

/// <summary>报关板块 API 准入：系统/平台管理员、业务数据 bypass、财务部（IdentityType=5）、物流部（IdentityType=6）。</summary>
public static class CustomsModuleAccessHttp
{
    public static async Task<bool> CanAccessAsync(IRbacService rbac, ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var summary = await rbac.GetUserPermissionSummaryAsync(userId.Trim());
        return CustomsModuleAccessRules.CanAccessModule(summary);
    }
}
