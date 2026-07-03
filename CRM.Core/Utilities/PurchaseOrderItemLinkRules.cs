namespace CRM.Core.Utilities;

/// <summary>
/// 采购订单明细与销售订单明细关联规则（以销定采链路）。
/// </summary>
public static class PurchaseOrderItemLinkRules
{
    /// <summary>前端占位：无销售明细时传入的全零 GUID，不视为以销定采。</summary>
    public const string EmptySellOrderItemSentinel = "00000000-0000-0000-0000-000000000000";

    public const short PurchaseOrderTypeCustomer = 1;
    public const short PurchaseOrderTypeStocking = 2;
    public const short PurchaseOrderTypeSample = 3;

    public static bool IsLinkedSellOrderLine(string? sellOrderItemId) =>
        !string.IsNullOrWhiteSpace(sellOrderItemId) &&
        !string.Equals(sellOrderItemId.Trim(), EmptySellOrderItemSentinel, StringComparison.OrdinalIgnoreCase);

    /// <summary>有销售明细关联 → 客单采购(1)；否则备货(2)；无销售关联且请求为样品 → 3。</summary>
    public static short ResolveHeaderType(short requestedType, IEnumerable<string?> sellOrderItemIds)
    {
        if (sellOrderItemIds.Any(IsLinkedSellOrderLine))
            return PurchaseOrderTypeCustomer;
        if (requestedType == PurchaseOrderTypeSample)
            return PurchaseOrderTypeSample;
        return PurchaseOrderTypeStocking;
    }

    /// <summary>客单采购(1) 时，每条明细须有关联的销售订单明细 ID。</summary>
    public static void ValidateCustomerOrderItems(short headerType, IReadOnlyList<string?> sellOrderItemIds)
    {
        if (headerType != PurchaseOrderTypeCustomer)
            return;
        if (sellOrderItemIds.Count == 0)
            throw new ArgumentException("客单采购订单至少需要一条明细");
        for (var i = 0; i < sellOrderItemIds.Count; i++)
        {
            if (!IsLinkedSellOrderLine(sellOrderItemIds[i]))
                throw new ArgumentException($"客单采购订单的第 {i + 1} 条明细须关联销售订单明细");
        }
    }

    /// <summary>入库过账：客单采购头但行无销售关联时，库存按备货类型入账。</summary>
    public static short ResolveInboundStockType(short poHeaderType, string? lineSellOrderItemId, string? poLineSellOrderItemId)
    {
        var headerType = poHeaderType is >= 1 and <= 3 ? poHeaderType : PurchaseOrderTypeCustomer;
        if (headerType != PurchaseOrderTypeCustomer)
            return headerType;
        if (IsLinkedSellOrderLine(lineSellOrderItemId) || IsLinkedSellOrderLine(poLineSellOrderItemId))
            return PurchaseOrderTypeCustomer;
        return PurchaseOrderTypeStocking;
    }
}
