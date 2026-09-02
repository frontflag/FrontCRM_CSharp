using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class SalesOrderRefreshCompletedGateService : ISalesOrderRefreshCompletedGateService
{
    private const short StockOutCompleted = 2;
    private const short StockOutFinished = 4;
    private const int MaxLabels = 12;

    private readonly ISalesParamsService _salesParams;
    private readonly IRepository<SellOrderItem> _soItemRepo;
    private readonly IRepository<StockOutRequest> _notifyRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<FinanceReceivable> _receivableRepo;

    public SalesOrderRefreshCompletedGateService(
        ISalesParamsService salesParams,
        IRepository<SellOrderItem> soItemRepo,
        IRepository<StockOutRequest> notifyRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<Packing> packingRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<FinanceReceivable> receivableRepo)
    {
        _salesParams = salesParams;
        _soItemRepo = soItemRepo;
        _notifyRepo = notifyRepo;
        _packingItemRepo = packingItemRepo;
        _packingRepo = packingRepo;
        _stockOutRepo = stockOutRepo;
        _stockItemRepo = stockItemRepo;
        _receivableRepo = receivableRepo;
    }

    /// <inheritdoc />
    public async Task<SalesOrderRefreshCompletedPreview> PreviewAsync(
        string salesOrderId,
        SalesOrderRefreshFacet facet,
        CancellationToken cancellationToken = default)
    {
        var result = new SalesOrderRefreshCompletedPreview
        {
            Facet = facet.ToApiValue()
        };
        if (facet is SalesOrderRefreshFacet.Status or SalesOrderRefreshFacet.Customer)
        {
            result.CanProceed = true;
            result.AllowCompletedParam = true;
            return result;
        }

        var facets = await _salesParams.GetRefreshCompletedFacetsAsync(cancellationToken);
        result.AllowCompletedParam = facets.Allows(facet);

        var orderId = salesOrderId.Trim();
        var lineIds = (await _soItemRepo.FindAsync(x => x.SellOrderId == orderId && !x.IsDeleted))
            .Select(x => x.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (lineIds.Count == 0)
        {
            result.CanProceed = true;
            return result;
        }

        var labels = new List<string>();
        if (facet is SalesOrderRefreshFacet.Pn or SalesOrderRefreshFacet.Brand)
            await CollectIdentityCompletedAsync(lineIds, labels, cancellationToken);
        else if (facet == SalesOrderRefreshFacet.Qty)
            await CollectQtyCompletedAsync(lineIds, labels, cancellationToken);
        else if (facet == SalesOrderRefreshFacet.Price)
            await CollectPriceCompletedAsync(lineIds, labels, cancellationToken);

        result.CompletedDocuments = DistinctTake(labels);

        if (result.HasCompleted && !result.AllowCompletedParam)
        {
            result.CanProceed = false;
            result.BlockReason =
                "销售参数不允许本分面刷新已完结节点：" + string.Join("；", result.CompletedDocuments)
                + "。请在「销售参数 → 分面刷新」中允许后，再确认覆盖。";
            return result;
        }

        result.CanProceed = true;
        return result;
    }

    /// <inheritdoc />
    public async Task EnsureAllowedAsync(
        string salesOrderId,
        SalesOrderRefreshFacet facet,
        bool confirmCompleted,
        CancellationToken cancellationToken = default)
    {
        var preview = await PreviewAsync(salesOrderId, facet, cancellationToken);
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
        await CollectFinishedPackingsAsync(lineIds, labels, cancellationToken);
        await CollectCompletedStockOutsAsync(lineIds, labels, cancellationToken);
        await CollectVerifiedOrInvoicedReceivablesAsync(lineIds, labels, cancellationToken);
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
            var notices = (await _notifyRepo.FindAsync(n =>
                    !n.IsDeleted && chunkList.Contains(n.SalesOrderItemId)))
                .ToList();
            foreach (var n in notices.Where(x => x.Status >= StockOutRequestStatusCode.StockedOut))
                labels.Add($"出库通知 {n.RequestCode} 已出库");
        }
    }

    private async Task CollectPriceCompletedAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        await CollectFinishedPackingsAsync(lineIds, labels, cancellationToken);
        await CollectCompletedStockOutsAsync(lineIds, labels, cancellationToken);
        await CollectVerifiedOrInvoicedReceivablesAsync(lineIds, labels, cancellationToken);

        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var stockItems = (await _stockItemRepo.FindAsync(s =>
                    s.SellOrderItemId != null && chunkList.Contains(s.SellOrderItemId)))
                .ToList();
            if (stockItems.Count > 0)
                labels.Add($"库存明细 {stockItems.Count} 条已入账");
        }
    }

    private async Task CollectFinishedPackingsAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var packingItems = (await _packingItemRepo.FindAsync(p =>
                    !p.IsDeleted
                    && p.SellOrderItemId != null
                    && chunkList.Contains(p.SellOrderItemId)))
                .ToList();
            var packingIds = packingItems
                .Select(p => p.PackingId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (packingIds.Count == 0)
                continue;
            var packings = (await _packingRepo.FindAsync(p => !p.IsDeleted && packingIds.Contains(p.Id))).ToList();
            foreach (var p in packings.Where(x => x.Status >= PackingStatusCode.StockOutFinished))
                labels.Add($"装箱单 {p.Code} 已出库完成");
        }
    }

    private async Task CollectCompletedStockOutsAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var stockOuts = (await _stockOutRepo.FindAsync(s =>
                    !s.IsDeleted
                    && s.SellOrderItemId != null
                    && chunkList.Contains(s.SellOrderItemId)))
                .ToList();
            foreach (var s in stockOuts.Where(x => x.Status == StockOutCompleted || x.Status == StockOutFinished))
                labels.Add($"出库单 {s.StockOutCode} 已出库");
        }
    }

    private async Task CollectVerifiedOrInvoicedReceivablesAsync(
        List<string> lineIds,
        List<string> labels,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in lineIds.Chunk(200))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkList = chunk.ToList();
            var receivables = (await _receivableRepo.FindAsync(r =>
                    !r.IsDeleted
                    && r.SellOrderItemId != null
                    && chunkList.Contains(r.SellOrderItemId)))
                .ToList();
            foreach (var r in receivables.Where(x =>
                         x.VerifiedDone > 0m
                         || x.VerificationStatus > FinanceVerificationStatusCode.Pending
                         || x.InvoiceMatchDone > 0m))
            {
                labels.Add($"应收 {r.ReceivableCode ?? r.Id} 已核销或已开票匹配");
            }
        }
    }
}
