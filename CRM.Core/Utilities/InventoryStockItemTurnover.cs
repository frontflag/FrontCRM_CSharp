namespace CRM.Core.Utilities;

/// <summary>库存明细看板周转天数：在库数量 / 近 30 天出库数量 × 30。</summary>
public static class InventoryStockItemTurnover
{
    /// <summary>任一数量 ≤ 0 时返回 null（界面显示「—」）。</summary>
    public static decimal? Days(int onHandQty, int outboundQtyLast30)
    {
        if (onHandQty <= 0 || outboundQtyLast30 <= 0)
            return null;
        return Math.Round((decimal)onHandQty / outboundQtyLast30 * 30m, 1, MidpointRounding.AwayFromZero);
    }
}
