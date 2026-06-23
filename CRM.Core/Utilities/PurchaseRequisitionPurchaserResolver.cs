using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;

namespace CRM.Core.Utilities;

/// <summary>采购申请默认采购员：报价主表采购员 → 需求明细分配采购员。</summary>
public static class PurchaseRequisitionPurchaserResolver
{
    public static string? ResolveFromQuote(Quote? quote, RFQItem? rfqItem)
    {
        if (quote != null && !string.IsNullOrWhiteSpace(quote.PurchaseUserId))
            return quote.PurchaseUserId.Trim();

        if (rfqItem == null) return null;
        if (!string.IsNullOrWhiteSpace(rfqItem.AssignedPurchaserUserId1))
            return rfqItem.AssignedPurchaserUserId1.Trim();
        if (!string.IsNullOrWhiteSpace(rfqItem.AssignedPurchaserUserId2))
            return rfqItem.AssignedPurchaserUserId2.Trim();
        return null;
    }
}
