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

    /// <summary>装箱明细业务编号：<c>{装箱单号}-{行序号}</c>。</summary>
    public static string PackingItem(string? packingCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(packingCode) ? $"{packingCode}-{seq}" : string.Empty;

    /// <summary>拣货明细业务编号（无装箱明细关联时的回退）：<c>{拣货任务号}-{行序号}</c>。</summary>
    public static string PickingTaskItem(string? taskCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(taskCode) ? $"{taskCode}-{seq}" : string.Empty;
}
