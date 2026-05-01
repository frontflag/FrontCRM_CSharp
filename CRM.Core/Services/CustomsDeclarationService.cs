using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Services.InternalTransfer;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

public class CustomsDeclarationService : ICustomsDeclarationService
{
    private readonly IRepository<CustomsDeclaration> _declarationRepo;
    private readonly IRepository<CustomsDeclarationItem> _declarationItemRepo;
    private readonly IRepository<StockTransfer> _transferRepo;
    private readonly IRepository<StockTransferItem> _transferItemRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<StockInfo> _stockRepo;
    private readonly IRepository<WarehouseInfo> _warehouseRepo;
    private readonly IInternalTransferPostingKernel _postingKernel;
    private readonly ISerialNumberService _serialNumberService;
    private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogOperationAppendService _logOperationAppend;
    private readonly ILogger<CustomsDeclarationService> _logger;

    public CustomsDeclarationService(
        IRepository<CustomsDeclaration> declarationRepo,
        IRepository<CustomsDeclarationItem> declarationItemRepo,
        IRepository<StockTransfer> transferRepo,
        IRepository<StockTransferItem> transferItemRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<StockInfo> stockRepo,
        IRepository<WarehouseInfo> warehouseRepo,
        IInternalTransferPostingKernel postingKernel,
        ISerialNumberService serialNumberService,
        ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
        IUnitOfWork unitOfWork,
        ILogOperationAppendService logOperationAppend,
        ILogger<CustomsDeclarationService> logger)
    {
        _declarationRepo = declarationRepo;
        _declarationItemRepo = declarationItemRepo;
        _transferRepo = transferRepo;
        _transferItemRepo = transferItemRepo;
        _stockItemRepo = stockItemRepo;
        _stockRepo = stockRepo;
        _warehouseRepo = warehouseRepo;
        _postingKernel = postingKernel;
        _serialNumberService = serialNumberService;
        _purchasedStockAvailableSync = purchasedStockAvailableSync;
        _unitOfWork = unitOfWork;
        _logOperationAppend = logOperationAppend;
        _logger = logger;
    }

