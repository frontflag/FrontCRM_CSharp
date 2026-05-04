using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public partial class InventoryCenterService
{
    public async Task<PagedResult<InventoryMaterialOverviewDto>> GetMaterialOverviewPagedAsync(
        string? warehouseId,
        string? materialModel,
        string? stockCode,
        short? stockType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (items, total) = await _inventoryMaterialOverviewStockPageQuery.GetStockPageAsync(
                warehouseId,
                materialModel,
                stockCode,
                stockType,
                page,
                pageSize,
                cancellationToken);
            var p = page < 1 ? 1 : page;
            var ps = pageSize < 1 ? 20 : Math.Min(pageSize, IInventoryMaterialOverviewStockPageQuery.MaxPageSize);
            if (items.Count == 0)
            {
                return new PagedResult<InventoryMaterialOverviewDto>
                {
                    Items = Array.Empty<InventoryMaterialOverviewDto>(),
                    TotalCount = total,
                    PageIndex = p,
                    PageSize = ps
                };
            }

            var ledgers = (await _ledgerRepository.GetAllAsync()).ToList();
            var list = await BuildMaterialOverviewDtosAsync(
                items.ToList(),
                ledgers,
                materialModel,
                stockCode,
                applyMaterialStockCodePostFilter: false);
            return new PagedResult<InventoryMaterialOverviewDto>
            {
                Items = list,
                TotalCount = total,
                PageIndex = p,
                PageSize = ps
            };
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
            var p = page < 1 ? 1 : page;
            var ps = pageSize < 1 ? 20 : Math.Min(pageSize, IInventoryMaterialOverviewStockPageQuery.MaxPageSize);
            return new PagedResult<InventoryMaterialOverviewDto>
            {
                Items = Array.Empty<InventoryMaterialOverviewDto>(),
                TotalCount = 0,
                PageIndex = p,
                PageSize = ps
            };
        }
    }

    private async Task<List<InventoryMaterialOverviewDto>> BuildMaterialOverviewDtosAsync(
        List<StockInfo> stocks,
        List<InventoryLedger> ledgers,
        string? materialModel,
        string? stockCode,
        bool applyMaterialStockCodePostFilter)
    {
        if (stocks.Count == 0)
            return new List<InventoryMaterialOverviewDto>();

        Dictionary<string, MaterialInfo> materialById = new();
        try
        {
            materialById = (await _materialRepository.GetAllAsync()).ToDictionary(m => m.Id, m => m);
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
        }

        Dictionary<string, (string? Pn, string? Brand)> productLineByProductId = new(StringComparer.Ordinal);
        var poItemsList = new List<PurchaseOrderItem>();
        try
        {
            foreach (var line in await _sellOrderItemRepository.GetAllAsync())
                MergeProductDisplay(productLineByProductId, line.ProductId, line.PN, line.Brand);

            poItemsList = (await _purchaseOrderItemRepository.GetAllAsync()).ToList();
            foreach (var line in poItemsList)
                MergeProductDisplay(productLineByProductId, line.ProductId, line.PN, line.Brand);
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
        }

        Dictionary<string, (string? Pn, string? Brand)> displayByStockMaterialId = new(StringComparer.Ordinal);
        try
        {
            var inLines = (await _stockInItemRepository.GetAllAsync()).ToList();
            var stockInById = (await _stockInRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
            var siIdsForExt = inLines.Select(x => x.StockInId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var inExtends = siIdsForExt.Count == 0
                ? new List<StockInItemExtend>()
                : (await _stockInItemExtendRepository.FindAsync(e => siIdsForExt.Contains(e.StockInId))).ToList();
            var extByLineId = inExtends.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
            var primaryExtBySi = StockInItemExtendPrimaryPicker.PrimaryByStockInId(inExtends);
            foreach (var sil in inLines)
            {
                var mid = sil.MaterialId?.Trim();
                if (string.IsNullOrEmpty(mid) || displayByStockMaterialId.ContainsKey(mid))
                    continue;
                if (!stockInById.TryGetValue(sil.StockInId, out _))
                    continue;
                string? poLineId = null;
                if (extByLineId.TryGetValue(sil.Id, out var le) && !string.IsNullOrWhiteSpace(le.PurchaseOrderItemId))
                    poLineId = le.PurchaseOrderItemId.Trim();
                else if (primaryExtBySi.TryGetValue(sil.StockInId, out var prim) &&
                         !string.IsNullOrWhiteSpace(prim?.PurchaseOrderItemId))
                    poLineId = prim!.PurchaseOrderItemId.Trim();
                if (string.IsNullOrEmpty(poLineId))
                    continue;
                var hit = poItemsList.FirstOrDefault(p =>
                    string.Equals(p.Id?.Trim(), poLineId, StringComparison.OrdinalIgnoreCase));
                if (hit == null)
                    continue;
                if (!string.Equals(mid, hit.Id, StringComparison.OrdinalIgnoreCase) &&
                    !(string.Equals(mid, hit.ProductId?.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                    !(string.Equals(mid, hit.PN?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    continue;
                displayByStockMaterialId[mid] = (hit.PN, hit.Brand);
            }
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
        }

        Dictionary<string, string> warehouseCodeById = new(StringComparer.Ordinal);
        try
        {
            foreach (var w in await _warehouseRepository.GetAllAsync())
            {
                var id = w.Id?.Trim();
                if (string.IsNullOrEmpty(id)) continue;
                var code = string.IsNullOrWhiteSpace(w.WarehouseCode) ? null : w.WarehouseCode.Trim();
                warehouseCodeById[id] = code ?? id;
            }
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
        }

        Dictionary<string, DateTime?> detailLastMoveByStockAggregateId = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, (DateTime? CreateTime, string? StockInId)> firstDetailByStockAggregateId =
            new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var detailRows = (await _stockItemRepository.GetAllAsync()).ToList();
            detailLastMoveByStockAggregateId = detailRows
                .Where(x => !string.IsNullOrWhiteSpace(x.StockAggregateId))
                .GroupBy(x => x.StockAggregateId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var ts = g
                            .Select(x => x.ModifyTime > x.CreateTime ? x.ModifyTime : x.CreateTime)
                            .OrderByDescending(t => t)
                            .FirstOrDefault();
                        return ts == default ? (DateTime?)null : ts;
                    },
                    StringComparer.OrdinalIgnoreCase);
            firstDetailByStockAggregateId = detailRows
                .Where(x => !string.IsNullOrWhiteSpace(x.StockAggregateId))
                .GroupBy(x => x.StockAggregateId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var first = g
                        .OrderBy(x => x.CreateTime)
                        .ThenBy(x => x.Id, StringComparer.Ordinal)
                        .FirstOrDefault();
                    var value = first == null
                        ? ((DateTime?)null, (string?)null)
                        : (first.CreateTime == default ? (DateTime?)null : first.CreateTime, first.StockInId);
                    return new KeyValuePair<string, (DateTime? CreateTime, string? StockInId)>(g.Key, value);
                })
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
        }

        Dictionary<string, StockIn> overviewStockInById = new(StringComparer.Ordinal);
        Dictionary<string, string?> overviewPrimaryPoLineByStockIn = new(StringComparer.Ordinal);
        Dictionary<string, PurchaseOrder> overviewPoById = new(StringComparer.Ordinal);
        Dictionary<string, string> overviewPoIdByPoLineId = new(StringComparer.Ordinal);
        Dictionary<string, PurchaseOrderItem> overviewPoItemByLineId = new(StringComparer.Ordinal);
        try
        {
            foreach (var sin in await _stockInRepository.GetAllAsync())
            {
                var sid = sin.Id?.Trim();
                if (!string.IsNullOrEmpty(sid))
                    overviewStockInById[sid] = sin;
            }

            var overviewExtends = (await _stockInItemExtendRepository.GetAllAsync()).ToList();
            foreach (var kv in StockInItemExtendPrimaryPicker.PrimaryByStockInId(overviewExtends))
                overviewPrimaryPoLineByStockIn[kv.Key] = kv.Value?.PurchaseOrderItemId?.Trim();

            foreach (var po in await _purchaseOrderRepository.GetAllAsync())
            {
                var pid = po.Id?.Trim();
                if (!string.IsNullOrEmpty(pid))
                    overviewPoById[pid] = po;
            }

            foreach (var pil in await _purchaseOrderItemRepository.GetAllAsync())
            {
                var lid = pil.Id?.Trim();
                var poid = pil.PurchaseOrderId?.Trim();
                if (!string.IsNullOrEmpty(lid) && !string.IsNullOrEmpty(poid))
                    overviewPoIdByPoLineId[lid] = poid;
                if (!string.IsNullOrEmpty(lid))
                    overviewPoItemByLineId[lid] = pil;
            }
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
        }

        Dictionary<string, string> userDisplayById = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var u in await _userRepository.GetAllAsync())
            {
                var uid = u.Id?.Trim();
                if (string.IsNullOrWhiteSpace(uid))
                    continue;
                var login = EntityLookupService.FormatUserLoginName(u) ?? uid;
                userDisplayById[uid] = login;
            }
        }
        catch (Exception ex) when (IsTableMissingException(ex))
        {
        }

        var overviewRows = stocks
            .Select(s =>
            {
                var material = s.MaterialId ?? string.Empty;
                var warehouse = s.WarehouseId ?? string.Empty;
                var stockTypeV = s.StockType;
                var model = string.IsNullOrWhiteSpace(s.PurchasePn) ? null : s.PurchasePn.Trim();
                var name = string.IsNullOrWhiteSpace(s.PurchaseBrand) ? null : s.PurchaseBrand.Trim();
                materialById.TryGetValue(material, out var mat);
                if (string.IsNullOrWhiteSpace(model))
                    model = string.IsNullOrWhiteSpace(mat?.MaterialModel) ? null : mat!.MaterialModel!.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = string.IsNullOrWhiteSpace(mat?.MaterialName) ? null : mat!.MaterialName!.Trim();
                if ((model == null || name == null) && productLineByProductId.TryGetValue(material, out var pl))
                {
                    if (model == null && !string.IsNullOrWhiteSpace(pl.Pn))
                        model = pl.Pn.Trim();
                    if (name == null && !string.IsNullOrWhiteSpace(pl.Brand))
                        name = pl.Brand.Trim();
                }

                if ((model == null || name == null) && displayByStockMaterialId.TryGetValue(material, out var sl))
                {
                    if (model == null && !string.IsNullOrWhiteSpace(sl.Pn))
                        model = sl.Pn.Trim();
                    if (name == null && !string.IsNullOrWhiteSpace(sl.Brand))
                        name = sl.Brand.Trim();
                }

                var onHand = s.QtyRepertory;
                var available = s.QtyRepertoryAvailable;
                var locked = s.QtyOccupy + s.QtySales;
                var latestIn = ledgers
                    .Where(x => x.MaterialId == material && x.WarehouseId == warehouse && x.BizType == "STOCK_IN" && x.QtyIn > 0)
                    .OrderByDescending(x => x.CreateTime)
                    .FirstOrDefault();
                var avgCost = latestIn?.UnitCost ?? 0m;
                short amountCurrency = 1;
                if (latestIn != null &&
                    !string.IsNullOrWhiteSpace(latestIn.BizId) &&
                    overviewStockInById.TryGetValue(latestIn.BizId.Trim(), out _) &&
                    overviewPrimaryPoLineByStockIn.TryGetValue(latestIn.BizId.Trim(), out var poLineForCur) &&
                    !string.IsNullOrWhiteSpace(poLineForCur))
                {
                    var lineId = poLineForCur.Trim();
                    if (overviewPoItemByLineId.TryGetValue(lineId, out var poiForCur) && poiForCur.Currency > 0)
                        amountCurrency = poiForCur.Currency;
                    else if (overviewPoIdByPoLineId.TryGetValue(lineId, out var poIdForCur) &&
                             overviewPoById.TryGetValue(poIdForCur.Trim(), out var poForCur) &&
                             poForCur.Currency > 0)
                        amountCurrency = poForCur.Currency;
                }

                var stockAggregateId = (s.Id ?? string.Empty).Trim();
                DateTime? firstDetailCreateTime = null;
                string? firstDetailCreateUserName = null;
                if (!string.IsNullOrWhiteSpace(stockAggregateId)
                    && firstDetailByStockAggregateId.TryGetValue(stockAggregateId, out var firstDetail))
                {
                    firstDetailCreateTime = firstDetail.CreateTime;
                    var firstDetailStockInId = firstDetail.StockInId?.Trim();
                    if (!string.IsNullOrWhiteSpace(firstDetailStockInId)
                        && overviewStockInById.TryGetValue(firstDetailStockInId, out var firstStockIn))
                    {
                        var uid = !string.IsNullOrWhiteSpace(firstStockIn.CreateByUserId)
                            ? firstStockIn.CreateByUserId!.Trim()
                            : (firstStockIn.CreatedBy?.Trim() ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(uid))
                        {
                            firstDetailCreateUserName = userDisplayById.TryGetValue(uid, out var display)
                                ? display
                                : uid;
                        }
                    }
                }

                var lastMove = !string.IsNullOrWhiteSpace(stockAggregateId)
                    && detailLastMoveByStockAggregateId.TryGetValue(stockAggregateId, out var detailLastMove)
                        ? detailLastMove
                        : ledgers
                            .Where(x => x.MaterialId == material && x.WarehouseId == warehouse)
                            .OrderByDescending(x => x.CreateTime)
                            .FirstOrDefault()?.CreateTime;

                string? whCode = null;
                var whKey = warehouse.Trim();
                if (!string.IsNullOrEmpty(whKey) && warehouseCodeById.TryGetValue(whKey, out var resolved))
                    whCode = resolved;

                var overviewRegionType = RegionTypeCode.Normalize(s.RegionType);

                return new InventoryMaterialOverviewDto
                {
                    StockId = s.Id ?? string.Empty,
                    StockCode = string.IsNullOrWhiteSpace(s.StockCode) ? null : s.StockCode.Trim(),
                    MaterialId = material,
                    MaterialModel = model,
                    MaterialName = name,
                    WarehouseId = warehouse,
                    WarehouseCode = whCode,
                    StockType = stockTypeV,
                    RegionType = overviewRegionType,
                    OnHandQty = onHand,
                    AvailableQty = available,
                    LockedQty = locked,
                    InventoryAmount = onHand * avgCost,
                    Currency = amountCurrency,
                    CreateTime = firstDetailCreateTime,
                    CreateUserName = firstDetailCreateUserName,
                    LastMoveTime = lastMove
                };
            });

        IEnumerable<InventoryMaterialOverviewDto> seq = overviewRows;
        if (applyMaterialStockCodePostFilter)
        {
            seq = seq
                .Where(x => string.IsNullOrWhiteSpace(materialModel)
                            || (!string.IsNullOrWhiteSpace(x.MaterialModel)
                                && x.MaterialModel.Contains(materialModel.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Where(x => string.IsNullOrWhiteSpace(stockCode)
                            || (!string.IsNullOrWhiteSpace(x.StockCode)
                                && x.StockCode.Contains(stockCode.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        return seq
            .OrderByDescending(x => x.LastMoveTime ?? DateTime.MinValue)
            .ThenBy(x => x.WarehouseId, StringComparer.Ordinal)
            .ThenBy(x => x.StockType)
            .ThenBy(x => x.MaterialId, StringComparer.Ordinal)
            .ThenBy(x => x.StockId, StringComparer.Ordinal)
            .ToList();
    }
}
