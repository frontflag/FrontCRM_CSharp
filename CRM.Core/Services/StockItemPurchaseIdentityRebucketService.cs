using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class StockItemPurchaseIdentityRebucketService : IStockItemPurchaseIdentityRebucketService
{
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<StockInfo> _stockRepo;
    private readonly ISerialNumberService? _serialNumbers;
    private readonly ISellOrderItemPurchasedStockAvailableSyncService? _purchasedStockAvailable;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly ILogger<StockItemPurchaseIdentityRebucketService> _logger;

    public StockItemPurchaseIdentityRebucketService(
        IRepository<StockItem> stockItemRepo,
        IRepository<StockInfo> stockRepo,
        ILogger<StockItemPurchaseIdentityRebucketService> logger,
        ISerialNumberService? serialNumbers = null,
        ISellOrderItemPurchasedStockAvailableSyncService? purchasedStockAvailable = null,
        IUnitOfWork? unitOfWork = null)
    {
        _stockItemRepo = stockItemRepo;
        _stockRepo = stockRepo;
        _serialNumbers = serialNumbers;
        _purchasedStockAvailable = purchasedStockAvailable;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StockItemPurchaseIdentityRebucketResult> EnsureAggregatesAsync(
        IReadOnlyList<StockItem> layers,
        CancellationToken cancellationToken = default)
    {
        var result = new StockItemPurchaseIdentityRebucketResult();
        if (layers.Count == 0)
            return result;

        var sessionBuckets = new List<StockInfo>();
        var affectedAggIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pnBrandKeys = new HashSet<(string Pn, string Brand)>();

        foreach (var layer in layers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(layer.Id))
                continue;

            var currentAggId = layer.StockAggregateId?.Trim();
            StockInfo? current = null;
            if (!string.IsNullOrEmpty(currentAggId))
            {
                current = sessionBuckets.FirstOrDefault(s =>
                    string.Equals(s.Id, currentAggId, StringComparison.OrdinalIgnoreCase))
                    ?? await _stockRepo.GetByIdAsync(currentAggId);
                if (current != null
                    && sessionBuckets.All(s => !string.Equals(s.Id, current.Id, StringComparison.OrdinalIgnoreCase)))
                    sessionBuckets.Add(current);
            }

            if (current != null)
                RememberPnBrand(pnBrandKeys, current.PurchasePn, current.PurchaseBrand);
            RememberPnBrand(pnBrandKeys, layer.PurchasePn, layer.PurchaseBrand);

            if (current != null && BucketMatchesLayer(current, layer))
            {
                if (SyncAggregateDisplayFromLayer(current, layer))
                {
                    current.ModifyTime = DateTime.UtcNow;
                    await _stockRepo.UpdateAsync(current);
                }
                continue;
            }

            var target = FindMatchingBucket(sessionBuckets, layer)
                ?? await FindMatchingBucketInStoreAsync(layer);
            var created = false;
            if (target == null)
            {
                target = await CreateBucketAsync(layer, current, cancellationToken);
                created = true;
                result.StockAggregatesCreated++;
            }

            if (sessionBuckets.All(s => !string.Equals(s.Id, target.Id, StringComparison.OrdinalIgnoreCase)))
                sessionBuckets.Add(target);

            if (!string.IsNullOrEmpty(currentAggId))
                affectedAggIds.Add(currentAggId);
            affectedAggIds.Add(target.Id);

            if (!string.Equals(layer.StockAggregateId?.Trim(), target.Id, StringComparison.OrdinalIgnoreCase))
            {
                layer.StockAggregateId = target.Id;
                layer.ModifyTime = DateTime.UtcNow;
                await _stockItemRepo.UpdateAsync(layer);
                result.StockItemsMoved++;
            }

            if (created)
                _logger.LogInformation(
                    "PO身份刷新新建库存分桶: StockId={StockId} Code={Code} Pn={Pn} Brand={Brand} FromLayer={LayerId}",
                    target.Id,
                    target.StockCode,
                    target.PurchasePn,
                    target.PurchaseBrand,
                    layer.Id);
        }

        foreach (var aggId in affectedAggIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RecalculateAggregateAsync(aggId, sessionBuckets);
            result.StockAggregatesRecalculated++;
        }

        foreach (var aggId in affectedAggIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await TryRemoveEmptyAggregateAsync(aggId, sessionBuckets))
                result.StockAggregatesRemoved++;
        }

        if (_purchasedStockAvailable != null)
        {
            foreach (var (pn, brand) in pnBrandKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _purchasedStockAvailable.RecalculateByPurchasePnAndBrandAsync(pn, brand, cancellationToken);
            }
        }

        return result;
    }

    private static void RememberPnBrand(HashSet<(string Pn, string Brand)> keys, string? pn, string? brand)
    {
        var p = Norm(pn);
        var b = Norm(brand);
        if (p.Length == 0 || b.Length == 0)
            return;
        keys.Add((p, b));
    }

    private static bool BucketMatchesLayer(StockInfo stock, StockItem layer)
    {
        return stock.StockType == layer.StockType
               && RegionTypeCode.Normalize(stock.RegionType) == RegionTypeCode.Normalize(layer.RegionType)
               && string.Equals(Norm(stock.WarehouseId), Norm(layer.WarehouseId), StringComparison.OrdinalIgnoreCase)
               && string.Equals(Norm(stock.PurchasePn), Norm(layer.PurchasePn), StringComparison.OrdinalIgnoreCase)
               && string.Equals(Norm(stock.PurchaseBrand), Norm(layer.PurchaseBrand), StringComparison.OrdinalIgnoreCase)
               && string.Equals(Norm(stock.SellOrderItemId), Norm(layer.SellOrderItemId), StringComparison.OrdinalIgnoreCase);
    }

    private static bool SyncAggregateDisplayFromLayer(StockInfo stock, StockItem layer)
    {
        var dirty = false;
        var pn = Clip(layer.PurchasePn);
        var brand = Clip(layer.PurchaseBrand);
        if (!string.Equals(stock.PurchasePn, pn, StringComparison.Ordinal))
        {
            stock.PurchasePn = pn;
            dirty = true;
        }
        if (!string.Equals(stock.PurchaseBrand, brand, StringComparison.Ordinal))
        {
            stock.PurchaseBrand = brand;
            dirty = true;
        }
        return dirty;
    }

    private static StockInfo? FindMatchingBucket(List<StockInfo> session, StockItem layer) =>
        session.FirstOrDefault(s => !s.IsDeleted && BucketMatchesLayer(s, layer));

    private async Task<StockInfo?> FindMatchingBucketInStoreAsync(StockItem layer)
    {
        var type = layer.StockType;
        var candidates = (await _stockRepo.FindAsync(s => s.StockType == type && !s.IsDeleted)).ToList();
        return candidates.FirstOrDefault(s => BucketMatchesLayer(s, layer));
    }

    private async Task<StockInfo> CreateBucketAsync(
        StockItem layer,
        StockInfo? source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = _serialNumbers != null
            ? await _serialNumbers.GenerateNextAsync(ModuleCodes.Stock)
            : "STK-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var stock = new StockInfo
        {
            Id = Guid.NewGuid().ToString(),
            StockCode = code,
            MaterialId = layer.MaterialId,
            WarehouseId = layer.WarehouseId,
            LocationId = layer.LocationId ?? source?.LocationId,
            Unit = string.IsNullOrWhiteSpace(source?.Unit) ? "PCS" : source!.Unit,
            Status = source?.Status ?? 1,
            StockType = layer.StockType,
            RegionType = RegionTypeCode.Normalize(layer.RegionType),
            PurchasePn = Clip(layer.PurchasePn),
            PurchaseBrand = Clip(layer.PurchaseBrand),
            SellOrderItemId = Clip(layer.SellOrderItemId),
            SellOrderItemCode = Clip(layer.SellOrderItemCode),
            PurchaseOrderItemId = Clip(layer.PurchaseOrderItemId),
            PurchaseOrderItemCode = Clip(layer.PurchaseOrderItemCode),
            CreateTime = DateTime.UtcNow
        };
        await _stockRepo.AddAsync(stock);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
        return stock;
    }

    private async Task RecalculateAggregateAsync(string stockAggregateId, List<StockInfo> session)
    {
        var stock = session.FirstOrDefault(s =>
                string.Equals(s.Id, stockAggregateId, StringComparison.OrdinalIgnoreCase))
            ?? await _stockRepo.GetByIdAsync(stockAggregateId);
        if (stock == null)
            return;

        var rows = (await _stockItemRepo.FindAsync(x => x.StockAggregateId == stock.Id)).ToList();
        stock.Qty = rows.Sum(x => x.QtyInbound);
        stock.QtyStockOut = rows.Sum(x => x.QtyStockOut);
        stock.QtyOccupy = rows.Sum(x => x.QtyOccupy);
        stock.QtySales = rows.Sum(x => x.QtySales);
        stock.QtyRepertory = rows.Sum(x => x.QtyRepertory);
        stock.QtyRepertoryAvailable = rows.Sum(x => x.QtyRepertoryAvailable);
        stock.ModifyTime = DateTime.UtcNow;
        await _stockRepo.UpdateAsync(stock);
    }

    /// <summary>
    /// 换堆后旧桶已无在库明细、数量全 0：软删汇总行及 stock_extend（对齐库存中心普通删除空堆）。
    /// 仍挂有明细（即使数量为 0）则保留。
    /// </summary>
    private async Task<bool> TryRemoveEmptyAggregateAsync(string stockAggregateId, List<StockInfo> session)
    {
        var stock = session.FirstOrDefault(s =>
                string.Equals(s.Id, stockAggregateId, StringComparison.OrdinalIgnoreCase))
            ?? await _stockRepo.GetByIdAsync(stockAggregateId);
        if (stock == null || stock.IsDeleted)
            return false;

        var rows = (await _stockItemRepo.FindAsync(x => x.StockAggregateId == stock.Id)).ToList();
        if (rows.Count > 0)
            return false;

        if (stock.Qty != 0
            || stock.QtyStockOut != 0
            || stock.QtyOccupy != 0
            || stock.QtySales != 0
            || stock.QtyRepertory != 0
            || stock.QtyRepertoryAvailable != 0)
            return false;

        await _stockRepo.DeleteAsync(stock.Id);
        stock.IsDeleted = true;
        stock.ModifyTime = DateTime.UtcNow;

        if (_unitOfWork != null)
        {
            var escaped = stock.Id.Replace("'", "''", StringComparison.Ordinal);
            await _unitOfWork.ExecuteNonQueryAsync(
                $@"UPDATE public.stock_extend SET is_deleted = true, ""ModifyTime"" = NOW() WHERE ""StockId"" = '{escaped}' AND is_deleted = false");
        }

        _logger.LogInformation(
            "PO身份刷新删除空库存分桶: StockId={StockId} Code={Code} Pn={Pn} Brand={Brand}",
            stock.Id,
            stock.StockCode,
            stock.PurchasePn,
            stock.PurchaseBrand);
        return true;
    }

    private static string Norm(string? v) =>
        string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();

    private static string? Clip(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
