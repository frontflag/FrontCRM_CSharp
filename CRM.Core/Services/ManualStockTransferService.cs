using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

public sealed class ManualStockTransferService : IManualStockTransferService
{
    private readonly IRepository<StockItem> _stockItemRepository;
    private readonly IRepository<StockInfo> _stockRepository;
    private readonly IRepository<StockIn> _stockInRepository;
    private readonly IRepository<StockInItem> _stockInItemRepository;
    private readonly IRepository<InventoryLedger> _ledgerRepository;
    private readonly IRepository<StockTransferManual> _transferManualRepository;
    private readonly IRepository<StockTransferItemManual> _transferItemManualRepository;
    private readonly IRepository<MaterialInfo> _materialRepository;
    private readonly IRepository<WarehouseInfo> _warehouseRepository;
    private readonly IRepository<PickingTask> _pickingTaskRepository;
    private readonly IRepository<PickingTaskItem> _pickingTaskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISerialNumberService _serialNumberService;
    private readonly IStockExtendLineSeqService _stockExtendLineSeq;
    private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
    private readonly ILogger<ManualStockTransferService> _logger;

    public ManualStockTransferService(
        IRepository<StockItem> stockItemRepository,
        IRepository<StockInfo> stockRepository,
        IRepository<StockIn> stockInRepository,
        IRepository<StockInItem> stockInItemRepository,
        IRepository<InventoryLedger> ledgerRepository,
        IRepository<StockTransferManual> transferManualRepository,
        IRepository<StockTransferItemManual> transferItemManualRepository,
        IRepository<MaterialInfo> materialRepository,
        IRepository<WarehouseInfo> warehouseRepository,
        IRepository<PickingTask> pickingTaskRepository,
        IRepository<PickingTaskItem> pickingTaskItemRepository,
        IUnitOfWork unitOfWork,
        ISerialNumberService serialNumberService,
        IStockExtendLineSeqService stockExtendLineSeq,
        ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
        ILogger<ManualStockTransferService> logger)
    {
        _stockItemRepository = stockItemRepository;
        _stockRepository = stockRepository;
        _stockInRepository = stockInRepository;
        _stockInItemRepository = stockInItemRepository;
        _ledgerRepository = ledgerRepository;
        _transferManualRepository = transferManualRepository;
        _transferItemManualRepository = transferItemManualRepository;
        _materialRepository = materialRepository;
        _warehouseRepository = warehouseRepository;
        _pickingTaskRepository = pickingTaskRepository;
        _pickingTaskItemRepository = pickingTaskItemRepository;
        _unitOfWork = unitOfWork;
        _serialNumberService = serialNumberService;
        _stockExtendLineSeq = stockExtendLineSeq;
        _purchasedStockAvailableSync = purchasedStockAvailableSync;
        _logger = logger;
    }

