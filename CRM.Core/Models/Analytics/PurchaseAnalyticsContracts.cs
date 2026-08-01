using CRM.Core.Interfaces;

namespace CRM.Core.Models.Analytics;

public sealed class PurchaseAnalyticsQueryParams
{
    public string ViewLevel { get; set; } = SalesAnalyticsViewLevels.Company;
    public string? DepartmentId { get; set; }
    public string? PurchaseUserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string GroupBy { get; set; } = "month";
}

public sealed class PurchaseAnalyticsScopeContextDto
{
    public short PurchaseDataScope { get; set; }
    public string ViewLevel { get; set; } = string.Empty;
    public string ScopeLabel { get; set; } = string.Empty;
    public string? PrimaryDepartmentId { get; set; }
    public string? PrimaryDepartmentName { get; set; }
    public IReadOnlyList<string> AllowedViewLevels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<SalesAnalyticsDepartmentOptionDto> AllowedDepartments { get; set; } = Array.Empty<SalesAnalyticsDepartmentOptionDto>();
    public IReadOnlyList<PurchaseAnalyticsPurchaseUserOptionDto> AllowedPurchaseUsers { get; set; } = Array.Empty<PurchaseAnalyticsPurchaseUserOptionDto>();
    /// <summary>个人层是否展示采购员选择（Scope=1/4 为 false）。</summary>
    public bool CanSelectPurchaseUser { get; set; }
    public bool DataFiltered { get; set; }
    public bool MaskAmounts { get; set; }
    public string? ResolvedPurchaseUserId { get; set; }
    public string? ResolvedDepartmentId { get; set; }
}

public sealed class PurchaseAnalyticsPurchaseUserOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class PurchaseAnalyticsSnapshotDto
{
    public int QuoteItemCount { get; set; }
    public int QuoteVendorCount { get; set; }
    public decimal? QuoteToPurchaseConversionRate { get; set; }
    public int PurchaseOrderItemCount { get; set; }
    public int PurchaseOrderVendorCount { get; set; }
    public decimal? PurchaseAmountApproved { get; set; }
    /// <summary>已入库金额（USD）：Σ qty_receive_total × convert_price。</summary>
    public decimal? PurchaseAmountStockIn { get; set; }
    /// <summary>已付款金额（USD）：Σ payment_amount_finish。</summary>
    public decimal? PurchaseAmountPaid { get; set; }
}

public sealed class PurchaseAnalyticsTodoDto
{
    public decimal? PayableAmount { get; set; }
    public int PendingStockInItemCount { get; set; }
}

/// <summary>采购分析「供应商」Tab：成单供应商维 KPI / 身份等级行业分解 / 供应商 Top10。</summary>
public sealed class PurchaseAnalyticsVendorSnapshotDto
{
    /// <summary>成单供应商数（周期内成单订单 vendor_id 去重；与概况「采购供应商数」同口径）。</summary>
    public int ApprovedVendorCount { get; set; }

    /// <summary>复采供应商数（成单订单数 ≥ 2）。</summary>
    public int RepeatVendorCount { get; set; }
}

public sealed class PurchaseAnalyticsVendorRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByAmount { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByOrderCount { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
    /// <summary>OrderCount 为复采订单数 = max(0, 成单数 − 1)。</summary>
    public IReadOnlyList<SalesAnalyticsRankingRowDto> VendorByRepeatOrderCount { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
}

public sealed class PurchaseAnalyticsVendorDto
{
    public PurchaseAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public PurchaseAnalyticsVendorSnapshotDto Snapshot { get; set; } = new();
    public IReadOnlyList<SalesAnalyticsBreakdownGroupDto> Breakdowns { get; set; } = Array.Empty<SalesAnalyticsBreakdownGroupDto>();
    public PurchaseAnalyticsVendorRankingsDto Rankings { get; set; } = new();
}

public sealed class PurchaseAnalyticsDashboardDto
{
    public PurchaseAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public PurchaseAnalyticsSnapshotDto Snapshot { get; set; } = new();
    public PurchaseAnalyticsTodoDto Todo { get; set; } = new();
    public SalesAnalyticsRankingsDto Rankings { get; set; } = new();
}

public sealed class PurchaseAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int QuoteItemCount { get; set; }
    public int QuoteVendorCount { get; set; }
    public int PurchaseOrderItemCount { get; set; }
    public int PurchaseOrderVendorCount { get; set; }
    public decimal? PurchaseAmountApproved { get; set; }
    public decimal? PurchaseAmountStockIn { get; set; }
    public decimal? PurchaseAmountPaid { get; set; }
    public decimal? PayableAmount { get; set; }
    public decimal? QuoteToPurchaseConversionRate { get; set; }
}

public sealed class PurchaseAnalyticsResolvedScope
{
    public UserPermissionSummaryDto Summary { get; set; } = null!;
    public PurchaseAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public string ViewLevel { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? PurchaseUserId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string GroupBy { get; set; } = "month";
    public bool MaskAmounts { get; set; }
}
