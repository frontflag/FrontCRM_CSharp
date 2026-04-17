using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class CustomsDeclarationService : ICustomsDeclarationService
{
    private readonly IRepository<CustomsDeclaration> _declarationRepo;
    private readonly IRepository<StockTransfer> _transferRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CustomsDeclarationService(
        IRepository<CustomsDeclaration> declarationRepo,
        IRepository<StockTransfer> transferRepo,
        IUnitOfWork unitOfWork)
    {
        _declarationRepo = declarationRepo;
        _transferRepo = transferRepo;
        _unitOfWork = unitOfWork;
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

    public async Task CompleteDeclarationAndTransferAsync(string declarationId, string? actingUserId)
    {
        var dec = await _declarationRepo.GetByIdAsync(declarationId.Trim())
                  ?? throw new InvalidOperationException("报关单不存在");
        if (dec.InternalStatus != CustomsDeclarationInternalStatus.Processing)
            throw new InvalidOperationException("仅「报关中」状态可执行报关完成；当前状态不符合。");
        if (dec.CustomsClearanceStatus != CustomsClearanceStatusCodes.Cleared)
            throw new InvalidOperationException("海关状态须为「已结关」后才可报关完成。");

        var existing = (await _transferRepo.FindAsync(t => t.CustomsDeclarationId == dec.Id)).Any();
        if (existing)
            throw new InvalidOperationException("该报关单已存在移库单，请勿重复提交。");

        throw new InvalidOperationException(
            "报关完成与境内移库过账（扣减源 StockItem、虚拟入库、stockledger STOCK_TRANS）将在下一开发迭代接入 InventoryCenter；数据库表与接口已就绪，详见 document/实现方案/报关_移库.md。");
    }
}