    public async Task<ManualStockTransferPreviewDto> PreviewAsync(string sourceStockItemId, CancellationToken cancellationToken = default)
    {
        var dto = new ManualStockTransferPreviewDto
        {
            SourceStockItemId = sourceStockItemId?.Trim() ?? string.Empty,
            BlockReasons = Array.Empty<string>()
        };
        if (string.IsNullOrWhiteSpace(sourceStockItemId))
        {
            dto.BlockReasons = new[] { "在库明细 ID 不能为空。" };
            return dto;
        }

        var layer = await _stockItemRepository.GetByIdAsync(sourceStockItemId.Trim());
        if (layer == null)
        {
            dto.BlockReasons = new[] { "在库明细不存在或已删除。" };
            return dto;
        }

        var sourceStock = await _stockRepository.GetByIdAsync(layer.StockAggregateId?.Trim() ?? string.Empty);
        if (sourceStock == null)
        {
            dto.BlockReasons = new[] { "汇总库存分桶不存在。" };
            return dto;
        }

        dto.StockItemCode = layer.StockItemCode;
        dto.MaterialBrand = string.IsNullOrWhiteSpace(layer.PurchaseBrand) ? null : layer.PurchaseBrand.Trim();
        dto.RegionType = RegionTypeCode.Normalize(layer.RegionType);
        var pn = string.IsNullOrWhiteSpace(layer.PurchasePn) ? null : layer.PurchasePn.Trim();
        if (string.IsNullOrEmpty(pn) && !string.IsNullOrWhiteSpace(layer.MaterialId))
        {
            var mat = await _materialRepository.GetByIdAsync(layer.MaterialId.Trim());
            pn = string.IsNullOrWhiteSpace(mat?.MaterialModel) ? null : mat.MaterialModel!.Trim();
        }

        dto.MaterialModel = pn;
        dto.FromWarehouseId = layer.WarehouseId?.Trim() ?? string.Empty;
        dto.SourceLocationId = string.IsNullOrWhiteSpace(layer.LocationId) ? null : layer.LocationId.Trim();
        dto.QtyRepertory = layer.QtyRepertory;
        dto.QtyRepertoryAvailable = layer.QtyRepertoryAvailable;
        dto.PlannedMoveQty = Math.Max(0, layer.QtyRepertoryAvailable);

        var reasons = new List<string>();
        if (dto.PlannedMoveQty <= 0)
            reasons.Add("可迁数量为 0（可用库存不足）。");

        reasons.AddRange(await GetPickingBlockReasonsAsync(layer.Id, cancellationToken));

        var wh = await _warehouseRepository.GetByIdAsync(dto.FromWarehouseId);
        dto.FromWarehouseName = wh?.WarehouseName;

        dto.BlockReasons = reasons;
        dto.CanExecute = dto.PlannedMoveQty > 0 && reasons.Count == 0;
        return dto;
    }

    public async Task<ManualStockTransferExecuteResultDto> ExecuteAsync(
        ManualStockTransferExecuteRequest request,
        string? actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        var sid = request.SourceStockItemId?.Trim() ?? string.Empty;
        var toWh = request.ToWarehouseId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(sid))
            throw new InvalidOperationException("在库明细 ID 不能为空。");
        if (string.IsNullOrEmpty(toWh))
            throw new InvalidOperationException("目标仓库不能为空。");

        var preview = await PreviewAsync(sid, cancellationToken);
        if (!preview.CanExecute || preview.PlannedMoveQty <= 0)
            throw new InvalidOperationException(
                preview.BlockReasons.Count > 0
                    ? string.Join(" ", preview.BlockReasons)
                    : "当前无法在库迁出，请刷新后重试。");

        var moveQty = preview.PlannedMoveQty;

        var layer = await _stockItemRepository.GetByIdAsync(sid)
                    ?? throw new InvalidOperationException("在库明细不存在。");
        var sourceStock = await _stockRepository.GetByIdAsync(layer.StockAggregateId?.Trim() ?? string.Empty)
                          ?? throw new InvalidOperationException("汇总库存分桶不存在。");

        var fromWh = layer.WarehouseId?.Trim() ?? string.Empty;
        if (string.Equals(fromWh, toWh, StringComparison.OrdinalIgnoreCase))
        {
            var targetLoc = string.IsNullOrWhiteSpace(request.ToLocationId)
                ? (layer.LocationId?.Trim() ?? string.Empty)
                : request.ToLocationId.Trim();
            var srcLoc = layer.LocationId?.Trim() ?? string.Empty;
            if (string.Equals(targetLoc, srcLoc, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("源仓与目标仓、库位相同，无需移库。");
        }

        var toWarehouse = await _warehouseRepository.GetByIdAsync(toWh)
                          ?? throw new InvalidOperationException("目标仓库不存在。");

        var targetRegion = RegionTypeCode.Normalize(toWarehouse.RegionType);
        if (toWarehouse.Status != 1)
            throw new InvalidOperationException("目标仓库已停用。");

        // 二次占用校验（与 Preview 间隔内并发）
        var latePick = await GetPickingBlockReasonsAsync(layer.Id, cancellationToken);
        if (latePick.Count > 0)
            throw new InvalidOperationException(string.Join(" ", latePick));

        if (layer.QtyRepertoryAvailable < moveQty)
            throw new InvalidOperationException("可用库存已变化，请关闭后重新打开本页再试。");

        var transferCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockTransferManual);
        var stockInCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockIn);
        var transferId = Guid.NewGuid().ToString();
        var itemManualId = Guid.NewGuid().ToString();
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var targetStockItemId = Guid.NewGuid().ToString();

