using CRM.Core.Interfaces;

namespace CRM.Core.Models.Analytics;

public static class FinanceAnalyticsAccessModes
{
    public const string Finance = "finance";
    public const string SalesPurchaseOnly = "salesPurchaseOnly";
}

public sealed class FinanceAnalyticsQueryParams
{
    public string ViewLevel { get; set; } = SalesAnalyticsViewLevels.Company;
    public string? DepartmentId { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string GroupBy { get; set; } = "month";
}

public sealed class FinanceAnalyticsCurrencyAmountDto
{
    public short Currency { get; set; }
    public string CurrencyLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class FinanceAnalyticsMoneyDto
{
    public decimal? TotalUsd { get; set; }
    public IReadOnlyList<FinanceAnalyticsCurrencyAmountDto> ByCurrency { get; set; } =
        Array.Empty<FinanceAnalyticsCurrencyAmountDto>();
}

public sealed class FinanceAnalyticsScopeContextDto
{
    public short FinanceDataScope { get; set; }
    public short SaleDataScope { get; set; }
    public short PurchaseDataScope { get; set; }
    public string AccessMode { get; set; } = FinanceAnalyticsAccessModes.Finance;
    public string ViewLevel { get; set; } = string.Empty;
    public string ScopeLabel { get; set; } = string.Empty;
    public string? PrimaryDepartmentId { get; set; }
    public string? PrimaryDepartmentName { get; set; }
    public IReadOnlyList<string> AllowedViewLevels { get; set; } = Array.Empty<string>();
    public IReadOnlyList<SalesAnalyticsDepartmentOptionDto> AllowedDepartments { get; set; } = Array.Empty<SalesAnalyticsDepartmentOptionDto>();
    public bool DataFiltered { get; set; }
    public bool MaskAmounts { get; set; }
    public string ExchangeRateHint { get; set; } = "美元折算按查询日财务参数汇率";
    public string? ResolvedOwnerUserId { get; set; }
    public string? ResolvedDepartmentId { get; set; }
}

public sealed class FinanceAnalyticsTodoDto
{
    public FinanceAnalyticsMoneyDto PayableAmount { get; set; } = new();
    public FinanceAnalyticsMoneyDto ReceivableAmount { get; set; } = new();
    public FinanceAnalyticsMoneyDto PendingPurchaseInvoiceAmount { get; set; } = new();
    public FinanceAnalyticsMoneyDto PendingSellInvoiceAmount { get; set; } = new();
}

public sealed class FinanceAnalyticsCompletedDto
{
    public FinanceAnalyticsMoneyDto PaidAmount { get; set; } = new();
    public FinanceAnalyticsMoneyDto ReceivedAmount { get; set; } = new();
    public FinanceAnalyticsMoneyDto IssuedPurchaseInvoiceAmount { get; set; } = new();
    public FinanceAnalyticsMoneyDto IssuedSellInvoiceAmount { get; set; } = new();
}

public sealed class FinanceAnalyticsDashboardDto
{
    public FinanceAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public FinanceAnalyticsTodoDto Todo { get; set; } = new();
    public FinanceAnalyticsCompletedDto Completed { get; set; } = new();
}

public sealed class FinanceAnalyticsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public FinanceAnalyticsMoneyDto? PaidAmount { get; set; }
    public FinanceAnalyticsMoneyDto? ReceivedAmount { get; set; }
    public FinanceAnalyticsMoneyDto? PayableAmount { get; set; }
    public FinanceAnalyticsMoneyDto? ReceivableAmount { get; set; }
}

public sealed class FinanceAnalyticsResolvedScope
{
    public UserPermissionSummaryDto Summary { get; set; } = null!;
    public FinanceAnalyticsScopeContextDto ScopeContext { get; set; } = new();
    public string AccessMode { get; set; } = FinanceAnalyticsAccessModes.Finance;
    public string ViewLevel { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string GroupBy { get; set; } = "month";
    public bool MaskAmounts { get; set; }
    public HashSet<string> SalesPurchaseLensUserIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal UsdToCny { get; set; }
    public decimal UsdToHkd { get; set; }
    public decimal UsdToEur { get; set; }
}
