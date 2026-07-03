namespace CRM.Core.Utilities;

/// <summary>
/// 销售订单明细与报价主表关联规则（报价 → 销售 → 采购申请链路）。
/// </summary>
public static class SellOrderItemLinkRules
{
    /// <summary>前端占位：无报价时传入的全零 GUID，不视为已关联报价。</summary>
    public const string EmptyQuoteSentinel = "00000000-0000-0000-0000-000000000000";

    public const short SellOrderTypeCustomer = 1;
    public const short SellOrderTypeStocking = 2;
    public const short SellOrderTypeSample = 3;

    public static bool IsLinkedQuoteId(string? quoteId) =>
        !string.IsNullOrWhiteSpace(quoteId) &&
        !string.Equals(quoteId.Trim(), EmptyQuoteSentinel, StringComparison.OrdinalIgnoreCase);

    /// <summary>客单(1) 不允许手工添加明细；备货/样品允许。</summary>
    public static bool ShouldAllowManualAddItem(short headerType) =>
        headerType != SellOrderTypeCustomer;

    /// <summary>客单(1) 时，每条明细须有关联的报价单 ID。</summary>
    public static void ValidateCustomerOrderItems(short headerType, IReadOnlyList<string?> quoteIds)
    {
        if (headerType != SellOrderTypeCustomer)
            return;
        if (quoteIds.Count == 0)
            throw new ArgumentException("客单销售订单至少需要一条明细");
        for (var i = 0; i < quoteIds.Count; i++)
        {
            if (!IsLinkedQuoteId(quoteIds[i]))
                throw new ArgumentException($"客单销售订单的第 {i + 1} 条明细须关联报价单");
        }
    }

    /// <summary>客单销单行无报价关联时，禁止创建采购申请。</summary>
    public static void ValidatePurchaseRequisitionAllowed(short sellOrderHeaderType, string? sellOrderItemQuoteId)
    {
        if (sellOrderHeaderType != SellOrderTypeCustomer)
            return;
        if (!IsLinkedQuoteId(sellOrderItemQuoteId))
            throw new InvalidOperationException("客单销售明细须关联报价后方可申请采购");
    }
}
