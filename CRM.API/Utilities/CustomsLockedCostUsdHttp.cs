using System.Security.Claims;
using CRM.API.Authorization;
using CRM.Core.Interfaces;

namespace CRM.API.Utilities;

internal static class CustomsLockedCostUsdHttp
{
    /// <summary>JWT 登录用户。优先 NameIdentifier（ASP.NET 会把 sub 映射过来），再回退原始 sub/userId。</summary>
    public static string? UserId(ClaimsPrincipal? user) =>
        user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? user?.Claims.FirstOrDefault(c => c.Type is "sub" or "userId")?.Value;

    public static async Task<bool> CanCorrectAsync(IRbacService rbac, ClaimsPrincipal user)
    {
        var uid = UserId(user);
        if (string.IsNullOrWhiteSpace(uid))
            return false;
        var summary = await rbac.GetUserPermissionSummaryAsync(uid.Trim());
        return ManagementAccountPolicy.CanForceDelete(summary);
    }
}
