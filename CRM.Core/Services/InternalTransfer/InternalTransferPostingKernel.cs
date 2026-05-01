using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services.InternalTransfer;

/// <inheritdoc />
public sealed class InternalTransferPostingKernel : IInternalTransferPostingKernel
{
    private const short TransferStockOutType = 3;
    private const short TransferStockInType = 3;
    private const short VirtualDocumentCompletedStatus = 2;
    private const int SourceCodeMaxLen = 32;

    private readonly IRepository<StockItem> _stockItemRepository;
    private readonly IRepository<StockInfo> _stockRepository;
    private readonly IRepository<StockIn> _stockInRepository;
    private readonly IRepository<StockInItem> _stockInItemRepository;
    private readonly IRepository<StockOut> _stockOutRepository;
    private readonly IRepository<StockOutItem> _stockOutItemRepository;
    private readonly IRepository<InventoryLedger> _ledgerRepository;
    private readonly IRepository<WarehouseInfo> _warehouseRepository;
    private readonly IRepository<PickingTask> _pickingTaskRepository;
    private readonly IRepository<PickingTaskItem> _pickingTaskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISerialNumberService _serialNumberService;
    private readonly IStockExtendLineSeqService _stockExtendLineSeq;
    private readonly ILogger<InternalTransferPostingKernel> _logger;

