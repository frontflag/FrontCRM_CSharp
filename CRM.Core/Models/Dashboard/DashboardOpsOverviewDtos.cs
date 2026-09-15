namespace CRM.Core.Models.Dashboard;

public sealed class DashboardOpsTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class DashboardLogisticsOverviewDto
{
    public int StockInItemCount { get; set; }
    public int StockOutItemCount { get; set; }
    public int CustomsDeclarationItemCount { get; set; }
    public IReadOnlyList<DashboardOpsTrendPointDto> StockInTrends { get; set; } =
        Array.Empty<DashboardOpsTrendPointDto>();
    public IReadOnlyList<DashboardOpsTrendPointDto> StockOutTrends { get; set; } =
        Array.Empty<DashboardOpsTrendPointDto>();
}

public sealed class DashboardFinanceWriteOffOverviewDto
{
    public int? PurchaseInvoiceWriteOffCount { get; set; }
    public int? ReceivableWriteOffCount { get; set; }
    public int? SellInvoiceWriteOffCount { get; set; }
}

public sealed class DashboardOpsDateRange
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public sealed class DashboardFinanceWriteOffFlags
{
    public bool IncludePurchaseInvoice { get; set; }
    public bool IncludeReceivable { get; set; }
    public bool IncludeSellInvoice { get; set; }
}
