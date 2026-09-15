using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>
/// 维护销售明细扩展 <see cref="SellOrderItemExtend.PurchasedStock_AvailableQty"/>：同 PN+品牌下备货库存可用量之和
/// （入库过账、销售出库、销售明细创建/替换、操作面板手工刷新后触发；重算后落库）。
/// </summary>
public interface ISellOrderItemPurchasedStockAvailableSyncService
{
    /// <summary>按采购侧 PN+品牌重算全系统未完成销售明细（有扩展行且出库未完成）。</summary>
    Task RecalculateByPurchasePnAndBrandAsync(string? purchasePn, string? purchaseBrand, CancellationToken cancellationToken = default);

    /// <summary>
    /// 一次性加载库存与销售明细，按现网口径重算全部出库未完成行的备货可用量快照并落库。
    /// 供 Debug 全量对齐；不替代入库/出库/建单/面板四处触发。
    /// </summary>
    Task<PurchasedStockAvailableBatchRecalcResult> RecalculateAllUnfinishedSellLinesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>采购入库过账完成后：若为备货入库，解析 PN+品牌并触发重算。</summary>
    Task TryRecalculateFromCompletedStockInAsync(StockIn stockIn, CancellationToken cancellationToken = default);

    /// <summary>出库导致库存汇总变更后：对涉及的备货库存行按 PN+品牌触发重算。</summary>
    Task TryRecalculateFromChangedStockInfosAsync(IEnumerable<StockInfo> changedStocks, CancellationToken cancellationToken = default);

    /// <summary>出库导致在库明细层变更后：对涉及的备货 layer 按 PN+品牌触发重算。</summary>
    Task TryRecalculateFromChangedStockItemsAsync(IEnumerable<StockItem> changedStockItems, CancellationToken cancellationToken = default);
}

/// <summary>Debug：全量重算出库未完成销售明细备货可用量快照的结果。</summary>
public sealed class PurchasedStockAvailableBatchRecalcResult
{
    /// <summary>扫描的销售明细行数（仓储默认已排除软删）。</summary>
    public int TotalLines { get; set; }

    /// <summary>出库未完成且型号/品牌有效、有扩展行、纳入对池的行数。</summary>
    public int CandidateLines { get; set; }

    /// <summary>跳过：明细已取消或数量 ≤ 0。</summary>
    public int SkippedCancelledOrQty { get; set; }

    /// <summary>跳过：PN 或品牌为空。</summary>
    public int SkippedNoPnBrand { get; set; }

    /// <summary>跳过：出库进度已为完成。</summary>
    public int SkippedOutboundComplete { get; set; }

    /// <summary>跳过：无扩展行。</summary>
    public int SkippedNoExtend { get; set; }

    /// <summary>候选行中快照已与当前备货池一致、未写库。</summary>
    public int UnchangedCount { get; set; }

    /// <summary>实际改写快照的行数。</summary>
    public int UpdatedCount { get; set; }

    /// <summary>快照被调高的行数（含 0 → 有货）。</summary>
    public int IncreasedCount { get; set; }

    /// <summary>快照被调低的行数（含有数 → 0）。</summary>
    public int DecreasedCount { get; set; }

    /// <summary>变更明细业务编号（最多 50 条）。</summary>
    public List<string> ChangedLineCodes { get; set; } = new();
}
