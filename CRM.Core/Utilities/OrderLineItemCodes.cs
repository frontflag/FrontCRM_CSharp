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

    /// <summary>出库明细业务编号：<c>{出库单号}-{行序号}</c>。</summary>
    public static string StockOut(string? stockOutCode, int seq) =>
        seq > 0 && !string.IsNullOrWhiteSpace(stockOutCode) ? $"{stockOutCode}-{seq}" : string.Empty;

    /// <summary>写入出库明细时必须有号；单号无效则抛错，避免落空号。</summary>
    public static string RequireStockOut(string? stockOutCode, int seq)
    {
        var code = StockOut(stockOutCode, seq);
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("出库明细单号生成失败：出库单号无效");
        return code;
    }
}
