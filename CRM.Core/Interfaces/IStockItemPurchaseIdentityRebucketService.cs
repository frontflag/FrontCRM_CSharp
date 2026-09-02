using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>
/// 在库明细 PN/品牌变更后，对齐全库存分桶（找不到则新建），重算旧/新桶数量与备货可用量；
/// 换堆后已无在库明细且数量为 0 的旧汇总行软删。
/// </summary>
public interface IStockItemPurchaseIdentityRebucketService
{
    Task<StockItemPurchaseIdentityRebucketResult> EnsureAggregatesAsync(
        IReadOnlyList<StockItem> layers,
        CancellationToken cancellationToken = default);
}

public class StockItemPurchaseIdentityRebucketResult
{
    public int StockItemsMoved { get; set; }
    public int StockAggregatesCreated { get; set; }
    public int StockAggregatesRecalculated { get; set; }
    public int StockAggregatesRemoved { get; set; }
}
