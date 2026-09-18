namespace CRM.Core.Utilities;

/// <summary>
/// 采购订单明细列表「采购利润」：
/// (销售折算美金单价 − 采购折算美金单价) × 采购明细数量。
/// </summary>
public static class PurchaseOrderItemPurchaseProfitCalc
{
    /// <summary>
    /// 任一侧折算美金单价缺失或 ≤ 0 时返回 null（界面「—」）。
    /// 结果四舍五入到 2 位（与其它利润 USD 列一致）。
    /// </summary>
    public static decimal? Compute(decimal? sellConvertUsdUnitPrice, decimal? purchaseConvertUsdUnitPrice, decimal qty)
    {
        if (sellConvertUsdUnitPrice is not > 0m || purchaseConvertUsdUnitPrice is not > 0m)
            return null;
        return Math.Round(
            (sellConvertUsdUnitPrice.Value - purchaseConvertUsdUnitPrice.Value) * qty,
            2,
            MidpointRounding.AwayFromZero);
    }
}
