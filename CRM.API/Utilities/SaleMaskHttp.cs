using System.Security.Claims;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.API.Utilities;

/// <summary>HTTP 请求上下文中解析当前用户并判断 PRD §5.2.1 销售敏感列是否脱敏。</summary>
public static class SaleMaskHttp
{
    public static async Task<bool> ShouldMaskSale521Async(IRbacService rbac, ClaimsPrincipal user)
    {
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(id)) return false;
        var s = await rbac.GetUserPermissionSummaryAsync(id);
        return SaleSensitiveFieldMask521.ShouldMask(s);
    }
}
