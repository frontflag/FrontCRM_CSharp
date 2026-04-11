using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>
/// 维护销售明细扩展 <see cref="SellOrderItemExtend.PurchasedStock_AvailableQty"/>：同 PN+品牌下备货库存可用量之和。
/// </summary>
public interface ISellOrderItemPurchasedStockAvailableSyncService
{
    /// <summary>按采购侧 PN+品牌重算全系统未完成销售明细（有扩展行且出库未完成）。</summary>
    Task RecalculateByPurchasePnAndBrandAsync(string? purchasePn, string? purchaseBrand, CancellationToken cancellationToken = default);

    /// <summary>采购入库过账完成后：若为备货入库，解析 PN+品牌并触发重算。</summary>
    Task TryRecalculateFromCompletedStockInAsync(StockIn stockIn, CancellationToken cancellationToken = default);

    /// <summary>出库导致库存变更后：对涉及的备货库存行按 PN+品牌触发重算。</summary>
    Task TryRecalculateFromChangedStockInfosAsync(IEnumerable<StockInfo> changedStocks, CancellationToken cancellationToken = default);
}
