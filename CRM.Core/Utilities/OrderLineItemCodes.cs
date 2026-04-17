namespace CRM.Core.Utilities;

public static class OrderLineItemCodes
{
    public static string Sell(string? sellOrderCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(sellOrderCode) ? $"{sellOrderCode}-{seq}" : string.Empty;

    public static string Purchase(string? purchaseOrderCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(purchaseOrderCode) ? $"{purchaseOrderCode}-{seq}" : string.Empty;

    public static string StockIn(string? stockInCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(stockInCode) ? $"{stockInCode}-{seq}" : string.Empty;
}