    public async Task<CustomsDeclaration?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return await _declarationRepo.GetByIdAsync(id.Trim());
    }

    public async Task<CustomsDeclaration?> GetByStockOutRequestIdAsync(string stockOutRequestId)
    {
        if (string.IsNullOrWhiteSpace(stockOutRequestId))
            return null;
        var key = stockOutRequestId.Trim();
        var list = await _declarationRepo.FindAsync(x => x.StockOutRequestId == key);
        return list.FirstOrDefault();
    }

    public async Task SetCustomsClearanceStatusAsync(string declarationId, short customsClearanceStatus, string? actingUserId)
    {
        var dec = await _declarationRepo.GetByIdAsync(declarationId.Trim())
                  ?? throw new InvalidOperationException("报关单不存在");
        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Voided)
            throw new InvalidOperationException("报关单已作废，不能修改海关状态");
        if (dec.InternalStatus == CustomsDeclarationInternalStatus.Completed)
            throw new InvalidOperationException("报关单已完成，不能修改海关状态");

        dec.CustomsClearanceStatus = customsClearanceStatus;
        dec.ModifyTime = DateTime.UtcNow;
        dec.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _declarationRepo.UpdateAsync(dec);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task CompleteDeclarationAndTransferAsync(string declarationId, string? actingUserId)
    {
        var decId = declarationId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(decId))
            throw new InvalidOperationException("报关单 ID 不能为空。");

        var dec = await _declarationRepo.GetByIdAsync(decId)
                  ?? throw new InvalidOperationException("报关单不存在");
        if (dec.IsDeleted)
            throw new InvalidOperationException("报关单已删除。");
        if (dec.InternalStatus != CustomsDeclarationInternalStatus.Processing)
            throw new InvalidOperationException("仅「报关中」状态可执行报关完成；当前状态不符合。");
        if (dec.CustomsClearanceStatus != CustomsClearanceStatusCodes.Cleared)
            throw new InvalidOperationException("海关状态须为「已结关」后才可报关完成。");

        var existingTransfers = (await _transferRepo.FindAsync(t =>
            t.CustomsDeclarationId == dec.Id && !t.IsDeleted)).ToList();
        if (existingTransfers.Count > 0)
            throw new InvalidOperationException("该报关单已存在移库单，请勿重复提交。");

        var fromWh = dec.FromWarehouseId?.Trim() ?? string.Empty;
        var toWh = dec.ToWarehouseId?.Trim() ?? string.Empty;
        var sorId = dec.StockOutRequestId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(fromWh) || string.IsNullOrEmpty(toWh))
            throw new InvalidOperationException("报关单 From/To 仓库不能为空。");
        if (string.IsNullOrEmpty(sorId))
            throw new InvalidOperationException("报关单关联出库通知不能为空。");

        var fromWarehouse = await _warehouseRepo.GetByIdAsync(fromWh)
                            ?? throw new InvalidOperationException("源仓库不存在。");
        var toWarehouse = await _warehouseRepo.GetByIdAsync(toWh)
                          ?? throw new InvalidOperationException("目标仓库不存在。");
        if (RegionTypeCode.Normalize(fromWarehouse.RegionType) != RegionTypeCode.Overseas)
            throw new InvalidOperationException("报关进口迁库要求源仓为境外仓。");
        if (RegionTypeCode.Normalize(toWarehouse.RegionType) != RegionTypeCode.Domestic)
            throw new InvalidOperationException("报关进口迁库要求目标仓为境内仓。");
        if (toWarehouse.Status != 1)
            throw new InvalidOperationException("目标仓库已停用。");

        var items = (await _declarationItemRepo.FindAsync(x => x.DeclarationId == dec.Id && !x.IsDeleted))
            .OrderBy(x => x.LineNo)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (items.Count == 0)
            throw new InvalidOperationException("报关单无有效明细行，不能完成移库。");

        foreach (var line in items)
        {
            if (line.DeclareQty <= 0)
                throw new InvalidOperationException($"报关明细行 {line.LineNo} 申报数量无效。");
            var sorLine = line.StockOutRequestId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(sorLine) || !string.Equals(sorLine, sorId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"报关明细行 {line.LineNo} 的出库通知须与报关头一致。");

            var sid = line.SourceStockItemId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(sid))
                throw new InvalidOperationException($"报关明细行 {line.LineNo} 缺少源在库明细。");

            var layer = await _stockItemRepo.GetByIdAsync(sid)
                        ?? throw new InvalidOperationException($"报关明细行 {line.LineNo} 源在库明细不存在。");
            var layerWh = layer.WarehouseId?.Trim() ?? string.Empty;
            if (!string.Equals(layerWh, fromWh, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"报关明细行 {line.LineNo}：源在库行所在仓库与报关头 FromWarehouseId 不一致。");
            if (layer.QtyRepertoryAvailable < line.DeclareQty)
                throw new InvalidOperationException(
                    $"报关明细行 {line.LineNo}：可用库存不足（当前 {layer.QtyRepertoryAvailable}，申报 {line.DeclareQty}）。");
        }

        var transferCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockTransfer);
        var transferId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);

        var lineResults = new List<(CustomsDeclarationItem DecItem, string TransferItemId, InternalTransferPostingResult Posted)>();
        var changedAggregateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in items)
        {
            var transferItemId = Guid.NewGuid().ToString();
            var sid = line.SourceStockItemId!.Trim();
            var layer = await _stockItemRepo.GetByIdAsync(sid)
                        ?? throw new InvalidOperationException($"报关明细行 {line.LineNo} 源在库明细不存在。");
            var sourceStock = await _stockRepo.GetByIdAsync(layer.StockAggregateId?.Trim() ?? string.Empty)
                              ?? throw new InvalidOperationException("汇总库存分桶不存在。");
            changedAggregateIds.Add(sourceStock.Id);

            var kernelRequest = new InternalTransferPostingRequest
            {
                Kind = InternalTransferKind.Customs,
                MoveQty = line.DeclareQty,
                SourceStockItemId = sid,
                FromWarehouseId = fromWh,
                ToWarehouseId = toWh,
                ToLocationId = null,
                TransferHeaderId = transferId,
                TransferBusinessCode = transferCode,
                TransferItemLineId = transferItemId,
                LedgerBizType = StockLedgerBizType.StockTransfer,
                ActingUserId = actor
            };

            var posted = await _postingKernel.ExecuteAsync(kernelRequest, CancellationToken.None);
            changedAggregateIds.Add(posted.TargetStockAggregateId);
            lineResults.Add((line, transferItemId, posted));
        }

        var header = new StockTransfer
        {
            Id = transferId,
            TransferCode = transferCode,
            BizScene = StockTransferBizScene.CustomsImport,
            CustomsDeclarationId = dec.Id,
            FromWarehouseId = fromWh,
            ToWarehouseId = toWh,
            Status = StockTransferStatus.Confirmed,
            ConfirmedTime = now,
            ConfirmedByUserId = actor,
            CreateByUserId = actor,
            CreateTime = now,
            IsDeleted = false
        };
        await _transferRepo.AddAsync(header);

        foreach (var (decItem, transferItemId, posted) in lineResults)
        {
            var row = new StockTransferItem
            {
                Id = transferItemId,
                StockTransferId = transferId,
                SourceStockItemId = decItem.SourceStockItemId!.Trim(),
                CustomsDeclarationItemId = decItem.Id,
                StockOutRequestId = sorId,
                Qty = posted.MoveQty,
                TargetStockItemId = posted.TargetStockItemId,
                CreateTime = now,
                IsDeleted = false
            };
            await _transferItemRepo.AddAsync(row);
        }

        dec.InternalStatus = CustomsDeclarationInternalStatus.Completed;
        dec.ModifyTime = now;
        dec.ModifyByUserId = actor;
        await _declarationRepo.UpdateAsync(dec);

        await _unitOfWork.SaveChangesAsync();

        var changedStocks = new List<StockInfo>();
        foreach (var aggId in changedAggregateIds)
        {
            var s = await _stockRepo.GetByIdAsync(aggId);
            if (s != null)
                changedStocks.Add(s);
        }

        await _purchasedStockAvailableSync.TryRecalculateFromChangedStockInfosAsync(changedStocks, CancellationToken.None);

        _logger.LogInformation(
            "CustomsDeclaration complete+transfer DeclarationId={D} TransferCode={Code} Lines={N} TransferId={T}",
            dec.Id, transferCode, lineResults.Count, transferId);
    }

    /// <inheritdoc />
    public async Task DeleteDeclarationAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("报关单 ID 不能为空", nameof(id));

        var row = await _declarationRepo.GetByIdAsync(id.Trim())
                  ?? throw new InvalidOperationException("报关单不存在");
        if (row.InternalStatus == CustomsDeclarationInternalStatus.Completed)
            throw new InvalidOperationException("已完成报关单不能普通删除");

        await SoftDeleteLinkedStockTransfersAsync(row.Id);

        var items = (await _declarationItemRepo.FindAsync(x => x.DeclarationId == row.Id)).ToList();
        foreach (var item in items)
            await _declarationItemRepo.DeleteAsync(item.Id);
        await _declarationRepo.DeleteAsync(row.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ForceDeleteDeclarationAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName)
    {
        if (string.IsNullOrWhiteSpace(confirmBillCode))
            throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
        if (string.IsNullOrWhiteSpace(actingUserId))
            throw new ArgumentException("操作人不能为空", nameof(actingUserId));

        var row = await _declarationRepo.GetByIdAsync(id.Trim())
                  ?? throw new InvalidOperationException("报关单不存在");
        if (!string.Equals(confirmBillCode.Trim(), row.DeclarationCode.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("确认单号不匹配，已拒绝删除");

        await SoftDeleteLinkedStockTransfersAsync(row.Id);

        var items = (await _declarationItemRepo.FindAsync(x => x.DeclarationId == row.Id)).ToList();
        foreach (var item in items)
            await _declarationItemRepo.DeleteAsync(item.Id);
        await _declarationRepo.DeleteAsync(row.Id);
        await _unitOfWork.SaveChangesAsync();

        var recordCode = string.IsNullOrWhiteSpace(row.DeclarationCode) ? null : row.DeclarationCode.Trim();
        await _logOperationAppend.AppendAsync(
            BusinessLogTypes.CustomsDeclaration,
            row.Id,
            recordCode,
            "报关单强制删除",
            actingUserId.Trim(),
            string.IsNullOrWhiteSpace(actingUserName) ? null : actingUserName.Trim(),
            $"强制删除报关单 DeclarationId={row.Id}，确认单号={recordCode}，明细行数={items.Count}",
            reason: null);
    }

    private async Task SoftDeleteLinkedStockTransfersAsync(string customsDeclarationId)
    {
        var key = customsDeclarationId.Trim();
        var transfers = (await _transferRepo.FindAsync(t => t.CustomsDeclarationId == key && !t.IsDeleted)).ToList();
        foreach (var transfer in transfers)
        {
            var lines = (await _transferItemRepo.FindAsync(x => x.StockTransferId == transfer.Id)).ToList();
            foreach (var line in lines)
            {
                if (!line.IsDeleted)
                {
                    line.IsDeleted = true;
                    await _transferItemRepo.UpdateAsync(line);
                }
            }

            transfer.IsDeleted = true;
            await _transferRepo.UpdateAsync(transfer);
        }
    }
}
