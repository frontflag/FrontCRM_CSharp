using CRM.Core.Interfaces;

namespace CRM.Core.Models.Analytics;

public static class LogisticsAnalyticsAccessModes
{
    public const string Logistics = "logistics";
    public const string SalesPurchaseOnly = "salesPurchaseOnly";
}

public static class LogisticsAnalyticsInventoryTypes
{
    public const string All = "all";
    public const string CustomerOrder = "customerOrder";
    public const string PurchaseStock = "purchaseStock";
}

public static class LogisticsAnalyticsMatrixSubjects
{
    public const string Salesperson = "salesperson";
    public const string Vendor = "vendor";
    public const string Purchaser = "purchaser";
    public const string Brand = "brand";

    public static bool IsValid(string? subject) =>
        subject is Salesperson or Vendor or Purchaser or Brand;
}

public sealed class LogisticsAnalyticsQueryParams
{
    public string ViewLevel { get; set; } = SalesAnalyticsViewLevels.Company;
    public string? DepartmentId { get; set; }
    public string? OwnerUserId { get; set; }
    public string InventoryType { get; set; } = LogisticsAnalyticsInventoryTypes.All;
    public string? MatrixSubject { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    /// <summary>趋势区间结束日（出入库概况、入库数量趋势）。未传时回落 <see cref="DateTo"/>。</summary>
    public DateTime? TrendDateTo { get; set; }
    public string GroupBy { get; set; } = "month";
    public string? WarehouseId { get; set; }
}

public sealed class LogisticsAnalyticsSubjectCountsDto
{
    public int Customer { get; set; }
    public int Salesperson { get; set; }
    public int Vendor { get; set; }
    public int Purchaser { get; set; }
    public int Brand { get; set; }
}

public sealed class LogisticsAnalyticsScopeContextDto
{
    public short LogisticsDataScope { get; set; }
    public short SaleDataScope { get; set; }
    public short PurchaseDataScope { get; set; }
    public string AccessMode { get; set; } = LogisticsAnalyticsAccessModes.Logistics;
    public string ViewLevel { get; set; } = string.Empty;
    public string ScopeLabel { get; set; } = string.Empty;
    public string InventoryType { get; set; } = LogisticsAnalyticsInventoryTypes.All;
    public string? PrimaryDepartmentId { get; set; }
    public string? PrimaryDepartmentName { get; set; }
    public IReadOnlyList<string> AllowedViewLevels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<SalesAnalyticsDepartmentOptionDto> AllowedDepartments { get; set; } = Array.Empty<SalesAnalyticsDepartmentOptionDto>();
    public bool DataFiltered { get; set; }
    public bool MaskAmounts { get; set; }
    /// <summary>出库金额脱敏（521 或无 <c>sales.amount.read</c>）。与采购侧 <see cref="MaskAmounts"/> 独立。</summary>
    public bool MaskSalesAmounts { get; set; }
    public string? ResolvedOwnerUserId { get; set; }
    public string? ResolvedDepartmentId { get; set; }
}

public sealed class LogisticsAnalyticsSnapshotDto
{
    public string InventoryType { get; set; } = LogisticsAnalyticsInventoryTypes.All;
    public int OnHandQty { get; set; }
    public decimal? OnHandAmountUsd { get; set; }
    public decimal? WeightedAvgAgeDays { get; set; }
    public LogisticsAnalyticsSubjectCountsDto SubjectCounts { get; set; } = new();
}

public sealed class LogisticsAnalyticsTodoDto
{
    public int PendingStockInQty { get; set; }
}

/// <summary>
/// 出入库概况：趋势区间内仓库单据行金额（方案 B：过账 USD 快照 × 行数量）。
/// </summary>
public sealed class LogisticsAnalyticsFlowDto
{
    public SalesAnalyticsMoneyDto StockInAmount { get; set; } = new();
    public SalesAnalyticsMoneyDto StockOutAmount { get; set; } = new();
}

public sealed class LogisticsAnalyticsDashboardDto
{
    public LogisticsAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public LogisticsAnalyticsSnapshotDto Snapshot { get; set; } = new();
    public LogisticsAnalyticsTodoDto Todo { get; set; } = new();
    public LogisticsAnalyticsFlowDto Flow { get; set; } = new();
    public SalesAnalyticsRankingsDto Rankings { get; set; } = new();
}

public sealed class LogisticsAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    /// <summary>趋势区间内已过账采购入库行数量（与 <see cref="LogisticsAnalyticsFlowDto.StockInAmount"/> 同窗同单据）。</summary>
    public int StockInQty { get; set; }
    /// <summary>趋势区间内出库完成的销售出库行数量（与 <see cref="LogisticsAnalyticsFlowDto.StockOutAmount"/> 同窗同单据）。</summary>
    public int StockOutQty { get; set; }
    public int PendingStockInQty { get; set; }
}

public sealed class LogisticsAnalyticsMatrixChildDto
{
    public string SubjectKey { get; set; } = string.Empty;
    public string SubjectLabel { get; set; } = string.Empty;
    public int OnHandQty { get; set; }
    public decimal? OnHandAmountUsd { get; set; }
    public decimal? WeightedAvgAgeDays { get; set; }
}

public sealed class LogisticsAnalyticsMatrixRowDto
{
    public string? AnchorCustomerId { get; set; }
    public string AnchorCustomerName { get; set; } = string.Empty;
    public int OnHandQty { get; set; }
    public decimal? OnHandAmountUsd { get; set; }
    public decimal? WeightedAvgAgeDays { get; set; }
    public IReadOnlyList<LogisticsAnalyticsMatrixChildDto> Children { get; set; } = Array.Empty<LogisticsAnalyticsMatrixChildDto>();
}

public sealed class LogisticsAnalyticsCustomerMatrixDto
{
    public string InventoryType { get; set; } = LogisticsAnalyticsInventoryTypes.All;
    public string MatrixSubject { get; set; } = string.Empty;
    public IReadOnlyList<LogisticsAnalyticsMatrixRowDto> Rows { get; set; } = Array.Empty<LogisticsAnalyticsMatrixRowDto>();
}

public sealed class LogisticsAnalyticsResolvedScope
{
    public UserPermissionSummaryDto Summary { get; set; } = null!;
    public LogisticsAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public string AccessMode { get; set; } = LogisticsAnalyticsAccessModes.Logistics;
    public string ViewLevel { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? OwnerUserId { get; set; }
    public string InventoryType { get; set; } = LogisticsAnalyticsInventoryTypes.All;
    public string? MatrixSubject { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    /// <summary>趋势区间结束日（含当日）；出入库概况与入库数量趋势使用。</summary>
    public DateTime TrendDateTo { get; set; }
    public string GroupBy { get; set; } = "month";
    public string? WarehouseId { get; set; }
    public bool MaskAmounts { get; set; }
    public bool MaskSalesAmounts { get; set; }
    public HashSet<string> SalesPurchaseLensUserIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