    public InternalTransferPostingKernel(
        IRepository<StockItem> stockItemRepository,
        IRepository<StockInfo> stockRepository,
        IRepository<StockIn> stockInRepository,
        IRepository<StockInItem> stockInItemRepository,
        IRepository<StockOut> stockOutRepository,
        IRepository<StockOutItem> stockOutItemRepository,
        IRepository<InventoryLedger> ledgerRepository,
        IRepository<WarehouseInfo> warehouseRepository,
        IRepository<PickingTask> pickingTaskRepository,
        IRepository<PickingTaskItem> pickingTaskItemRepository,
        IUnitOfWork unitOfWork,
        ISerialNumberService serialNumberService,
        IStockExtendLineSeqService stockExtendLineSeq,
        ILogger<InternalTransferPostingKernel> logger)
    {
        _stockItemRepository = stockItemRepository;
        _stockRepository = stockRepository;
        _stockInRepository = stockInRepository;
        _stockInItemRepository = stockInItemRepository;
        _stockOutRepository = stockOutRepository;
        _stockOutItemRepository = stockOutItemRepository;
        _ledgerRepository = ledgerRepository;
        _warehouseRepository = warehouseRepository;
        _pickingTaskRepository = pickingTaskRepository;
        _pickingTaskItemRepository = pickingTaskItemRepository;
        _unitOfWork = unitOfWork;
        _serialNumberService = serialNumberService;
        _stockExtendLineSeq = stockExtendLineSeq;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<InternalTransferPostingResult> ExecuteAsync(
        InternalTransferPostingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sid = request.SourceStockItemId?.Trim() ?? string.Empty;
        var fromWh = request.FromWarehouseId?.Trim() ?? string.Empty;
        var toWh = request.ToWarehouseId?.Trim() ?? string.Empty;
        if (request.MoveQty <= 0)
            throw new InvalidOperationException("移库数量无效。");
        if (string.IsNullOrEmpty(sid))
            throw new InvalidOperationException("在库明细 ID 不能为空。");
        if (string.IsNullOrEmpty(fromWh) || string.IsNullOrEmpty(toWh))
            throw new InvalidOperationException("仓库不能为空。");
        if (string.IsNullOrWhiteSpace(request.TransferHeaderId))
            throw new InvalidOperationException("移库主单 ID 不能为空。");
        if (string.IsNullOrWhiteSpace(request.TransferBusinessCode))
            throw new InvalidOperationException("移库业务单号不能为空。");
        if (string.IsNullOrWhiteSpace(request.TransferItemLineId))
            throw new InvalidOperationException("移库明细行 ID 不能为空。");
        if (string.IsNullOrWhiteSpace(request.LedgerBizType))
            throw new InvalidOperationException("库存流水 BizType 不能为空。");

        var layer = await _stockItemRepository.GetByIdAsync(sid)
                    ?? throw new InvalidOperationException("在库明细不存在。");
        var sourceStock = await _stockRepository.GetByIdAsync(layer.StockAggregateId?.Trim() ?? string.Empty)
                          ?? throw new InvalidOperationException("汇总库存分桶不存在。");

        var layerWh = layer.WarehouseId?.Trim() ?? string.Empty;
        if (!string.Equals(layerWh, fromWh, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("源在库行与 FromWarehouseId 不一致。");

        if (layer.QtyRepertoryAvailable < request.MoveQty)
            throw new InvalidOperationException("可用库存已变化，请关闭后重新打开本页再试。");

        var pickBlock = await GetPickingBlockReasonsAsync(layer.Id, cancellationToken);
        if (pickBlock.Count > 0)
            throw new InvalidOperationException(string.Join(" ", pickBlock));

        var moveQty = request.MoveQty;
        var toWarehouse = await _warehouseRepository.GetByIdAsync(toWh)
                          ?? throw new InvalidOperationException("目标仓库不存在。");
        if (toWarehouse.Status != 1)
            throw new InvalidOperationException("目标仓库已停用。");

        var targetRegion = RegionTypeCode.Normalize(toWarehouse.RegionType);

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

        var stockInCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockIn);
        var stockOutCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOut);
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var stockOutId = Guid.NewGuid().ToString();
        var stockOutItemId = Guid.NewGuid().ToString();
        var targetStockItemId = Guid.NewGuid().ToString();

        var sourceCodeDisplay = TruncateForSourceCode(request.TransferBusinessCode);
        var inRemark = BuildVirtualInRemark(request.Kind, request.TransferBusinessCode);
        var outRemark = BuildVirtualOutRemark(request.Kind, request.TransferBusinessCode);
        var actor = ActingUserIdNormalizer.Normalize(request.ActingUserId);
        var now = DateTime.UtcNow;

        var virtualStockIn = new StockIn
        {
            Id = stockInId,
            StockInCode = stockInCode,
            StockInType = TransferStockInType,
            WarehouseId = toWh,
            StockInDate = now,
            TotalQuantity = moveQty,
            TotalAmount = lineAmount,
            Status = VirtualDocumentCompletedStatus,
            InspectStatus = 1,
            RegionType = targetRegion,
            Remark = inRemark,
            SourceId = request.TransferHeaderId.Trim(),
            SourceCode = sourceCodeDisplay,
            CreateByUserId = actor,
            CreateTime = now
        };
        await _stockInRepository.AddAsync(virtualStockIn);

        var targetLocationId = string.IsNullOrWhiteSpace(request.ToLocationId) ? layer.LocationId : request.ToLocationId.Trim();
        var virtualInLine = new StockInItem
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
            CreateTime = now
        };
        await _stockInItemRepository.AddAsync(virtualInLine);

        tgt.Qty += moveQty;
        tgt.QtyRepertory = tgt.Qty - tgt.QtyStockOut;
        tgt.QtyRepertoryAvailable = tgt.QtyRepertory - tgt.QtyOccupy - tgt.QtySales;
        tgt.ModifyTime = now;
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
            StockInItemCode = virtualInLine.StockInItemCode,
            StockInItemId = virtualInLine.Id,
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
            CreateTime = now
        };
        targetLayer.SyncDenormalizedComputedFields();
        await _stockItemRepository.AddAsync(targetLayer);

        ApplyPhysicalTransferOut(sourceStock, layer, moveQty);
        if (request.Kind == InternalTransferKind.Manual && layer.QtyRepertory <= 0)
            layer.TransferType = StockItemTransferTypeCodes.ManualTransferSource;
        await _stockRepository.UpdateAsync(sourceStock);
        await _stockItemRepository.UpdateAsync(layer);

        var virtualStockOut = new StockOut
        {
            Id = stockOutId,
            StockOutCode = stockOutCode,
            StockOutType = TransferStockOutType,
            Type = 0,
            WarehouseId = fromWh,
            StockOutDate = now,
            TotalQuantity = moveQty,
            TotalAmount = lineAmount,
            Status = VirtualDocumentCompletedStatus,
            RegionType = RegionTypeCode.Normalize(sourceStock.RegionType),
            Remark = outRemark,
            SourceId = request.TransferHeaderId.Trim(),
            SourceCode = sourceCodeDisplay,
            SellOrderItemId = string.IsNullOrWhiteSpace(layer.SellOrderItemId) ? null : layer.SellOrderItemId.Trim(),
            CreateTime = now,
            CreateByUserId = actor
        };
        await _stockOutRepository.AddAsync(virtualStockOut);

