using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;

namespace CRM.Core.Utilities;

/// <summary>
/// 需求明细「保护时长」：创建后 N 分钟内仅分配采购员可见/可报价；超过后或 N=0（无保护期）时，采购员可见并可报价。
/// </summary>
public static class RfqDemandProtectionRules
{
    public const int MaxProtectionMinutes = 43200;

    public const int DefaultProtectionMinutes = 30;

    /// <summary>是否配置了正数保护时长（0 表示无保护期，而非关闭保护池）。</summary>
    public static bool HasProtectionPeriod(int protectionMinutes) => protectionMinutes > 0;

    /// <summary>是否已超过保护时长，或当前为无保护期（0）。</summary>
    public static bool IsProtectionExpired(DateTime itemCreateTimeUtc, int protectionMinutes, DateTime utcNow)
    {
        if (protectionMinutes <= 0)
            return true;

        return utcNow - itemCreateTimeUtc > TimeSpan.FromMinutes(protectionMinutes);
    }

    public static DateTime ProtectionCutoffUtc(int protectionMinutes, DateTime utcNow) =>
        utcNow.AddMinutes(-protectionMinutes);

    /// <summary>是否属于可参与「保护到期」池的采购员（非采购禁止、须隶属采购侧部门）。</summary>
    public static bool CanParticipateInProtectionPool(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin)
            return true;
        if (summary.PurchaseDataScope == 4)
            return false;
        return summary.BelongsToPurchaseDept;
    }

    public static bool IsAssignedToUser(RFQItem item, string userId)
    {
        var uid = userId.Trim();
        var id1 = item.AssignedPurchaserUserId1?.Trim();
        var id2 = item.AssignedPurchaserUserId2?.Trim();
        return (!string.IsNullOrEmpty(id1) && string.Equals(id1, uid, StringComparison.OrdinalIgnoreCase))
               || (!string.IsNullOrEmpty(id2) && string.Equals(id2, uid, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsAssignedToAllowedPurchasers(
        RFQItem item,
        HashSet<string>? purchaseAllow)
    {
        if (purchaseAllow == null)
            return false;

        return (!string.IsNullOrWhiteSpace(item.AssignedPurchaserUserId1)
                && purchaseAllow.Contains(item.AssignedPurchaserUserId1!))
               || (!string.IsNullOrWhiteSpace(item.AssignedPurchaserUserId2)
                   && purchaseAllow.Contains(item.AssignedPurchaserUserId2!));
    }

    public static bool IsPurchaseSideVisible(
        UserPermissionSummaryDto summary,
        RFQItem item,
        string userId,
        HashSet<string>? purchaseAllow,
        int protectionMinutes,
        DateTime utcNow)
    {
        if (summary.PurchaseDataScope == 4)
            return false;
        if (summary.PurchaseDataScope == 0)
            return true;

        var uid = userId.Trim();

        if (summary.PurchaseDataScope == 1 && IsAssignedToUser(item, uid))
            return true;

        if ((summary.PurchaseDataScope == 2 || summary.PurchaseDataScope == 3)
            && IsAssignedToAllowedPurchasers(item, purchaseAllow))
            return true;

        if (CanParticipateInProtectionPool(summary)
            && IsProtectionExpired(item.CreateTime, protectionMinutes, utcNow))
            return true;

        return false;
    }
}
