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
    /// <summary>已出库：原币 Σ qty×price；折算美金 Σ qty×convert_price。</summary>
    public SalesAnalyticsMoneyDto SalesAmountStockOut { get; set; } = new();
    /// <summary>已收款：原币 Σ receipt_amount_finish；折算美金按明细价比/财务汇率。</summary>
    public SalesAnalyticsMoneyDto SalesAmountReceived { get; set; } = new();
}

/// <summary>销售分析金额：原币分档 + 折算 USD（与财务看板 MoneyDto 同形）。</summary>
public sealed class SalesAnalyticsMoneyDto
{
    public decimal? TotalUsd { get; set; }
    public IReadOnlyList<SalesAnalyticsCurrencyAmountDto> ByCurrency { get; set; } =
        Array.Empty<SalesAnalyticsCurrencyAmountDto>();
}

public sealed class SalesAnalyticsCurrencyAmountDto
{
    public short Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    /// <summary>原币金额合计。</summary>
    public decimal Amount { get; set; }
}

public sealed class SalesAnalyticsTodoDto
{
    /// <summary>待核销应收：finance_receivable Σ verified_to_be；查询日财务汇率折算 USD（对齐应收款列表看板「待核销应收款」）。</summary>
    public SalesAnalyticsMoneyDto ReceivableAmount { get; set; } = new();
    public int PendingStockOutItemCount { get; set; }
    /// <summary>待开票：明细 invoice_amount_not，按订单币别分档并以财务汇率折算 USD。</summary>
    public SalesAnalyticsMoneyDto PendingInvoiceAmount { get; set; } = new();
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
    /// <summary>已出库金额趋势（折算 USD）。</summary>
    public decimal? SalesAmountStockOut { get; set; }
    /// <summary>已收款金额趋势（折算 USD）。</summary>
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

/// <summary>出库进度（明细行）详情：三档汇总。</summary>
public sealed class SalesAnalyticsStockOutProgressSummaryItemDto
{
    public short Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Ratio { get; set; }
}

/// <summary>出库进度（明细行）详情：列表行。</summary>
public sealed class SalesAnalyticsStockOutProgressDetailItemDto
{
    public string SellOrderItemId { get; set; } = string.Empty;
    /// <summary>销售订单明细编号。</summary>
    public string SellOrderItemCode { get; set; } = string.Empty;
    public DateTime OrderCreateTime { get; set; }
    /// <summary>客户名称；无 <c>customer.info.read</c>（或 5.2.1 脱敏）时为 null。</summary>
    public string? CustomerName { get; set; }
    public string? SalesUserName { get; set; }
    public string? Pn { get; set; }
    public string? Brand { get; set; }
    public decimal Qty { get; set; }
    public short StockOutProgressStatus { get; set; }
    public string StockOutProgressLabel { get; set; } = string.Empty;
}

/// <summary>出库进度（明细行）详情弹窗数据。</summary>
public sealed class SalesAnalyticsStockOutProgressDetailDto
{
    public IReadOnlyList<SalesAnalyticsStockOutProgressSummaryItemDto> Summary { get; set; } =
        Array.Empty<SalesAnalyticsStockOutProgressSummaryItemDto>();

    public IReadOnlyList<SalesAnalyticsStockOutProgressDetailItemDto> Items { get; set; } =
        Array.Empty<SalesAnalyticsStockOutProgressDetailItemDto>();

    /// <summary>当前用户是否可查看客户名称列。</summary>
    public bool CanViewCustomer { get; set; }

    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// 销售分析「客户」Tab：成单客户维 KPI / 类型等级行业分解 / 客户 Top10。
/// 复购客户 = 成单订单数 ≥ 2 的去重客户。
/// </summary>
public sealed class SalesAnalyticsCustomerSnapshotDto
{
    /// <summary>成单客户数（周期内成单订单 customer_id 去重；与概况「销售客户数」同口径）。</summary>
    public int ApprovedCustomerCount { get; set; }

    /// <summary>复购客户数（成单订单数 ≥ 2）。</summary>
    public int RepeatCustomerCount { get; set; }
}

public sealed class SalesAnalyticsCustomerRankingsDto
{
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByAmount { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByOrderCount { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
    /// <summary>OrderCount 为复购订单数 = max(0, 成单数 − 1)。</summary>
    public IReadOnlyList<SalesAnalyticsRankingRowDto> CustomerByRepeatOrderCount { get; set; } = Array.Empty<SalesAnalyticsRankingRowDto>();
}

public sealed class SalesAnalyticsCustomerDto
{
    public SalesAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public SalesAnalyticsCustomerSnapshotDto Snapshot { get; set; } = new();
    public IReadOnlyList<SalesAnalyticsBreakdownGroupDto> Breakdowns { get; set; } = Array.Empty<SalesAnalyticsBreakdownGroupDto>();
    public SalesAnalyticsCustomerRankingsDto Rankings { get; set; } = new();
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
