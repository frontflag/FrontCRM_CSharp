namespace CRM.Core.Models.Analytics;

/// <summary>库存明细列表看板 KPI 快照。</summary>
public sealed class InventoryStockItemListAnalyticsSnapshotDto
{
    public int OnHandQty { get; set; }
    public IReadOnlyList<InventoryOnHandListAnalyticsCurrencyLineDto> CurrencyLines { get; set; } =
        Array.Empty<InventoryOnHandListAnalyticsCurrencyLineDto>();
    /// <summary>周转天数；在库或近 30 天出库为 0 时为 null。</summary>
    public decimal? TurnoverDays { get; set; }
    /// <summary>呆滞在库数量（PCS），非层数。</summary>
    public int StagnantQty { get; set; }
}

public sealed class InventoryStockItemListAnalyticsDashboardDto
{
    public InventoryOnHandListAnalyticsContextDto Context { get; set; } = new();
    public InventoryStockItemListAnalyticsSnapshotDto Snapshot { get; set; } = new();
}
