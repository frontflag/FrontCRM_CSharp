using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.System;
using CRM.Core.Services.InternalTransfer;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

public class CustomsDeclarationService : ICustomsDeclarationService
{
    private readonly IRepository<CustomsDeclaration> _declarationRepo;
    private readonly IRepository<CustomsDeclarationItem> _declarationItemRepo;
    private readonly IRepository<Packing> _packingRepo;
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
        IRepository<Packing> packingRepo,
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
        _packingRepo = packingRepo;
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
        var items = await _declarationItemRepo.FindAsync(x => x.StockOutRequestId == key && !x.IsDeleted);
        var decId = items.Select(x => x.DeclarationId?.Trim())
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));
        if (string.IsNullOrEmpty(decId))
            return null;
        return await _declarationRepo.GetByIdAsync(decId);
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
    public Task CompleteDeclarationAndTransferAsync(string declarationId, string? actingUserId)
    {
        _ = declarationId;
        _ = actingUserId;
        throw new InvalidOperationException(
            "报关 V2 已废弃「报关完成+移库一步过账」，请使用报关出库/入库流程。");
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

        await ClearPackingDeclarationLinksAsync(row, null);

        await _unitOfWork.SaveChangesAsync();

        await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
        {
            BizType = BusinessLogTypes.CustomsDeclaration,
            RecordId = row.Id,
            RecordCode = row.DeclarationCode,
            EntityDisplayName = DeleteLogEntityNames.CustomsDeclaration,
            ExtraDetail = $"明细行数={items.Count}"
        });
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

        await ClearPackingDeclarationLinksAsync(row, actingUserId);

        await _unitOfWork.SaveChangesAsync();

        await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
        {
            BizType = BusinessLogTypes.CustomsDeclaration,
            RecordId = row.Id,
            RecordCode = row.DeclarationCode,
            EntityDisplayName = DeleteLogEntityNames.CustomsDeclaration,
            IsForceDelete = true,
            ForceConfirmBillCode = confirmBillCode.Trim(),
            OperatorUserId = actingUserId.Trim(),
            OperatorUserName = actingUserName?.Trim(),
            OperationDescOverride =
                $"强制删除报关单 DeclarationId={row.Id}，确认单号={row.DeclarationCode}，明细行数={items.Count}"
        });
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

    private async Task ClearPackingDeclarationLinksAsync(CustomsDeclaration row, string? actingUserId)
    {
        var decId = row.Id.Trim();
        var packings = (await _packingRepo.FindAsync(p =>
            !p.IsDeleted && p.CustomsDeclarationId != null && p.CustomsDeclarationId == decId)).ToList();

        if (!string.IsNullOrWhiteSpace(row.PackingId))
        {
            var byHeader = await _packingRepo.GetByIdAsync(row.PackingId.Trim());
            if (byHeader != null
                && !byHeader.IsDeleted
                && string.Equals(byHeader.CustomsDeclarationId?.Trim(), decId, StringComparison.OrdinalIgnoreCase)
                && packings.All(p => !string.Equals(p.Id, byHeader.Id, StringComparison.OrdinalIgnoreCase)))
            {
                packings.Add(byHeader);
            }
        }

        if (packings.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        foreach (var packing in packings)
        {
            packing.CustomsDeclarationId = null;
            packing.ModifyTime = now;
            packing.ModifyByUserId = actor;
            await _packingRepo.UpdateAsync(packing);
        }
    }
}