        var pnKey = NormStockBucketText(sourceStock.PurchasePn);
        var brKey = NormStockBucketText(sourceStock.PurchaseBrand);
        var soKey = NormStockBucketText(sourceStock.SellOrderItemId);

        var allStocks = (await _stockRepository.GetAllAsync()).ToList();
        var targetStock = allStocks.FirstOrDefault(s =>
            StockMatchesInboundBucket(s, pnKey, brKey, toWh, sourceStock.StockType, targetRegion, soKey));

        var isNewTargetStock = targetStock == null;
        if (isNewTargetStock)
        {
            var stockCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Stock);
            targetStock = new StockInfo
            {
                Id = Guid.NewGuid().ToString(),
                StockCode = stockCode,
                MaterialId = layer.MaterialId,
                WarehouseId = toWh,
                LocationId = string.IsNullOrWhiteSpace(request.ToLocationId) ? null : request.ToLocationId.Trim(),
                BatchNo = string.IsNullOrWhiteSpace(layer.BatchNo) ? null : layer.BatchNo.Trim(),
                Unit = string.IsNullOrWhiteSpace(sourceStock.Unit) ? "PCS" : sourceStock.Unit,
                Status = 1,
                StockType = sourceStock.StockType,
                RegionType = targetRegion,
                PurchasePn = sourceStock.PurchasePn,
                PurchaseBrand = sourceStock.PurchaseBrand,
                PurchaseOrderItemId = sourceStock.PurchaseOrderItemId,
                PurchaseOrderItemCode = sourceStock.PurchaseOrderItemCode,
                SellOrderItemId = sourceStock.SellOrderItemId,
                SellOrderItemCode = sourceStock.SellOrderItemCode,
                ProductionDate = layer.ProductionDate,
                ExpiryDate = layer.ExpiryDate,
                CreateTime = DateTime.UtcNow
            };
            await _stockRepository.AddAsync(targetStock);
            await _unitOfWork.SaveChangesAsync();
        }

        var tgt = targetStock!;

        var lineAmount = layer.QtyInbound > 0
            ? Math.Round(layer.PurchaseAmount * (moveQty / (decimal)Math.Max(1, layer.QtyInbound)), 2, MidpointRounding.AwayFromZero)
            : Math.Round(layer.PurchasePrice * moveQty, 2, MidpointRounding.AwayFromZero);

        var virtualStockIn = new StockIn
        {
            Id = stockInId,
            StockInCode = stockInCode,
            StockInType = 3,
            WarehouseId = toWh,
            StockInDate = DateTime.UtcNow,
            TotalQuantity = moveQty,
            TotalAmount = lineAmount,
            Status = 2,
            InspectStatus = 1,
            RegionType = targetRegion,
            Remark = $"手工移库虚拟入库（手工单 {transferCode}）",
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId),
            CreateTime = DateTime.UtcNow
        };
        await _stockInRepository.AddAsync(virtualStockIn);

        var targetLocationId = string.IsNullOrWhiteSpace(request.ToLocationId) ? layer.LocationId : request.ToLocationId.Trim();
        var virtualLine = new StockInItem
        {
            Id = stockInItemId,
            StockInId = stockInId,
            MaterialId = layer.MaterialId,
            PurchasePn = layer.PurchasePn,
            PurchaseBrand = layer.PurchaseBrand,
            StockInItemCode = OrderLineItemCodes.StockIn(stockInCode, 1),
            Quantity = moveQty,
            OrderQty = moveQty,
            QtyReceived = moveQty,
            Price = layer.PurchasePrice,
            Amount = lineAmount,
            LocationId = targetLocationId,
            BatchNo = layer.BatchNo,
            ProductionDate = layer.ProductionDate,
            ExpiryDate = layer.ExpiryDate,
            CreateTime = DateTime.UtcNow
        };
        await _stockInItemRepository.AddAsync(virtualLine);

        tgt.Qty += moveQty;
        tgt.QtyRepertory = tgt.Qty - tgt.QtyStockOut;
        tgt.QtyRepertoryAvailable = tgt.QtyRepertory - tgt.QtyOccupy - tgt.QtySales;
        tgt.ModifyTime = DateTime.UtcNow;
        await _stockRepository.UpdateAsync(tgt);

        var lineSeq = await _stockExtendLineSeq.ReserveNextSequenceBlockAsync(tgt.Id, 1, cancellationToken);
        var stockItemBizCode = OrderLineItemCodes.StockItemLine(tgt.StockCode?.Trim(), lineSeq);
        if (string.IsNullOrEmpty(stockItemBizCode))
        {
            var compact = targetStockItemId.Replace("-", "", StringComparison.Ordinal);
            var tail = compact.Length >= 12 ? compact[..12] : compact;
            stockItemBizCode = $"SKI-MT-{tail}";
        }

        var targetLayer = new StockItem
        {
            Id = targetStockItemId,
            StockItemCode = stockItemBizCode,
            StockInItemCode = virtualLine.StockInItemCode,
            StockInItemId = virtualLine.Id,
            StockInId = stockInId,
            StockAggregateId = tgt.Id,
            MaterialId = layer.MaterialId,
            WarehouseId = toWh,
            LocationId = targetLocationId,
            BatchNo = layer.BatchNo,
            ProductionDate = layer.ProductionDate,
            ExpiryDate = layer.ExpiryDate,
            StockType = layer.StockType,
            RegionType = targetRegion,
            PurchasePn = layer.PurchasePn,
            PurchaseBrand = layer.PurchaseBrand,
            SellOrderItemId = layer.SellOrderItemId,
            SellOrderItemCode = layer.SellOrderItemCode,
            PurchaseOrderItemId = layer.PurchaseOrderItemId,
            PurchaseOrderItemCode = layer.PurchaseOrderItemCode,
            PurchasePrice = layer.PurchasePrice,
            PurchaseCurrency = layer.PurchaseCurrency,
            PurchasePriceUsd = layer.PurchasePriceUsd,
            PurchaseAmount = lineAmount,
            VendorId = layer.VendorId,
            VendorName = layer.VendorName,
            PurchaserId = layer.PurchaserId,
            PurchaserName = layer.PurchaserName,
            CustomerId = layer.CustomerId,
            CustomerName = layer.CustomerName,
            SalespersonId = layer.SalespersonId,
            SalespersonName = layer.SalespersonName,
            SalesPrice = layer.SalesPrice,
            SalesCurrency = layer.SalesCurrency,
            SalesPriceUsd = layer.SalesPriceUsd,
            QtyInbound = moveQty,
            QtyStockOut = 0,
            QtyOccupy = 0,
            QtySales = 0,
            QtyRepertory = moveQty,
            QtyRepertoryAvailable = moveQty,
            CreateTime = DateTime.UtcNow
        };
        targetLayer.SyncDenormalizedComputedFields();
        await _stockItemRepository.AddAsync(targetLayer);

        ApplyManualTransferOut(sourceStock, layer, moveQty);
        await _stockRepository.UpdateAsync(sourceStock);
        await _stockItemRepository.UpdateAsync(layer);

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        var header = new StockTransferManual
        {
            Id = transferId,
            TransferCode = transferCode,
            FromWarehouseId = fromWh,
            ToWarehouseId = toWh,
            Status = StockTransferStatus.Confirmed,
            Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
            ConfirmedTime = now,
            ConfirmedByUserId = actor,
            CreateByUserId = actor,
            CreateTime = now
        };
        await _transferManualRepository.AddAsync(header);

        var itemRow = new StockTransferItemManual
        {
            Id = itemManualId,
            StockTransferManualId = transferId,
            SourceStockItemId = sid,
            TargetStockItemId = targetStockItemId,
            Qty = moveQty,
            CreateTime = now
        };
        await _transferItemManualRepository.AddAsync(itemRow);

        var ledger = new InventoryLedger
        {
            Id = Guid.NewGuid().ToString(),
            BizType = StockLedgerBizType.ManualTransfer,
            BizId = transferId,
            BizLineId = itemManualId,
            MaterialId = layer.MaterialId,
            WarehouseId = fromWh,
            LocationId = layer.LocationId,
            BatchNo = layer.BatchNo,
            QtyIn = 0,
            QtyOut = moveQty,
            UnitCost = layer.PurchasePrice,
            Amount = -lineAmount,
            Remark = $"手工移库 {transferCode}",
            CreateTime = now,
            FromWarehouseId = fromWh,
            ToWarehouseId = toWh,
            StockTransferId = transferId,
            SourceStockItemId = sid,
            TargetStockItemId = targetStockItemId,
            CreateByUserId = actor,
            PurchaseOrderItemId = sourceStock.PurchaseOrderItemId,
            PurchaseOrderItemCode = sourceStock.PurchaseOrderItemCode,
            SellOrderItemId = sourceStock.SellOrderItemId,
            SellOrderItemCode = sourceStock.SellOrderItemCode
        };
        await _ledgerRepository.AddAsync(ledger);

        await _unitOfWork.SaveChangesAsync();

        var changed = new List<StockInfo> { sourceStock, tgt };
        await _purchasedStockAvailableSync.TryRecalculateFromChangedStockInfosAsync(changed, cancellationToken);

        _logger.LogInformation(
            "ManualStockTransfer done TransferCode={Code} MoveQty={Qty} SourceItem={S} TargetItem={T}",
            transferCode, moveQty, sid, targetStockItemId);

        return new ManualStockTransferExecuteResultDto
        {
            StockTransferManualId = transferId,
            TransferCode = transferCode,
            MoveQty = moveQty,
            TargetStockItemId = targetStockItemId,
            TargetStockAggregateId = tgt.Id
        };
    }

    private static void ApplyManualTransferOut(StockInfo stock, StockItem layer, int moveQty)
    {
        stock.QtyStockOut += moveQty;
        stock.QtyRepertory = stock.Qty - stock.QtyStockOut;
        stock.QtyRepertoryAvailable = stock.QtyRepertory - stock.QtyOccupy - stock.QtySales;
        stock.ModifyTime = DateTime.UtcNow;

        layer.QtyStockOut += moveQty;
        layer.QtyRepertory = layer.QtyInbound - layer.QtyStockOut;
        layer.QtyRepertoryAvailable = layer.QtyRepertory - layer.QtyOccupy - layer.QtySales;
        layer.ModifyTime = DateTime.UtcNow;
        layer.SyncDenormalizedComputedFields();
    }

    private async Task<IReadOnlyList<string>> GetPickingBlockReasonsAsync(string stockItemId, CancellationToken cancellationToken)
    {
        var sid = stockItemId.Trim();
        var items = (await _pickingTaskItemRepository.GetAllAsync())
            .Where(x => string.Equals(x.StockItemId?.Trim(), sid, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (items.Count == 0)
            return Array.Empty<string>();

        var taskById = (await _pickingTaskRepository.GetAllAsync())
            .ToDictionary(t => t.Id?.Trim() ?? "", t => t, StringComparer.OrdinalIgnoreCase);

        var reasons = new List<string>();
        foreach (var g in items.GroupBy(x => x.PickingTaskId?.Trim() ?? "", StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(g.Key) || !taskById.TryGetValue(g.Key, out var task))
                continue;
            if (task.Status is 100 or -1)
                continue;
            reasons.Add($"存在未完成拣货任务（{task.TaskCode}，状态 {task.Status}），无法移库。");
        }

        return reasons;
    }

    private static string NormStockBucketText(string? v) =>
        string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();

    private static bool StockMatchesInboundBucket(
        StockInfo s,
        string purchasePnKey,
        string purchaseBrandKey,
        string warehouseId,
        short stockType,
        short regionType,
        string sellOrderItemKey)
    {
        var wh = warehouseId.Trim();
        return s.StockType == stockType
               && RegionTypeCode.Normalize(s.RegionType) == RegionTypeCode.Normalize(regionType)
               && string.Equals(s.WarehouseId?.Trim() ?? string.Empty, wh, StringComparison.OrdinalIgnoreCase)
               && string.Equals(NormStockBucketText(s.PurchasePn), purchasePnKey, StringComparison.OrdinalIgnoreCase)
               && string.Equals(NormStockBucketText(s.PurchaseBrand), purchaseBrandKey, StringComparison.OrdinalIgnoreCase)
               && string.Equals(NormStockBucketText(s.SellOrderItemId), sellOrderItemKey, StringComparison.OrdinalIgnoreCase);
    }
}