        var virtualOutLine = new StockOutItem
        {
            Id = stockOutItemId,
            StockOutId = stockOutId,
            MaterialId = layer.MaterialId,
            PurchasePn = string.IsNullOrWhiteSpace(layer.PurchasePn) ? null : layer.PurchasePn.Trim(),
            PurchaseBrand = string.IsNullOrWhiteSpace(layer.PurchaseBrand) ? null : layer.PurchaseBrand.Trim(),
            Quantity = moveQty,
            OrderQty = moveQty,
            PlanQty = moveQty,
            PickQty = 0,
            ActualQty = moveQty,
            Price = layer.PurchasePrice,
            Amount = lineAmount,
            StockId = sourceStock.Id,
            StockItemId = layer.Id,
            WarehouseId = fromWh,
            LocationId = layer.LocationId,
            BatchNo = layer.BatchNo,
            CreateTime = now
        };
        await _stockOutItemRepository.AddAsync(virtualOutLine);

        var ledger = new InventoryLedger
        {
            Id = Guid.NewGuid().ToString(),
            BizType = request.LedgerBizType.Trim(),
            BizId = request.TransferHeaderId.Trim(),
            BizLineId = request.TransferItemLineId.Trim(),
            MaterialId = layer.MaterialId,
            WarehouseId = fromWh,
            LocationId = layer.LocationId,
            BatchNo = layer.BatchNo,
            QtyIn = 0,
            QtyOut = moveQty,
            UnitCost = layer.PurchasePrice,
            Amount = -lineAmount,
            Currency = layer.PurchaseCurrency > 0 ? layer.PurchaseCurrency : (short)CurrencyCode.RMB,
            Remark = $"{request.TransferBusinessCode.Trim()}（含虚拟调拨出/入库凭证）",
            CreateTime = now,
            FromWarehouseId = fromWh,
            ToWarehouseId = toWh,
            StockTransferId = request.TransferHeaderId.Trim(),
            SourceStockItemId = sid,
            TargetStockItemId = targetStockItemId,
            CreateByUserId = actor,
            PurchaseOrderItemId = sourceStock.PurchaseOrderItemId,
            PurchaseOrderItemCode = sourceStock.PurchaseOrderItemCode,
            SellOrderItemId = sourceStock.SellOrderItemId,
            SellOrderItemCode = sourceStock.SellOrderItemCode
        };
        await _ledgerRepository.AddAsync(ledger);

        _logger.LogInformation(
            "InternalTransferPostingKernel done Header={Header} MoveQty={Qty} SourceItem={S} TargetItem={T} VOut={Vo} VIn={Vi}",
            request.TransferHeaderId, moveQty, sid, targetStockItemId, stockOutId, stockInId);

        return new InternalTransferPostingResult
        {
            MoveQty = moveQty,
            TargetStockItemId = targetStockItemId,
            TargetStockAggregateId = tgt.Id,
            VirtualStockOutId = stockOutId,
            VirtualStockInId = stockInId,
            StockOutCode = stockOutCode,
            StockInCode = stockInCode
        };
    }

    private static string BuildVirtualInRemark(InternalTransferKind kind, string transferBusinessCode)
    {
        _ = kind;
        var code = transferBusinessCode?.Trim() ?? string.Empty;
        return $"【移库虚拟入库】{code}";
    }

    private static string BuildVirtualOutRemark(InternalTransferKind kind, string transferBusinessCode)
    {
        _ = kind;
        var code = transferBusinessCode?.Trim() ?? string.Empty;
        return $"【移库虚拟出库】{code}";
    }

    private static string? TruncateForSourceCode(string? code)
    {
        var s = code?.Trim() ?? string.Empty;
        if (s.Length <= SourceCodeMaxLen)
            return string.IsNullOrEmpty(s) ? null : s;
        return s[..SourceCodeMaxLen];
    }

    private static void ApplyPhysicalTransferOut(StockInfo stock, StockItem layer, int moveQty)
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
        _ = cancellationToken;
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
