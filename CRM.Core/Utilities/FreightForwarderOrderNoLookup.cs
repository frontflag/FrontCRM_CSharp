using CRM.Core.Constants;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Utilities;

/// <summary>货代单号：由采购订单头/明细关联解析。</summary>
public static class FreightForwarderOrderNoLookup
{
    public static string? FromPurchaseOrderId(
        string? purchaseOrderId,
        IReadOnlyDictionary<string, PurchaseOrder> poById)
    {
        var id = purchaseOrderId?.Trim();
        if (string.IsNullOrEmpty(id) || !poById.TryGetValue(id, out var po))
            return null;
        return PurchaseOrderFreightForwarderOrderNoRules.Normalize(po.FreightForwarderOrderNo);
    }

    public static string? FromPurchaseOrderItemId(
        string? purchaseOrderItemId,
        IReadOnlyDictionary<string, PurchaseOrderItem> poiById,
        IReadOnlyDictionary<string, PurchaseOrder> poById)
    {
        var poiId = purchaseOrderItemId?.Trim();
        if (string.IsNullOrEmpty(poiId) || !poiById.TryGetValue(poiId, out var poi))
            return null;
        return FromPurchaseOrderId(poi.PurchaseOrderId, poById);
    }
}
