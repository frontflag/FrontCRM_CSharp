namespace CRM.Core.Utilities;

/// <summary>预计销售利润成本来源（绩效公式标注 / API <c>salesExpectedCostSource</c>）。</summary>
public static class SellOrderSalesExpectedCostSources
{
    public const string None = "none";
    public const string PurchaseOrder = "po";
    public const string Stocking = "stocking";
    public const string Quote = "quote";
}

/// <summary>预计销售利润瀑布计算结果。</summary>
public sealed class SellOrderSalesExpectedProfitResult
{
    public string CostSource { get; init; } = SellOrderSalesExpectedCostSources.None;

    /// <summary>成本基数（USD）；<see cref="SellOrderSalesExpectedCostSources.None"/> 时为 0。</summary>
    public decimal CostUsd { get; init; }

    /// <summary>利润 USD；无可用成本来源时为 null（界面「-」）。</summary>
    public decimal? ProfitUsd { get; init; }

    /// <summary>收入 ÷ 成本；成本 ≤ 0 或无来源时为 null。</summary>
    public decimal? ProfitRate { get; init; }

    /// <summary>写入扩展表的利润（无来源时存 0）。</summary>
    public decimal ProfitUsdForStorage => ProfitUsd ?? 0m;
}

/// <summary>
/// 预计销售利润成本瀑布：
/// 1) 本行有任一采购明细 → 全部 PO 明细折 USD（不论状态，价可为 0）→ 标注「采购成本」；
/// 2) 否则备货已覆盖整行 qty → 备货采购成本（优先出库批次，否则拣货备货层）→「备货采购成本」；
/// 3) 否则有报价折成本 →「报价成本」；
/// 4) 否则无预计销售利润。
/// </summary>
public static class SellOrderSalesExpectedProfitCalc
{
    /// <param name="hasPoItems">本行是否存在任一 <c>purchaseorderitem</c>。</param>
    /// <param name="poCostUsdTotal">全部 PO 明细折 USD 合计（与 <c>PoCostUsdTotal</c> 同口径）。</param>
    /// <param name="stockingCovered">备货已用数量是否 ≥ 销售行 qty。</param>
    /// <param name="stockingUnitCostUsd">备货加权采购单价 USD（覆盖时有效）。</param>
    /// <param name="quoteConvertCost">报价折 USD 单价快照。</param>
    public static SellOrderSalesExpectedProfitResult Compute(
        decimal revenueUsd,
        decimal lineQty,
        bool hasPoItems,
        decimal poCostUsdTotal,
        bool stockingCovered,
        decimal stockingUnitCostUsd,
        decimal quoteConvertCost)
    {
        if (hasPoItems)
            return FromCost(SellOrderSalesExpectedCostSources.PurchaseOrder, revenueUsd, poCostUsdTotal);

        if (stockingCovered && lineQty > 0m)
        {
            var stockingCost = Math.Round(lineQty * stockingUnitCostUsd, 2, MidpointRounding.AwayFromZero);
            return FromCost(SellOrderSalesExpectedCostSources.Stocking, revenueUsd, stockingCost);
        }

        if (quoteConvertCost > 0m && lineQty > 0m)
        {
            var quoteCost = Math.Round(lineQty * quoteConvertCost, 2, MidpointRounding.AwayFromZero);
            return FromCost(SellOrderSalesExpectedCostSources.Quote, revenueUsd, quoteCost);
        }

        return new SellOrderSalesExpectedProfitResult
        {
            CostSource = SellOrderSalesExpectedCostSources.None,
            CostUsd = 0m,
            ProfitUsd = null,
            ProfitRate = null
        };
    }

    /// <summary>
    /// 由出库批次与备货拣货用量解析是否覆盖整行及加权单价。
    /// 优先：出库批次数量 ≥ 行 qty 且有可用批次价 → 批次成本/批次量；
    /// 否则：备货拣货量 ≥ 行 qty 且拣货成本量 &gt; 0 → 拣货成本/拣货量。
    /// </summary>
    public static (bool Covered, decimal UnitCostUsd) ResolveStockingUnitCost(
        decimal lineQty,
        decimal outboundQty,
        decimal outboundCostUsd,
        decimal stockingUsedQty,
        decimal stockingPickCostUsd)
    {
        if (lineQty <= 0m)
            return (false, 0m);

        if (outboundQty + 1e-9m >= lineQty && outboundQty > 0m)
        {
            // 允许单价为 0（与 PO 价为 0 同策略：仍走备货档）
            var unit = Math.Round(outboundCostUsd / outboundQty, 6, MidpointRounding.AwayFromZero);
            return (true, unit);
        }

        if (stockingUsedQty + 1e-9m >= lineQty && stockingUsedQty > 0m)
        {
            var unit = Math.Round(stockingPickCostUsd / stockingUsedQty, 6, MidpointRounding.AwayFromZero);
            return (true, unit);
        }

        return (false, 0m);
    }

    private static SellOrderSalesExpectedProfitResult FromCost(string source, decimal revenueUsd, decimal costUsd)
    {
        var cost = Math.Round(costUsd, 2, MidpointRounding.AwayFromZero);
        var profit = Math.Round(revenueUsd - cost, 2, MidpointRounding.AwayFromZero);
        decimal? rate = cost > 0m
            ? Math.Round(revenueUsd / cost, 6, MidpointRounding.AwayFromZero)
            : null;
        return new SellOrderSalesExpectedProfitResult
        {
            CostSource = source,
            CostUsd = cost,
            ProfitUsd = profit,
            ProfitRate = rate
        };
    }
}
