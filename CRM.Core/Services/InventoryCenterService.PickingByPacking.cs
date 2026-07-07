using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public partial class InventoryCenterService
{
    public async Task<PickingTask> GeneratePickingTaskByPackingAsync(GeneratePickingTaskByPackingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PackingId))
            throw new ArgumentException("装箱单 ID 不能为空", nameof(request));
        if (string.IsNullOrWhiteSpace(request.WarehouseId))
            throw new ArgumentException("仓库 ID 不能为空", nameof(request));

        var packingId = request.PackingId.Trim();
        var packing = await EnsurePackingConfirmedByIdAsync(packingId);

        var warehouseId = request.WarehouseId.Trim();
        await SyncPackingStorageIdFromWarehouseAsync(packing, warehouseId);

        var existing = await GetActivePickingTasksByPackingIdAsync(packingId);
        if (existing.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            return existing[0];
        }

        var taskCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PickingTask);
        var task = new PickingTask
        {
            Id = Guid.NewGuid().ToString(),
            TaskCode = taskCode,
            PackingId = packing.Id,
            WarehouseId = warehouseId,
            OperatorId = string.IsNullOrWhiteSpace(request.OperatorId) ? "SYSTEM" : request.OperatorId.Trim(),
            Status = 1,
            CreateTime = DateTime.UtcNow
        };
        await _pickingTaskRepository.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();
        return task;
    }

    public async Task<PickPageByPackingDto> GetPickPageByPackingAsync(
        string packingId,
        CancellationToken cancellationToken = default)
    {
        var pid = packingId?.Trim();
        if (string.IsNullOrEmpty(pid))
            throw new ArgumentException("装箱单 ID 不能为空", nameof(packingId));

        var packing = await _packingRepository.GetByIdAsync(pid)
            ?? throw new InvalidOperationException("装箱单不存在");

        var packingItems = (await _packingItemRepository.FindAsync(pi =>
            !pi.IsDeleted && pi.PackingId == pid))
            .OrderBy(pi => pi.CreateTime)
            .ThenBy(pi => pi.Id)
            .ToList();

        var activeTasks = await GetActivePickingTasksByPackingIdAsync(pid);
        PickingTask? task = activeTasks.Count > 0 ? activeTasks[0] : null;

        List<PickingTaskItem> taskItems = new();
        Dictionary<string, StockInfo> stockById = new(StringComparer.OrdinalIgnoreCase);
        if (task != null)
        {
            taskItems = await GetPickingTaskItemsByTaskIdAsync(task.Id);
            try
            {
                stockById = (await _stockRepository.GetAllAsync())
                    .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Key.Length > 0)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // ignore
            }
        }

        var itemsByPackingItem = taskItems
            .Where(i => !string.IsNullOrWhiteSpace(i.PackingItemId))
            .GroupBy(i => i.PackingItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var allStockItemIds = taskItems
            .Select(i => i.StockItemId?.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var codeLookups = await ResolveStockItemCodeLookupsByStockItemIdsAsync(allStockItemIds);

        PickingTaskSummaryDto? taskDto = null;
        if (task != null)
        {
            var plan = taskItems.Sum(x => x.PlanQty);
            var picked = taskItems.Sum(x => x.PickedQty);
            var lineDtos = MapPickingTaskItemsToLineDtos(
                taskItems.OrderBy(x => x.CreateTime),
                stockById,
                codeLookups.StockInItemCodeByStockItemId,
                codeLookups.StockItemCodeByStockItemId);
            taskDto = new PickingTaskSummaryDto
            {
                Id = task.Id,
                TaskCode = task.TaskCode,
                PackingId = task.PackingId,
                WarehouseId = task.WarehouseId,
                OperatorId = task.OperatorId,
                Status = task.Status,
                Remark = task.Remark,
                CreateTime = task.CreateTime,
                PlanQtyTotal = plan,
                PickedQtyTotal = picked,
                DistinctStockTypes = lineDtos
                    .Where(x => x.StockType.HasValue)
                    .Select(x => x.StockType!.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList(),
                Items = lineDtos
            };
        }

        var soIds = packingItems
            .Select(x => x.SellOrderId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sellOrders = soIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepository.FindAsync(so => soIds.Contains(so.Id)))
                .ToDictionary(so => so.Id.Trim(), so => so, StringComparer.OrdinalIgnoreCase);

        var soItemIds = packingItems
            .Select(x => x.SellOrderItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sellItems = soItemIds.Count == 0
            ? new Dictionary<string, SellOrderItem>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderItemRepository.FindAsync(si => soItemIds.Contains(si.Id)))
                .ToDictionary(si => si.Id.Trim(), si => si, StringComparer.OrdinalIgnoreCase);

        var lines = new List<PickPagePackingLineDto>();
        foreach (var pi in packingItems)
        {
            var piKey = pi.Id.Trim();
            itemsByPackingItem.TryGetValue(piKey, out var pickRows);
            pickRows ??= new List<PickingTaskItem>();

            var pickLineDtos = MapPickingTaskItemsToLineDtos(
                pickRows.OrderBy(x => x.CreateTime),
                stockById,
                codeLookups.StockInItemCodeByStockItemId,
                codeLookups.StockItemCodeByStockItemId);

            var planSum = pickRows.Sum(x => x.PlanQty);
            var pickedSum = pickRows.Sum(x => x.PickedQty);

            SellOrder? so = null;
            if (!string.IsNullOrWhiteSpace(pi.SellOrderId))
                sellOrders.TryGetValue(pi.SellOrderId.Trim(), out so);
            SellOrderItem? soItem = null;
            if (!string.IsNullOrWhiteSpace(pi.SellOrderItemId))
                sellItems.TryGetValue(pi.SellOrderItemId.Trim(), out soItem);

            lines.Add(new PickPagePackingLineDto
            {
                PackingItemId = piKey,
                ItemCode = string.IsNullOrWhiteSpace(pi.ItemCode) ? null : pi.ItemCode.Trim(),
                Pn = pi.Pn,
                Brand = pi.Brand,
                Qty = pi.Qty,
                Unit = pi.Unit,
                StockOutNotifyId = pi.StockOutNotifyId,
                SellOrderItemId = pi.SellOrderItemId,
                SellOrderCode = so?.SellOrderCode,
                SellOrderItemCode = soItem?.SellOrderItemCode,
                Comment = pi.Comment,
                PlanQtyTotal = planSum,
                PickedQtyTotal = pickedSum,
                LineStatus = ResolvePickLineStatus(planSum, pickedSum, pi.Qty),
                PickingItems = pickLineDtos
            });
        }

        string? packingWarehouseId = string.IsNullOrWhiteSpace(packing.StorageId)
            ? null
            : packing.StorageId.Trim();
        string? packingWarehouseDisplay = null;
        if (!string.IsNullOrEmpty(packingWarehouseId))
        {
            var wh = await _warehouseRepository.GetByIdAsync(packingWarehouseId);
            if (wh != null)
            {
                var name = (wh.WarehouseName ?? "").Trim();
                var code = (wh.WarehouseCode ?? "").Trim();
                packingWarehouseDisplay = string.IsNullOrEmpty(code)
                    ? (string.IsNullOrEmpty(name) ? packingWarehouseId : name)
                    : (string.IsNullOrEmpty(name) ? code : $"{name}（{code}）");
            }
        }

        return new PickPageByPackingDto
        {
            PackingId = packing.Id,
            PackingCode = packing.Code,
            PackingStatus = packing.Status,
            WarehouseId = packingWarehouseId,
            WarehouseDisplay = packingWarehouseDisplay,
            PickingTask = taskDto,
            Lines = lines
        };
    }

    public async Task<IReadOnlyList<PickingStockItemCandidateDto>> GetPickingCandidateStockItemsByPackingItemAsync(
        string packingItemId,
        string warehouseId)
    {
        if (string.IsNullOrWhiteSpace(packingItemId))
            throw new ArgumentException("装箱明细 ID 不能为空", nameof(packingItemId));
        if (string.IsNullOrWhiteSpace(warehouseId))
            throw new ArgumentException("仓库ID不能为空", nameof(warehouseId));

        var pi = await _packingItemRepository.GetByIdAsync(packingItemId.Trim())
            ?? throw new InvalidOperationException("装箱明细不存在");
        if (pi.IsDeleted)
            throw new InvalidOperationException("装箱明细已删除");

        var packing = await EnsurePackingConfirmedByIdAsync(pi.PackingId);
        _ = packing;

        var sellLineId = pi.SellOrderItemId?.Trim() ?? "";
        if (sellLineId.Length == 0)
            throw new InvalidOperationException("装箱明细缺少销售订单明细，无法加载拣货候选");

        var sellLine = await _sellOrderItemRepository.GetByIdAsync(sellLineId)
            ?? throw new InvalidOperationException("销售订单明细不存在");

        return await BuildPickingStockItemCandidatesForSellLineAsync(sellLine, warehouseId.Trim());
    }

    internal async Task SavePickingTaskItemsForPackingAsync(PickingTask task, IReadOnlyList<SavePickingTaskItemLineRequest> lines)
    {
        var packingId = task.PackingId?.Trim() ?? "";
        if (packingId.Length == 0)
            throw new InvalidOperationException("拣货任务未关联装箱单");

        var packing = await _packingRepository.GetByIdAsync(packingId)
            ?? throw new InvalidOperationException("装箱单不存在");
        if (packing.Status != PackingStatusCode.Confirmed && packing.Status != PackingStatusCode.Picked)
        {
            throw new InvalidOperationException(
                $"关联装箱单状态不允许保存拣货明细（须为已确认或已拣货，当前为 {DescribePackingStatus(packing.Status)}）");
        }

        var packingItems = (await _packingItemRepository.FindAsync(pi =>
            !pi.IsDeleted && pi.PackingId == packingId)).ToList();
        if (packingItems.Count == 0)
            throw new InvalidOperationException("装箱单无明细行");

        var packingItemById = packingItems
            .GroupBy(pi => pi.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var piId = line.PackingItemId?.Trim() ?? "";
            if (piId.Length == 0)
                throw new InvalidOperationException("拣货行缺少 packingItemId");
            if (!packingItemById.ContainsKey(piId))
                throw new InvalidOperationException($"装箱明细 {piId} 不属于本装箱单");
        }

        var grouped = lines
            .GroupBy(l => l.PackingItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var g in grouped)
        {
            if (!packingItemById.TryGetValue(g.Key, out var pi))
                continue;
            var sum = g.Sum(x => x.Qty);
            if (sum != pi.Qty)
                throw new InvalidOperationException(
                    $"装箱明细 {pi.ItemCode} 的拣货数量之和（{sum}）须等于装箱数量（{pi.Qty}）。");
        }

        var wh = task.WarehouseId.Trim();
        var stocks = (await _stockRepository.GetAllAsync()).ToList();
        var stocksById = stocks
            .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var materials = (await _materialRepository.GetAllAsync()).ToList();
        var materialById = materials
            .GroupBy(m => m.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var materialByAltKey = StockMaterialMatch.BuildMaterialCodeModelIndex(materials);
        var poItems = (await _purchaseOrderItemRepository.GetAllAsync()).ToList();
        var poItemById = poItems
            .GroupBy(p => p.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var packingItemCodeById = packingItems.ToDictionary(
            pi => pi.Id.Trim(),
            pi => pi.ItemCode?.Trim() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var existing = await GetPickingTaskItemsByTaskIdAsync(task.Id);
        var stockItemsById = (await _stockItemRepository.GetAllAsync())
            .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var e in existing)
        {
            await ReleasePickingTaskItemInventoryAsync(e, stocksById, stockItemsById);
            await _pickingTaskItemRepository.DeleteAsync(e.Id);
        }

        var validatedLines = new List<(
            SavePickingTaskItemLineRequest Line,
            string StockItemId,
            string StockId,
            string PackingItemId,
            bool IsStockingSupplement,
            string MaterialId,
            string? BatchNo,
            string? LocationId,
            SellOrderItem SellLine)>();

        foreach (var line in lines)
        {
            var piId = line.PackingItemId!.Trim();
            var pi = packingItemById[piId];
            var sellLineId = pi.SellOrderItemId?.Trim() ?? "";
            if (sellLineId.Length == 0)
                throw new InvalidOperationException($"装箱明细 {pi.ItemCode} 缺少销售订单明细");
            var sellLine = await _sellOrderItemRepository.GetByIdAsync(sellLineId)
                ?? throw new InvalidOperationException("销售订单明细不存在");

            var sid = line.StockItemId?.Trim() ?? "";
            var agg = line.StockId?.Trim() ?? "";
            if (sid.Length == 0 || agg.Length == 0)
                throw new InvalidOperationException("拣货行缺少 stockItemId 或 stockId");
            var si = await _stockItemRepository.GetByIdAsync(sid)
                ?? throw new InvalidOperationException($"在库明细不存在：{sid}");
            if (!string.Equals(si.StockAggregateId?.Trim(), agg, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"stockItemId 与 stockId（汇总桶）不一致：{sid}");
            if (!string.Equals(si.WarehouseId?.Trim(), wh, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("在库明细不属于该拣货任务仓库");
            if (line.Qty <= 0)
                throw new InvalidOperationException("拣货数量须大于 0");
            if (line.Qty > si.QtyRepertoryAvailable)
                throw new InvalidOperationException($"在库明细 {sid} 可用量不足（可用 {si.QtyRepertoryAvailable}，申请 {line.Qty}）");

            if (!stocksById.TryGetValue(agg, out var stock))
                throw new InvalidOperationException($"汇总库存不存在：{agg}");

            var bindSell = string.Equals(si.SellOrderItemId?.Trim(), sellLineId, StringComparison.OrdinalIgnoreCase);
            bool isStockingSupplement;
            if (bindSell)
                isStockingSupplement = false;
            else if (stock.StockType == 2
                     && StockMaterialMatch.StockingSupplementEligible(stock, sellLine, materialById, materialByAltKey, poItemById))
                isStockingSupplement = true;
            else
                throw new InvalidOperationException($"在库明细 {sid} 不在本行可拣范围内（非本销售行绑定且非备货匹配）");

            validatedLines.Add((
                line,
                sid,
                agg,
                piId,
                isStockingSupplement,
                si.MaterialId?.Trim() ?? string.Empty,
                si.BatchNo,
                si.LocationId,
                sellLine));
        }

        var itemCodes = PickingTaskItemCodeAssigner.Assign(
            validatedLines.Select(x => x.PackingItemId).ToList(),
            packingItemCodeById,
            task.TaskCode);

        if (!string.IsNullOrWhiteSpace(task.WarehouseId))
            await UpdatePackingStorageIdAsync(packing, task.WarehouseId);

        var changedStocks = new HashSet<StockInfo>();
        var changedLayers = new HashSet<StockItem>();
        for (var i = 0; i < validatedLines.Count; i++)
        {
            var v = validatedLines[i];
            var takeQty = v.Line.Qty;
            if (!stockItemsById.TryGetValue(v.StockItemId, out var layer))
                layer = await _stockItemRepository.GetByIdAsync(v.StockItemId)
                    ?? throw new InvalidOperationException($"在库明细不存在：{v.StockItemId}");
            if (!stocksById.TryGetValue(v.StockId, out var stock))
                throw new InvalidOperationException($"汇总库存不存在：{v.StockId}");

            InventoryStockOutboundMutation.ApplyTake(stock, layer, takeQty);
            changedStocks.Add(stock);
            changedLayers.Add(layer);
            stockItemsById[v.StockItemId] = layer;

            await _pickingTaskItemRepository.AddAsync(new PickingTaskItem
            {
                Id = Guid.NewGuid().ToString(),
                PickingTaskId = task.Id,
                ItemCode = itemCodes[i],
                PackingItemId = v.PackingItemId,
                MaterialId = v.MaterialId,
                StockId = v.StockId,
                StockItemId = v.StockItemId,
                BatchNo = v.BatchNo,
                LocationId = v.LocationId,
                PlanQty = takeQty,
                PickedQty = takeQty,
                IsStockingSupplement = v.IsStockingSupplement,
                CreateTime = DateTime.UtcNow
            });
        }

        foreach (var stock in changedStocks)
            await _stockRepository.UpdateAsync(stock);
        foreach (var layer in changedLayers)
            await _stockItemRepository.UpdateAsync(layer);

        await MarkPackingPickedAfterPickAsync(packing);
        await _unitOfWork.SaveChangesAsync();

        // 生成拣货单（保存明细）后自动结案拣货任务，与原先「完成拣货」一致
        await CompletePickingTaskForPackingAsync(task);
    }

    internal async Task CompletePickingTaskForPackingAsync(PickingTask task)
    {
        if (task.Status == 100)
            return;

        var packingId = task.PackingId?.Trim() ?? "";
        if (packingId.Length == 0)
            throw new InvalidOperationException("拣货任务未关联装箱单");

        var packing = await _packingRepository.GetByIdAsync(packingId)
            ?? throw new InvalidOperationException("装箱单不存在");

        if (packing.Status == PackingStatusCode.Confirmed)
            await MarkPackingPickedAfterPickAsync(packing);
        else if (packing.Status != PackingStatusCode.Picked)
        {
            throw new InvalidOperationException(
                $"关联装箱单状态不允许完成拣货（须为已确认或已拣货，当前为 {DescribePackingStatus(packing.Status)}）");
        }

        var packingItems = (await _packingItemRepository.FindAsync(pi =>
            !pi.IsDeleted && pi.PackingId == packingId)).ToList();

        var items = await GetPickingTaskItemsByTaskIdAsync(task.Id);
        if (items.Count == 0)
            throw new InvalidOperationException("请先保存拣货明细后再完成拣货");
        if (items.Any(x => string.IsNullOrWhiteSpace(x.StockItemId)))
            throw new InvalidOperationException("拣货明细缺少 stock_item_id，请使用新流程保存拣货后再完成");

        foreach (var pi in packingItems)
        {
            var piKey = pi.Id.Trim();
            var planSum = items
                .Where(x => string.Equals(x.PackingItemId?.Trim(), piKey, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.PlanQty);
            if (planSum != pi.Qty)
            {
                throw new InvalidOperationException(
                    $"装箱明细 {pi.ItemCode} 的计划拣货量（{planSum}）须等于装箱数量（{pi.Qty}）才能完成拣货");
            }
        }

        task.Status = 100;
        task.ModifyTime = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.PickedQty = item.PlanQty;
            item.ModifyTime = DateTime.UtcNow;
            await _pickingTaskItemRepository.UpdateAsync(item);
        }

        await _pickingTaskRepository.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        if (StockOutTypeCode.NormalizeForNotify(packing.StockOutType) == StockOutTypeCode.Customs)
        {
            await _customsV2FlowService.WritebackDeclarationItemsAfterPickingAsync(
                packingId, task.Id, null);
        }
    }

    private async Task<Packing> EnsurePackingConfirmedByIdAsync(string packingId)
    {
        var packing = await _packingRepository.GetByIdAsync(packingId.Trim())
            ?? throw new InvalidOperationException("装箱单不存在");
        if (packing.IsDeleted)
            throw new InvalidOperationException("装箱单已删除");
        if (packing.Status != PackingStatusCode.Confirmed)
        {
            throw new InvalidOperationException(
                $"装箱单状态不允许拣货（须为已确认，当前为 {DescribePackingStatus(packing.Status)}）");
        }

        return packing;
    }

    private static string ResolvePickLineStatus(int planSum, int pickedSum, int targetQty)
    {
        if (planSum <= 0)
            return "pending";
        if (planSum > targetQty)
            return "over";
        if (planSum < targetQty)
            return "partial";
        if (pickedSum >= planSum)
            return "done";
        return "allocated";
    }

    private async Task<IReadOnlyList<PickingStockItemCandidateDto>> BuildPickingStockItemCandidatesForSellLineAsync(
        SellOrderItem sellLine,
        string warehouseId)
    {
        var sellLineId = sellLine.Id.Trim();
        var wh = warehouseId.Trim();

        var materials = (await _materialRepository.GetAllAsync()).ToList();
        var materialById = materials
            .GroupBy(m => m.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var materialByAltKey = StockMaterialMatch.BuildMaterialCodeModelIndex(materials);
        var poItems = (await _purchaseOrderItemRepository.GetAllAsync()).ToList();
        var poItemById = poItems
            .GroupBy(p => p.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var poIds = poItems
            .Select(p => p.PurchaseOrderId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var poById = poIds.Count == 0
            ? new Dictionary<string, PurchaseOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _purchaseOrderRepository.FindAsync(p => poIds.Contains(p.Id)))
                .GroupBy(p => p.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var stocks = (await _stockRepository.GetAllAsync()).ToList();
        var stocksById = stocks
            .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var allLayers = (await _stockItemRepository.GetAllAsync()).ToList();
        var candidates = new List<StockItem>();
        foreach (var si in allLayers)
        {
            if (!string.Equals(si.WarehouseId?.Trim(), wh, StringComparison.OrdinalIgnoreCase))
                continue;
            if (si.QtyRepertoryAvailable <= 0)
                continue;
            var aggId = si.StockAggregateId?.Trim() ?? "";
            if (aggId.Length == 0 || !stocksById.TryGetValue(aggId, out var stock))
                continue;

            var bindSell = string.Equals(si.SellOrderItemId?.Trim(), sellLineId, StringComparison.OrdinalIgnoreCase);
            if (bindSell)
            {
                candidates.Add(si);
                continue;
            }

            if (stock.StockType == 2
                && StockMaterialMatch.StockingSupplementEligible(stock, sellLine, materialById, materialByAltKey, poItemById))
                candidates.Add(si);
        }

        var distinct = candidates
            .GroupBy(x => x.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .Select(g => g.First())
            .OrderBy(si => si.ProductionDate ?? si.CreateTime)
            .ThenBy(si => si.CreateTime)
            .ToList();

        var stockItemKeys = distinct.Select(si => si.Id?.Trim() ?? "").Where(x => x.Length > 0).ToList();
        var codeLookups = await ResolveStockItemCodeLookupsByStockItemIdsAsync(stockItemKeys);

        var stockInIds = distinct
            .Select(x => x.StockInId?.Trim() ?? "")
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var stockInDateById = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
        if (stockInIds.Count > 0)
        {
            try
            {
                var sins = (await _stockInRepository.FindAsync(x => stockInIds.Contains(x.Id))).ToList();
                foreach (var s in sins)
                    stockInDateById[s.Id.Trim()] = s.StockInDate;
            }
            catch
            {
                // ignore
            }
        }

        var list = new List<PickingStockItemCandidateDto>();
        foreach (var si in distinct)
        {
            var aggId = si.StockAggregateId?.Trim() ?? "";
            stocksById.TryGetValue(aggId, out var st);
            var bindSell = string.Equals(si.SellOrderItemId?.Trim(), sellLineId, StringComparison.OrdinalIgnoreCase);
            var stkId = si.Id.Trim();
            codeLookups.StockInItemCodeByStockItemId.TryGetValue(stkId, out var sinCode);
            codeLookups.StockItemCodeByStockItemId.TryGetValue(stkId, out var stockItemBizCode);
            stockInDateById.TryGetValue(si.StockInId?.Trim() ?? "", out var sinDate);
            list.Add(new PickingStockItemCandidateDto
            {
                StockItemId = stkId,
                StockItemCode = stockItemBizCode,
                StockInItemCode = sinCode,
                StockInDate = sinDate,
                StockAggregateId = aggId,
                MaterialId = si.MaterialId?.Trim() ?? string.Empty,
                AvailableQty = si.QtyRepertoryAvailable,
                StockType = st?.StockType ?? 1,
                PurchasePn = si.PurchasePn,
                PurchaseBrand = si.PurchaseBrand,
                FreightForwarderOrderNo = FreightForwarderOrderNoLookup.FromPurchaseOrderItemId(
                    si.PurchaseOrderItemId, poItemById, poById),
                LocationId = si.LocationId,
                BatchNo = si.BatchNo,
                WarehouseId = si.WarehouseId?.Trim() ?? wh,
                ProductionDate = si.ProductionDate,
                CreateTime = si.CreateTime,
                IsStockingCandidate = !bindSell && (st?.StockType == 2)
            });
        }

        return list;
    }

    private async Task<(StockOutRequest? Sor, SellOrder? So, Packing? Packing)> ResolvePickingTaskDisplayContextAsync(
        PickingTask task,
        IReadOnlyDictionary<string, StockOutRequest>? sorById,
        IReadOnlyDictionary<string, SellOrder>? soById)
    {
        var packingId = task.PackingId?.Trim();
        if (string.IsNullOrEmpty(packingId))
            return (null, null, null);

        var packing = await _packingRepository.GetByIdAsync(packingId);
        var packingItems = (await _packingItemRepository.FindAsync(pi =>
                !pi.IsDeleted && pi.PackingId == packingId))
            .OrderBy(pi => pi.CreateTime)
            .ThenBy(pi => pi.Id)
            .ToList();

        var notifyId = packingItems
            .Select(pi => pi.StockOutNotifyId?.Trim())
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));

        StockOutRequest? sor = null;
        if (!string.IsNullOrEmpty(notifyId))
        {
            if (sorById != null && sorById.TryGetValue(notifyId, out var cached))
                sor = cached;
            else
                sor = await _stockOutRequestRepository.GetByIdAsync(notifyId);
        }

        SellOrder? so = null;
        if (sor != null && !string.IsNullOrWhiteSpace(sor.SalesOrderId))
        {
            var soId = sor.SalesOrderId.Trim();
            if (soById != null && soById.TryGetValue(soId, out var soCached))
                so = soCached;
            else
                so = await _sellOrderRepository.GetByIdAsync(soId);
        }

        return (sor, so, packing);
    }

    /// <summary>拣货详情「装箱信息」：从关联装箱单及其出库通知读取物流字段（不在拣货单表冗余）。</summary>
    private async Task<PickingTaskPackingPanelDto?> BuildPickingTaskPackingPanelAsync(
        PickingTask task,
        Packing? packingHint = null,
        CancellationToken cancellationToken = default)
    {
        var packingId = task.PackingId?.Trim();
        if (string.IsNullOrEmpty(packingId))
            return null;

        var packing = packingHint ?? await _packingRepository.GetByIdAsync(packingId);
        if (packing == null)
            return null;

        var packingItems = (await _packingItemRepository.FindAsync(pi =>
                !pi.IsDeleted && pi.PackingId == packingId))
            .ToList();

        var notifyIds = packingItems
            .Select(pi => pi.StockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        StockOutRequest? firstNotify = null;
        if (notifyIds.Count > 0)
        {
            var notifies = (await _stockOutRequestRepository.FindAsync(r =>
                    !r.IsDeleted && notifyIds.Contains(r.Id)))
                .OrderBy(r => r.RequestCode, StringComparer.OrdinalIgnoreCase)
                .ToList();
            firstNotify = notifies.FirstOrDefault();
        }

        string? customsDeclarationId = null;
        string? customsDeclarationCode = null;
        StockOutCustomsSummaryDto? customsSummary = null;
        if (StockOutTypeCode.NormalizeForNotify(packing.StockOutType) == StockOutTypeCode.Customs)
        {
            customsDeclarationId = string.IsNullOrWhiteSpace(packing.CustomsDeclarationId)
                ? null
                : packing.CustomsDeclarationId.Trim();
            if (!string.IsNullOrEmpty(customsDeclarationId))
            {
                customsSummary = await _customsTraceQuery.ResolveCustomsSummaryByDeclarationIdAsync(
                    customsDeclarationId,
                    cancellationToken);
                customsDeclarationCode = customsSummary?.DeclarationCode;
            }
        }

        return new PickingTaskPackingPanelDto
        {
            PackingId = packingId,
            PackingCode = string.IsNullOrWhiteSpace(packing.Code) ? null : packing.Code.Trim(),
            ShipmentMethod = string.IsNullOrWhiteSpace(firstNotify?.ShipmentMethod)
                ? null
                : firstNotify!.ShipmentMethod.Trim(),
            ExpressCompany = string.IsNullOrWhiteSpace(firstNotify?.ExpressCompany)
                ? null
                : firstNotify!.ExpressCompany.Trim(),
            StockOutType = packing.StockOutType,
            CustomsDeclarationId = customsDeclarationId,
            CustomsDeclarationCode = customsDeclarationCode,
            CustomsSummary = customsSummary
        };
    }

    /// <summary>按装箱单查未取消的拣货任务（按创建时间倒序，禁止 GetAllAsync 扫全表）。</summary>
    private async Task<List<PickingTask>> GetActivePickingTasksByPackingIdAsync(string packingId)
    {
        var pid = packingId.Trim();
        // EF 无法翻译 string.Equals(..., StringComparison)；装箱/任务主键为 GUID，用相等比较即可
        var list = (await _pickingTaskRepository.FindAsync(x =>
                x.PackingId == pid && x.Status != -1))
            .ToList();
        return list.OrderByDescending(x => x.CreateTime).ToList();
    }

    /// <summary>按拣货任务 ID 查明细（禁止 GetAllAsync 扫全表，避免其它单 NULL item_code 拖垮本单）。</summary>
    private async Task<List<PickingTaskItem>> GetPickingTaskItemsByTaskIdAsync(string pickingTaskId)
    {
        var tid = pickingTaskId.Trim();
        return (await _pickingTaskItemRepository.FindAsync(x => x.PickingTaskId == tid)).ToList();
    }

    /// <summary>拣货页所选仓库（<c>warehouseinfo.Id</c>）写入 <see cref="Packing.StorageId"/>。</summary>
    private Task SyncPackingStorageIdFromWarehouseAsync(Packing packing, string warehouseId) =>
        UpdatePackingStorageIdAsync(packing, warehouseId);

    private async Task UpdatePackingStorageIdAsync(Packing packing, string? warehouseInfoId)
    {
        var wid = warehouseInfoId?.Trim();
        if (string.IsNullOrEmpty(wid))
            return;
        if (string.Equals(packing.StorageId?.Trim(), wid, StringComparison.OrdinalIgnoreCase))
            return;

        packing.StorageId = wid;
        packing.ModifyTime = DateTime.UtcNow;
        await _packingRepository.UpdateAsync(packing);
    }

    /// <inheritdoc />
    public async Task ReleasePickingTasksByPackingIdAsync(string packingId)
    {
        if (string.IsNullOrWhiteSpace(packingId))
            throw new ArgumentException("装箱单 ID 不能为空", nameof(packingId));

        var tasks = (await GetActivePickingTasksByPackingIdAsync(packingId.Trim()))
            .Where(t => !t.IsDeleted)
            .ToList();
        foreach (var task in tasks)
            await ReleaseAndDeletePickingTaskAsync(task);
    }

    public async Task DeletePickingSlipAsync(string id)
    {
        var task = await _pickingTaskRepository.GetByIdAsync(id.Trim())
            ?? throw new InvalidOperationException("拣货单不存在");

        if (task.Status != 1)
            throw new InvalidOperationException("仅待拣货状态可普通删除");

        if (await HasStockOutLinkedToPickingTaskAsync(task.Id))
            throw new InvalidOperationException("存在下游出库单关联本拣货任务，不能删除");

        await ReleaseAndDeletePickingTaskAsync(task);
    }

    private async Task ReleaseAndDeletePickingTaskAsync(PickingTask task)
    {
        var items = await GetPickingTaskItemsByTaskIdAsync(task.Id);
        var stocks = (await _stockRepository.GetAllAsync()).ToList();
        var stocksById = stocks
            .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var stockItemsById = (await _stockItemRepository.GetAllAsync())
            .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var changedStocks = new HashSet<StockInfo>();
        var changedLayers = new HashSet<StockItem>();
        foreach (var line in items)
        {
            await ReleasePickingTaskItemInventoryAsync(line, stocksById, stockItemsById, changedStocks, changedLayers);
            await _pickingTaskItemRepository.DeleteAsync(line.Id);
        }

        foreach (var stock in changedStocks)
            await _stockRepository.UpdateAsync(stock);
        foreach (var layer in changedLayers)
            await _stockItemRepository.UpdateAsync(layer);

        await _pickingTaskRepository.DeleteAsync(task.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task ReleasePickingTaskItemInventoryAsync(
        PickingTaskItem line,
        Dictionary<string, StockInfo> stocksById,
        Dictionary<string, StockItem> stockItemsById,
        HashSet<StockInfo>? changedStocks = null,
        HashSet<StockItem>? changedLayers = null)
    {
        var takeQty = line.PlanQty;
        if (takeQty <= 0)
            return;
        if (line.PickedQty < takeQty)
            return;

        var layerId = line.StockItemId?.Trim() ?? "";
        var stockId = line.StockId?.Trim() ?? "";
        if (layerId.Length == 0 || stockId.Length == 0)
            return;

        if (!stockItemsById.TryGetValue(layerId, out var layer))
        {
            layer = await _stockItemRepository.GetByIdAsync(layerId);
            if (layer == null)
                return;
            stockItemsById[layerId] = layer;
        }

        if (!stocksById.TryGetValue(stockId, out var stock))
        {
            stock = await _stockRepository.GetByIdAsync(stockId);
            if (stock == null)
                return;
            stocksById[stockId] = stock;
        }

        if (!InventoryStockOutboundMutation.IsInventoryAlreadyAppliedAtPick(line, layer, takeQty))
            return;

        InventoryStockOutboundMutation.ApplyRestore(stock, layer, takeQty);
        changedStocks?.Add(stock);
        changedLayers?.Add(layer);
    }
}
