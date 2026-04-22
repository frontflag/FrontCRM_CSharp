namespace CRM.Core.Utilities;

public static class OrderLineItemCodes
{
    public static string Sell(string? sellOrderCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(sellOrderCode) ? $"{sellOrderCode}-{seq}" : string.Empty;

    public static string Purchase(string? purchaseOrderCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(purchaseOrderCode) ? $"{purchaseOrderCode}-{seq}" : string.Empty;

    public static string StockIn(string? stockInCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(stockInCode) ? $"{stockInCode}-{seq}" : string.Empty;

    /// <summary>在库明细业务编号（与分桶 <c>StockCode</c> 一致：<c>{StockCode}-{行序号}</c>，规则同 <see cref="StockIn"/>）。</summary>
    public static string StockItemLine(string? stockCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(stockCode) ? $"{stockCode}-{seq}" : string.Empty;
}
