namespace CRM.Infrastructure.Analytics;

internal sealed class StockAnalyticsRow
{
    public int Qty { get; init; }
    public decimal AmountUsd { get; init; }
    public DateTime StockInDate { get; init; }
    public int AgeDays { get; set; }
    public string? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? SalespersonId { get; init; }
    public string? SalespersonName { get; init; }
    public string? VendorId { get; init; }
    public string? VendorName { get; init; }
    public string? PurchaserId { get; init; }
    public string? PurchaserName { get; init; }
    public string? Brand { get; init; }
}
