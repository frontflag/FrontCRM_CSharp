namespace CRM.Core.Interfaces;

public sealed class CommissionPoolListQuery
{
    public string? Keyword { get; init; }
    public string? PurchasePn { get; init; }
    public string? SalesUserId { get; init; }
    public string? PurchaseUserId { get; init; }
    public string? RestrictSalesUserId { get; init; }
    public string? RestrictPurchaseUserId { get; init; }
    public bool RestrictEither { get; init; }
    public short? SalesCommissionStatus { get; init; }
    public short? PurchaseCommissionStatus { get; init; }
    public short? ReceiptProgressStatus { get; init; }
    public DateOnly? StockOutDateFrom { get; init; }
    public DateOnly? StockOutDateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class CommissionPoolListRowDto
{
    public string Id { get; init; } = string.Empty;
    public string StockOutItemId { get; init; } = string.Empty;
    public string StockOutId { get; init; } = string.Empty;
    public string StockOutCode { get; init; } = string.Empty;
    public string? StockOutItemCode { get; init; }
    public DateOnly? StockOutDate { get; init; }
    public string? SalesUserId { get; init; }
    public string? SalesUserName { get; init; }
    public string? PurchaseUserId { get; init; }
    public string? PurchaseUserName { get; init; }
    public string? PurchasePn { get; init; }
    public string? PurchaseBrand { get; init; }
    public decimal? PurchasePrice { get; init; }
    public short? PurchaseCurrency { get; init; }
    public decimal? PurchasePriceUsd { get; init; }
    public decimal? SalesPrice { get; init; }
    public short? SalesCurrency { get; init; }
    public decimal? SalesPriceUsd { get; init; }
    public int? QtyStockOut { get; init; }
    public decimal GpUsd { get; init; }
    public short ReceiptProgressStatus { get; init; }
    public DateOnly? ReceiptDate { get; init; }
    public string? SellOrderId { get; init; }
    public string? SellOrderCode { get; init; }
    public string? SellOrderItemCode { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public string? PurchaseOrderItemCode { get; init; }
    public short SalesCommissionStatus { get; init; }
    public short PurchaseCommissionStatus { get; init; }
}

public interface ICommissionPoolListService
{
    Task<CommissionPaged<CommissionPoolListRowDto>> ListAsync(
        CommissionPoolListQuery query,
        CancellationToken cancellationToken = default);
}
