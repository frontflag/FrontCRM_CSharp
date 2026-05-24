using CRM.Core.Models.Inventory;

namespace CRM.Core.Utilities;

/// <summary>出库/拣货确认时的汇总层与在库明细层数量变更（与 <see cref="Services.StockOutService"/> 原逻辑一致）。</summary>
public static class InventoryStockOutboundMutation
{
    /// <summary>扣减可用量并计入已出库量（生成拣货单、执行出库共用）。</summary>
    public static void ApplyTake(StockInfo stock, StockItem? layer, int takeQty)
    {
        if (takeQty <= 0)
            return;

        stock.QtySales += takeQty;
        stock.QtyRepertoryAvailable -= takeQty;
        stock.QtySales -= takeQty;
        stock.QtyOccupy += takeQty;
        stock.QtyStockOut += takeQty;
        stock.QtyOccupy -= takeQty;
        stock.QtyRepertory = stock.Qty - stock.QtyStockOut;
        stock.QtyRepertoryAvailable = stock.QtyRepertory - stock.QtyOccupy - stock.QtySales;

        if (layer == null)
            return;

        layer.QtySales += takeQty;
        layer.QtyRepertoryAvailable -= takeQty;
        layer.QtySales -= takeQty;
        layer.QtyOccupy += takeQty;
        layer.QtyStockOut += takeQty;
        layer.QtyOccupy -= takeQty;
        layer.QtyRepertory = layer.QtyInbound - layer.QtyStockOut;
        layer.QtyRepertoryAvailable = layer.QtyRepertory - layer.QtyOccupy - layer.QtySales;
        layer.ModifyTime = DateTime.UtcNow;
    }

    /// <summary>删除/重保存拣货单时归还库存（与 <see cref="ApplyTake"/> 相反）。</summary>
    public static void ApplyRestore(StockInfo stock, StockItem? layer, int takeQty)
    {
        if (takeQty <= 0)
            return;

        stock.QtyStockOut -= takeQty;
        if (stock.QtyStockOut < 0)
            stock.QtyStockOut = 0;
        stock.QtyRepertory = stock.Qty - stock.QtyStockOut;
        stock.QtyRepertoryAvailable = stock.QtyRepertory - stock.QtyOccupy - stock.QtySales;

        if (layer == null)
            return;

        layer.QtyStockOut -= takeQty;
        if (layer.QtyStockOut < 0)
            layer.QtyStockOut = 0;
        layer.QtyRepertory = layer.QtyInbound - layer.QtyStockOut;
        layer.QtyRepertoryAvailable = layer.QtyRepertory - layer.QtyOccupy - layer.QtySales;
        layer.ModifyTime = DateTime.UtcNow;
    }

    /// <summary>拣货保存已扣库存时，执行出库不再重复扣减。</summary>
    public static bool IsInventoryAlreadyAppliedAtPick(PickingTaskItem pickItem, StockItem layer, int takeQty)
    {
        if (takeQty <= 0)
            return false;
        if (pickItem.PickedQty < takeQty || pickItem.PlanQty != takeQty)
            return false;
        return layer.QtyStockOut >= takeQty;
    }
}
