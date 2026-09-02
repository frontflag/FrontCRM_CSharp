using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class PurchaseOrderRefreshCompletedGateService : IPurchaseOrderRefreshCompletedGateService
{
    private const short ArrivalStockedIn = 100;
    private const int MaxLabels = 12;

    private readonly IPurchaseQuoterPoolService _purchaseParams;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<StockInNotify> _notifyRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<CustomsDeclarationItem> _customsItemRepo;
    private readonly IRepository<CustomsDeclaration> _customsRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutExtendRepo;

    public PurchaseOrderRefreshCompletedGateService(
        IPurchaseQuoterPoolService purchaseParams,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<StockInNotify> notifyRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItemExtend> stockInItemExtendRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<Packing> packingRepo,
        IRepository<CustomsDeclarationItem> customsItemRepo,
        IRepository<CustomsDeclaration> customsRepo,
        IRepository<StockOutItemExtend> stockOutExtendRepo)
    {
        _purchaseParams = purchaseParams;
        _poItemRepo = poItemRepo;
        _notifyRepo = notifyRepo;
        _stockInRepo = stockInRepo;
        _stockInItemExtendRepo = stockInItemExtendRepo;
        _stockItemRepo = stockItemRepo;
        _packingItemRepo = packingItemRepo;
        _packingRepo = packingRepo;
        _customsItemRepo = customsItemRepo;
        _customsRepo = customsRepo;
        _stockOutExtendRepo = stockOutExtendRepo;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderRefreshCompletedPreview> PreviewAsync(
        string purchaseOrderId,
        PurchaseOrderRefreshFacet facet,
        CancellationToken cancellationToken = default)
    {
        var result = new PurchaseOrderRefreshCompletedPreview
        {
            Facet = facet.ToApiValue()
        };
        if (facet is PurchaseOrderRefreshFacet.Status or PurchaseOrderRefreshFacet.Vendor)
        {
            result.CanProceed = true;
            result.AllowCompletedParam = true;
            return result;
        }

        var facets = await _purchaseParams.GetRefreshCompletedFacetsAsync(cancellationToken);
        result.AllowCompletedParam = facets.Allows(facet);

        var orderId = purchaseOrderId.Trim();
        var lineIds = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == orderId))
            .Select(x => x.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (lineIds.Count == 0)
        {
            result.CanProceed = true;
            return result;
        }

        var labels = new List<string>();
        if (facet is PurchaseOrderRefreshFacet.Pn or PurchaseOrderRefreshFacet.Brand)
            await CollectIdentityCompletedAsync(lineIds, labels, cancellationToken);
        else if (facet == PurchaseOrderRefreshFacet.Qty)
            await CollectQtyCompletedAsync(lineIds, labels, cancellationToken);
        else if (facet == PurchaseOrderRefreshFacet.Price)
            await CollectPriceCompletedAsync(lineIds, labels, cancellationToken);

        result.CompletedDocuments = DistinctTake(labels);

        if (result.HasCompleted && !result.AllowCompletedParam)
        {
            result.CanProceed = false;
            result.BlockReason =
                "采购参数不允许本分面刷新已完结节点：" + string.Join("；", result.CompletedDocuments)
                + "。请在「采购参数 → 分面刷新」中允许后，再确认覆盖。";
            return result;
        }

        result.CanProceed = true;
        return result;
    }

    /// <inheritdoc />
    public async Task EnsureAllowedAsync(
        string purchaseOrderId,
        PurchaseOrderRefreshFacet facet,
        bool confirmCompleted,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAsync(purchaseOrderId, facet, cancellationToken);
        if (!preview.CanProceed)
            throw new InvalidOperationException(preview.BlockReason ?? "当前不可刷新已完结下游");
        if (preview.HasCompleted && !confirmCompleted)
            throw new InvalidOperationException(
                "存在已完结下游，须确认后再刷新：" + string.Join("；", preview.CompletedDocuments));
    }

    private static List<string> DistinctTake(List<string> labels)
    {
        var distinct = labels.Distinct(StringComparer.Ordinal).ToList();
        if (distinct.Count <= MaxLabels)
            return distinct;
        var taken = distinct.Take(MaxLabels).ToList();
        taken.Add($"另有 {distinct.Count - MaxLabels} 条已完结单据");
        return taken;
    }

    private async Task CollectIdentityCompletedAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        await CollectQtyCompletedAsync(lineIds, labels, cancellationToken);
        await CollectPostedStockInsAsync(lineIds, labels, cancellationToken);

        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var stockItems = (await _stockItemRepo.FindAsync(s =>
                    s.PurchaseOrderItemId != null && chunkList.Contains(s.PurchaseOrderItemId)))
                .ToList();
            if (stockItems.Count > 0)
                labels.Add($"库存明细 {stockItems.Count} 条已入账");

            var stockItemIds = stockItems.Select(s => s.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            if (stockItemIds.Count == 0)
                continue;

            var packingItems = (await _packingItemRepo.FindAsync(p =>
                    p.StockItemId != null && stockItemIds.Contains(p.StockItemId)))
                .ToList();
            var packingIds = packingItems.Select(p => p.PackingId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (packingIds.Count > 0)
            {
                var packings = (await _packingRepo.FindAsync(p => packingIds.Contains(p.Id))).ToList();
                foreach (var p in packings.Where(x => x.Status >= PackingStatusCode.Ready))
                    labels.Add($"装箱单 {p.Code} 已备货/已出库");
            }

            var customsItems = (await _customsItemRepo.FindAsync(c =>
                    c.SourceStockItemId != null && stockItemIds.Contains(c.SourceStockItemId)))
                .ToList();
            if (customsItems.Count == 0)
                continue;
            var declarationIds = customsItems.Select(c => c.DeclarationId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            var declarations = declarationIds.Count == 0
                ? new List<CustomsDeclaration>()
                : (await _customsRepo.FindAsync(d => declarationIds.Contains(d.Id))).ToList();
            foreach (var d in declarations)
                labels.Add($"报关单 {d.DeclarationCode} 已有明细");
        }
    }

    private async Task CollectQtyCompletedAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var notices = (await _notifyRepo.FindAsync(n => chunkList.Contains(n.PurchaseOrderItemId))).ToList();
            foreach (var n in notices.Where(x => x.Status >= ArrivalStockedIn))
                labels.Add($"到货通知 {n.NoticeCode} 已入库");
        }
    }

    private async Task CollectPriceCompletedAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        await CollectQtyCompletedAsync(lineIds, labels, cancellationToken);
        await CollectPostedStockInsAsync(lineIds, labels, cancellationToken);

        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var stockItems = (await _stockItemRepo.FindAsync(s =>
                    s.PurchaseOrderItemId != null && chunkList.Contains(s.PurchaseOrderItemId)))
                .ToList();
            if (stockItems.Count > 0)
                labels.Add($"库存明细 {stockItems.Count} 条已入账");

            var extends = (await _stockInItemExtendRepo.FindAsync(e =>
                    e.PurchaseOrderItemId != null && chunkList.Contains(e.PurchaseOrderItemId)))
                .ToList();
            if (extends.Any(x => x.InvoiceMatchDone > 0))
                labels.Add("入库明细已进项匹配");

            var outs = (await _stockOutExtendRepo.FindAsync(x =>
                    x.PurchaseOrderItemId != null && chunkList.Contains(x.PurchaseOrderItemId)))
                .ToList();
            if (outs.Count > 0)
                labels.Add($"出库明细 {outs.Count} 条已有采购价快照");
        }
    }

    private async Task CollectPostedStockInsAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var extends = (await _stockInItemExtendRepo.FindAsync(e =>
                    e.PurchaseOrderItemId != null && chunkList.Contains(e.PurchaseOrderItemId)))
                .ToList();
            var stockInIds = extends.Select(e => e.StockInId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (stockInIds.Count == 0)
                continue;
            var headers = (await _stockInRepo.FindAsync(s => stockInIds.Contains(s.Id))).ToList();
            foreach (var h in headers.Where(x => x.Status == StockInHeaderStatusCode.Posted))
                labels.Add($"入库单 {h.StockInCode} 已过账");
        }
    }
}
