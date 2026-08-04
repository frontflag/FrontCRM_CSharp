namespace CRM.Core.Utilities;

/// <summary>销售明细利润字段的展示口径（列表/API 与前端一致）。</summary>
public static class SellOrderItemProfitDisplay
{
    /// <summary>
    /// 出库利润率：扩展表在出库成本为 0 时存 0；对外展示为 null（前端显示 —）。
    /// 出库利润为负且利润率为 0 时保留 0（有成本、收入为 0 等可计算情形）。
    /// </summary>
    public static decimal? ResolveProfitOutRateBizForDisplay(decimal storedRate, decimal profitOutBizUsd)
    {
        if (storedRate == 0m && profitOutBizUsd >= 0m)
            return null;
        return storedRate;
    }

    /// <summary>预计销售利润率：收入 ÷ 成本基数；成本 ≤ 0 时返回 null。</summary>
    public static decimal? ResolveSalesExpectedRateForDisplay(decimal revenueUsd, decimal costUsd)
    {
        if (costUsd <= 0m)
            return null;
        return Math.Round(revenueUsd / costUsd, 6, MidpointRounding.AwayFromZero);
    }

    /// <summary>报价/通用利润率：成本为 0 且利润 ≥ 0 时返回 null。</summary>
    public static decimal? ResolveStoredRateForDisplay(decimal storedRate, decimal costUsd, decimal profitUsd)
    {
        if (costUsd <= 0m && profitUsd >= 0m)
            return null;
        return storedRate;
    }
}
