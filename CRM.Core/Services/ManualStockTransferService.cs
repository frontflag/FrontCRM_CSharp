using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Services.InternalTransfer;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

public sealed class ManualStockTransferService : IManualStockTransferService
{
    private readonly IRepository<StockItem> _stockItemRepository;
    private readonly IRepository<StockInfo> _stockRepository;
    private readonly IRepository<StockTransferManual> _transferManualRepository;
    private readonly IRepository<StockTransferItemManual> _transferItemManualRepository;
    private readonly IRepository<MaterialInfo> _materialRepository;
    private readonly IRepository<WarehouseInfo> _warehouseRepository;
    private readonly IRepository<PickingTask> _pickingTaskRepository;
    private readonly IRepository<PickingTaskItem> _pickingTaskItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISerialNumberService _serialNumberService;
    private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
    private readonly IInternalTransferPostingKernel _postingKernel;
    private readonly ILogger<ManualStockTransferService> _logger;

    public ManualStockTransferService(
        IRepository<StockItem> stockItemRepository,
        IRepository<StockInfo> stockRepository,
        IRepository<StockTransferManual> transferManualRepository,
        IRepository<StockTransferItemManual> transferItemManualRepository,
        IRepository<MaterialInfo> materialRepository,
        IRepository<WarehouseInfo> warehouseRepository,
        IRepository<PickingTask> pickingTaskRepository,
        IRepository<PickingTaskItem> pickingTaskItemRepository,
        IUnitOfWork unitOfWork,
        ISerialNumberService serialNumberService,
        ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
        IInternalTransferPostingKernel postingKernel,
        ILogger<ManualStockTransferService> logger)
    {
        _stockItemRepository = stockItemRepository;
        _stockRepository = stockRepository;
        _transferManualRepository = transferManualRepository;
        _transferItemManualRepository = transferItemManualRepository;
        _materialRepository = materialRepository;
        _warehouseRepository = warehouseRepository;
        _pickingTaskRepository = pickingTaskRepository;
        _pickingTaskItemRepository = pickingTaskItemRepository;
        _unitOfWork = unitOfWork;
        _serialNumberService = serialNumberService;
        _purchasedStockAvailableSync = purchasedStockAvailableSync;
        _postingKernel = postingKernel;
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

        if (toWarehouse.Status != 1)
            throw new InvalidOperationException("目标仓库已停用。");

        var latePick = await GetPickingBlockReasonsAsync(layer.Id, cancellationToken);
        if (latePick.Count > 0)
            throw new InvalidOperationException(string.Join(" ", latePick));

        if (layer.QtyRepertoryAvailable < moveQty)
            throw new InvalidOperationException("可用库存已变化，请关闭后重新打开本页再试。");

        var transferCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockTransferManual);
        var transferId = Guid.NewGuid().ToString();
        var itemManualId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);

        var kernelRequest = new InternalTransferPostingRequest
        {
            Kind = InternalTransferKind.Manual,
            MoveQty = moveQty,
            SourceStockItemId = sid,
            FromWarehouseId = fromWh,
            ToWarehouseId = toWh,
            ToLocationId = request.ToLocationId,
            TransferHeaderId = transferId,
            TransferBusinessCode = transferCode,
            TransferItemLineId = itemManualId,
            LedgerBizType = StockLedgerBizType.ManualTransfer,
            ActingUserId = actor
        };

        var posted = await _postingKernel.ExecuteAsync(kernelRequest, cancellationToken);

        var sourceStockAfter = await _stockRepository.GetByIdAsync(sourceStock.Id)
                               ?? throw new InvalidOperationException("汇总库存分桶不存在。");
        var targetStockAfter = await _stockRepository.GetByIdAsync(posted.TargetStockAggregateId)
                               ?? throw new InvalidOperationException("目标汇总库存分桶不存在。");

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
            TargetStockItemId = posted.TargetStockItemId,
            Qty = posted.MoveQty,
            CreateTime = now
        };
        await _transferItemManualRepository.AddAsync(itemRow);

        await _unitOfWork.SaveChangesAsync();

        var changed = new List<StockInfo> { sourceStockAfter, targetStockAfter };
        await _purchasedStockAvailableSync.TryRecalculateFromChangedStockInfosAsync(changed, cancellationToken);

        _logger.LogInformation(
            "ManualStockTransfer done TransferCode={Code} MoveQty={Qty} SourceItem={S} TargetItem={T} VOut={Vo} VIn={Vi}",
            transferCode, posted.MoveQty, sid, posted.TargetStockItemId, posted.VirtualStockOutId, posted.VirtualStockInId);

        return new ManualStockTransferExecuteResultDto
        {
            StockTransferManualId = transferId,
            TransferCode = transferCode,
            MoveQty = posted.MoveQty,
            TargetStockItemId = posted.TargetStockItemId,
            TargetStockAggregateId = posted.TargetStockAggregateId,
            VirtualStockOutId = posted.VirtualStockOutId,
            VirtualStockInId = posted.VirtualStockInId,
            StockOutCode = posted.StockOutCode,
            StockInCode = posted.StockInCode
        };
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
}
