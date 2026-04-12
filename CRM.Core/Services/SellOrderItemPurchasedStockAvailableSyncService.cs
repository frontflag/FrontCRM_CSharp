using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class SellOrderItemPurchasedStockAvailableSyncService : ISellOrderItemPurchasedStockAvailableSyncService
{
    private const short SellOrderItemCancelled = 1;
    private const short StockOutProgressComplete = 2;

    private readonly IRepository<StockInfo> _stockRepo;
    private readonly IRepository<SellOrderItem> _soItemRepo;
    private readonly IRepository<SellOrderItemExtend> _extendRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly ILogger<SellOrderItemPurchasedStockAvailableSyncService> _logger;

    public SellOrderItemPurchasedStockAvailableSyncService(
        IRepository<StockInfo> stockRepo,
        IRepository<SellOrderItem> soItemRepo,
        IRepository<SellOrderItemExtend> extendRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<PurchaseOrder> poRepo,
        IRepository<StockInItem> stockInItemRepo,
        ILogger<SellOrderItemPurchasedStockAvailableSyncService> logger)
    {
        _stockRepo = stockRepo;
        _soItemRepo = soItemRepo;
        _extendRepo = extendRepo;
        _poItemRepo = poItemRepo;
        _poRepo = poRepo;
        _stockInItemRepo = stockInItemRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecalculateByPurchasePnAndBrandAsync(
        string? purchasePn,
        string? purchaseBrand,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var pnKey = NormKey(purchasePn);
        var brKey = NormKey(purchaseBrand);
        if (string.IsNullOrEmpty(pnKey) || string.IsNullOrEmpty(brKey))
            return;

        var stocks = (await _stockRepo.GetAllAsync()).ToList();
        var sumAvail = stocks
            .Where(s => s.StockType == StockInventoryTypeCodes.Stocking
                        && string.Equals(NormKey(s.PurchasePn), pnKey, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(NormKey(s.PurchaseBrand), brKey, StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.QtyRepertoryAvailable);
        var intVal = ToIntNonNegative(sumAvail);

        var soItems = (await _soItemRepo.GetAllAsync()).ToList();
        var updated = 0;
        foreach (var line in soItems)
        {
            if (line.Status == SellOrderItemCancelled || line.Qty <= 0m)
                continue;
            if (!string.Equals(NormKey(line.PN), pnKey, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(NormKey(line.Brand), brKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var ext = await _extendRepo.GetByIdAsync(line.Id);
            if (ext == null)
                continue;
            if (ext.StockOutProgressStatus == StockOutProgressComplete)
                continue;

            if (ext.PurchasedStock_AvailableQty == intVal)
                continue;

            ext.PurchasedStock_AvailableQty = intVal;
            ext.ModifyTime = DateTime.UtcNow;
            await _extendRepo.UpdateAsync(ext);
            updated++;
        }

        if (updated > 0)
        {
            _logger.LogInformation(
                "[PurchasedStockAvail] Updated {Count} sell lines for PN={Pn} Brand={Br} SumAvail={Sum} IntVal={IntVal}",
                updated, pnKey, brKey, sumAvail, intVal);
        }
    }

    /// <inheritdoc />
    public async Task TryRecalculateFromCompletedStockInAsync(StockIn stockIn, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (stockIn.Status != 2)
            return;

        var keys = new HashSet<(string Pn, string Br)>();
        var lines = (await _stockInItemRepo.GetAllAsync())
            .Where(x => string.Equals(x.StockInId?.Trim(), stockIn.Id?.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var line in lines)
        {
            var poi = await ResolvePoItemForStockInLineAsync(line, stockIn);
            if (poi == null)
                continue;
            var po = await ResolvePoHeaderAsync(poi);
            if (po == null)
                continue;
            var st = StockInventoryTypeCodes.Normalize(po.Type);
            if (st != StockInventoryTypeCodes.Stocking)
                continue;

            var pn = NormKey(poi.PN);
            var br = NormKey(poi.Brand);
            if (string.IsNullOrEmpty(pn) || string.IsNullOrEmpty(br))
                continue;
            keys.Add((pn, br));
        }

        foreach (var (pn, br) in keys)
            await RecalculateByPurchasePnAndBrandAsync(pn, br, cancellationToken);
    }

    /// <inheritdoc />
    public async Task TryRecalculateFromChangedStockInfosAsync(
        IEnumerable<StockInfo> changedStocks,
        CancellationToken cancellationToken = default)
    {
        var keys = new HashSet<(string Pn, string Br)>();
        foreach (var s in changedStocks)
        {
            if (s.StockType != StockInventoryTypeCodes.Stocking)
                continue;
            var pn = NormKey(s.PurchasePn);
            var br = NormKey(s.PurchaseBrand);
            if (string.IsNullOrEmpty(pn) || string.IsNullOrEmpty(br))
                continue;
            keys.Add((pn, br));
        }

        foreach (var (pn, br) in keys)
            await RecalculateByPurchasePnAndBrandAsync(pn, br, cancellationToken);
    }

    private async Task<PurchaseOrderItem?> ResolvePoItemForStockInLineAsync(StockInItem line, StockIn stockIn)
    {
        var mid = line.MaterialId?.Trim();
        if (!string.IsNullOrEmpty(mid))
        {
            var byId = await _poItemRepo.GetByIdAsync(mid);
            if (byId != null)
                return byId;
        }

        var headerPoLine = stockIn.PurchaseOrderItemId?.Trim();
        if (!string.IsNullOrEmpty(headerPoLine))
            return await _poItemRepo.GetByIdAsync(headerPoLine);

        return null;
    }

    private async Task<PurchaseOrder?> ResolvePoHeaderAsync(PurchaseOrderItem poi)
    {
        var poid = poi.PurchaseOrderId?.Trim();
        if (string.IsNullOrEmpty(poid))
            return null;
        return await _poRepo.GetByIdAsync(poid);
    }

    private static string NormKey(string? v) =>
        string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();

    private static int ToIntNonNegative(int sum) => sum < 0 ? 0 : sum;
}
