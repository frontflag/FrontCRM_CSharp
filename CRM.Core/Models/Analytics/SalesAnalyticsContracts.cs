using CRM.Core.Interfaces;

namespace CRM.Core.Models.Analytics;

public static class SalesAnalyticsViewLevels
{
    public const string Company = "company";
    public const string Department = "department";
    public const string Personal = "personal";
}

public sealed class SalesAnalyticsQueryParams
{
    public string ViewLevel { get; set; } = SalesAnalyticsViewLevels.Company;
    public string? DepartmentId { get; set; }
    public string? SalesUserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string GroupBy { get; set; } = "month";
}

public sealed class SalesAnalyticsScopeContextDto
{
    public short SaleDataScope { get; set; }
    public string ViewLevel { get; set; } = string.Empty;
    public string ScopeLabel { get; set; } = string.Empty;
    public string? PrimaryDepartmentId { get; set; }
    public string? PrimaryDepartmentName { get; set; }
    public IReadOnlyList<string> AllowedViewLevels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<SalesAnalyticsDepartmentOptionDto> AllowedDepartments { get; set; } = Array.Empty<SalesAnalyticsDepartmentOptionDto>();
    /// <summary>个人层可选业务员（Scope 0/3 等）；Scope=1 时为空。</summary>
    public IReadOnlyList<SalesAnalyticsSalesUserOptionDto> AllowedSalesUsers { get; set; } = Array.Empty<SalesAnalyticsSalesUserOptionDto>();
    public bool DataFiltered { get; set; }
    public bool MaskAmounts { get; set; }
    public string? ResolvedSalesUserId { get; set; }
    public string? ResolvedDepartmentId { get; set; }
}

public sealed class SalesAnalyticsDepartmentOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class SalesAnalyticsSalesUserOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class SalesAnalyticsSnapshotDto
{
    public int RfqItemCount { get; set; }
    public int RfqCustomerCount { get; set; }
    public decimal? RfqToSalesConversionRate { get; set; }
    public int SalesOrderItemCount { get; set; }
    public int SalesOrderCustomerCount { get; set; }
    public decimal? SalesAmountApproved { get; set; }
    /// <summary>已出库金额（本位币）：Σ qty_stock_out_actual × convert_price。</summary>
    public decimal? SalesAmountStockOut { get; set; }
    /// <summary>已收款金额（本位币）：Σ receipt_amount_finish。</summary>
    public decimal? SalesAmountReceived { get; set; }
}

public sealed class SalesAnalyticsTodoDto
{
    public decimal? ReceivableAmount { get; set; }
    public int PendingStockOutItemCount { get; set; }
    /// <summary>待开票金额：Σ invoice_amount_not（活跃订单明细）。</summary>
    public decimal? PendingInvoiceAmount { get; set; }
}

public sealed class SalesAnalyticsRankingRowDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public int OrderCount { get; set; }
}

public sealed class SalesAnalyticsRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> Primary { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> Secondary { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
}

public sealed class SalesAnalyticsDashboardDto
{
    public SalesAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public SalesAnalyticsSnapshotDto Snapshot { get; set; } = new();
    public SalesAnalyticsTodoDto Todo { get; set; } = new();
    public SalesAnalyticsRankingsDto Rankings { get; set; } = new();
}

public sealed class SalesAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int RfqItemCount { get; set; }
    /// <summary>需求客户数（周期内 rfq.customer_id 去重）。</summary>
    public int RfqCustomerCount { get; set; }
    public int SalesOrderItemCount { get; set; }
    /// <summary>销售客户数（周期内 sellorder.customer_id 去重）。</summary>
    public int SalesOrderCustomerCount { get; set; }
    public decimal? SalesAmountApproved { get; set; }
    public decimal? SalesAmountStockOut { get; set; }
    public decimal? SalesAmountReceived { get; set; }
    public decimal? ReceivableAmount { get; set; }
    public decimal? RfqToSalesConversionRate { get; set; }
}

public sealed class SalesAnalyticsBreakdownItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Ratio { get; set; }
}

public sealed class SalesAnalyticsBreakdownGroupDto
{
    public string GroupKey { get; set; } = string.Empty;
    public string GroupLabel { get; set; } = string.Empty;
    public IReadOnlyList<SalesAnalyticsBreakdownItemDto> Items { get; set; } = Array.Empty<SalesAnalyticsBreakdownItemDto>();
}

public sealed class SalesAnalyticsResolvedScope
{
    public UserPermissionSummaryDto Summary { get; set; } = null!;
    public SalesAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public string ViewLevel { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? SalesUserId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string GroupBy { get; set; } = "month";
    public bool MaskAmounts { get; set; }
}
