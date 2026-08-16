using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>
/// 以销售订单行当前单价 / 币别 / 折算美金为准，无状态门控覆盖下游销售价快照。
/// </summary>
public interface ISalesOrderSalesPriceDownstreamSyncService
{
    /// <param name="items">本单销售明细（已加载的当前价）。</param>
    Task<SalesOrderSalesPriceDownstreamSyncResult> ApplyAsync(
        IReadOnlyList<SellOrderItem> items,
        CancellationToken cancellationToken = default);
}

public class SalesOrderSalesPriceDownstreamSyncResult
{
    public int PackingItemExtendsUpdated { get; set; }
    public int StockItemsUpdated { get; set; }
    public int StockOutItemExtendsUpdated { get; set; }
    public int StockOutHeadersUpdated { get; set; }
    public int ReceivablesUpdated { get; set; }
    public List<SalesOrderSalesPriceLineChangeDto> LineChanges { get; set; } = new();
    public List<SalesOrderReceivableAmountWarningDto> ReceivableWarnings { get; set; } = new();

    public bool HasUpdates =>
        PackingItemExtendsUpdated > 0
        || StockItemsUpdated > 0
        || StockOutItemExtendsUpdated > 0
        || StockOutHeadersUpdated > 0
        || ReceivablesUpdated > 0
        || LineChanges.Count > 0
        || ReceivableWarnings.Count > 0;
}
