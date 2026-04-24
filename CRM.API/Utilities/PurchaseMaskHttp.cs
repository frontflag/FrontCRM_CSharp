using System.Security.Claims;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.API.Utilities;

/// <summary>HTTP 请求上下文中解析当前用户并判断 PRD §5.1.1 采购敏感列是否脱敏。</summary>
public static class PurchaseMaskHttp
{
    public static async Task<bool> ShouldMaskPurchase511Async(IRbacService rbac, ClaimsPrincipal user)
    {
        var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(id)) return false;
        var s = await rbac.GetUserPermissionSummaryAsync(id);
        return PurchaseSensitiveFieldMask511.ShouldMask(s);
    }
}
