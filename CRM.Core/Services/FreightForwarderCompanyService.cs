using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.System;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class FreightForwarderCompanyService : IFreightForwarderCompanyService
{
    private readonly IRepository<FreightForwarderCompany> _companyRepo;
    private readonly IRepository<FreightForwarderCompanyBank> _bankRepo;
    private readonly IRepository<FinanceReceipt> _receiptRepo;
    private readonly IRepository<FinanceFreightForwarderPayment> _ffPaymentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISerialNumberService _serialNumberService;
    private readonly ILogOperationAppendService _logOperationAppend;
    private readonly IUserService _userService;

    public FreightForwarderCompanyService(
        IRepository<FreightForwarderCompany> companyRepo,
        IRepository<FreightForwarderCompanyBank> bankRepo,
        IRepository<FinanceReceipt> receiptRepo,
        IRepository<FinanceFreightForwarderPayment> ffPaymentRepo,
        IUnitOfWork unitOfWork,
        ISerialNumberService serialNumberService,
        ILogOperationAppendService logOperationAppend,
        IUserService userService)
    {
        _companyRepo = companyRepo;
        _bankRepo = bankRepo;
        _receiptRepo = receiptRepo;
        _ffPaymentRepo = ffPaymentRepo;
        _unitOfWork = unitOfWork;
        _serialNumberService = serialNumberService;
        _logOperationAppend = logOperationAppend;
        _userService = userService;
    }

    public async Task<IReadOnlyList<FreightForwarderCompany>> GetActiveListAsync()
    {
        var all = await _companyRepo.GetAllAsync();
        return all
            .Where(x => x.Status == FreightForwarderCompanyStatusCodes.Active)
            .OrderBy(x => x.CompanyCode)
            .ToList();
    }

    public async Task<IReadOnlyList<FreightForwarderCompany>> GetAllOrderedForAdminAsync()
    {
        var all = await _companyRepo.GetAllAsync();
        return all.OrderBy(x => x.CompanyCode).ThenBy(x => x.Cname).ToList();
    }

    public async Task<FreightForwarderCompany?> GetByIdAsync(string id, bool includeBanks = false)
    {
        var row = await _companyRepo.GetByIdAsync(id);
        if (row == null || !includeBanks) return row;

        var banks = (await _bankRepo.FindAsync(b => b.FreightForwarderCompanyId == id))
            .OrderByDescending(b => b.IsDefault)
            .ThenBy(b => b.BankName)
            .ToList();
        row.Banks = banks;
        return row;
    }

    public async Task<FreightForwarderCompany> CreateAsync(string cname, string? ename, string? remark, string? actingUserId)
    {
        if (string.IsNullOrWhiteSpace(cname))
            throw new ArgumentException("公司中文名不能为空", nameof(cname));

        var code = await _serialNumberService.GenerateNextAsync(ModuleCodes.FreightForwarderCompany);
        var row = new FreightForwarderCompany
        {
            Id = Guid.NewGuid().ToString(),
            CompanyCode = code,
            Cname = cname.Trim(),
            Ename = string.IsNullOrWhiteSpace(ename) ? null : ename.Trim(),
            Status = FreightForwarderCompanyStatusCodes.Active,
            Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim(),
            IsDeleted = false,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
        };
        await _companyRepo.AddAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task<FreightForwarderCompany> UpdateAsync(string id, string cname, string? ename, string? remark, string? actingUserId)
    {
        var row = await _companyRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("货代公司不存在");
        if (string.IsNullOrWhiteSpace(cname))
            throw new ArgumentException("公司中文名不能为空", nameof(cname));

        row.Cname = cname.Trim();
        row.Ename = string.IsNullOrWhiteSpace(ename) ? null : ename.Trim();
        row.Remark = string.IsNullOrWhiteSpace(remark) ? null : remark.Trim();
        row.ModifyTime = DateTime.UtcNow;
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _companyRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task<FreightForwarderCompany> SetStatusAsync(string id, short status, string? actingUserId)
    {
        if (status != FreightForwarderCompanyStatusCodes.Active && status != FreightForwarderCompanyStatusCodes.Inactive)
            throw new ArgumentException("状态无效，应为 1（启用）或 0（停用）。", nameof(status));

        var row = await _companyRepo.GetByIdAsync(id)
            ?? throw new InvalidOperationException("货代公司不存在");
        row.Status = status;
        row.ModifyTime = DateTime.UtcNow;
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _companyRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task SoftDeleteAsync(string id, string? actingUserId)
    {
        var key = id.Trim();
        var row = (await _companyRepo.FindIgnoreFiltersAsync(x => x.Id == key)).FirstOrDefault()
                  ?? throw new InvalidOperationException("货代公司不存在");
        if (row.IsDeleted)
            throw new InvalidOperationException("货代公司已删除");

        await EnsureNotReferencedAsync(key);

        row.IsDeleted = true;
        row.DeletedAt = DateTime.UtcNow;
        row.DeletedByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _companyRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();

        var (actorId, actorName) = await OperationLogActorResolver.ResolveAsync(_userService, actingUserId);
        await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
        {
            BizType = BusinessLogTypes.FreightForwarderCompany,
            RecordId = row.Id,
            RecordCode = row.CompanyCode,
            EntityDisplayName = DeleteLogEntityNames.FreightForwarderCompany,
            OperatorUserId = actorId,
            OperatorUserName = actorName
        });
    }

    public async Task<IReadOnlyList<FreightForwarderCompanyBank>> GetBanksAsync(string companyId)
    {
        var banks = await _bankRepo.FindAsync(b => b.FreightForwarderCompanyId == companyId);
        return banks
            .OrderByDescending(b => b.IsDefault)
            .ThenBy(b => b.BankName)
            .ToList();
    }

    public async Task<FreightForwarderCompanyBank> CreateBankAsync(
        string companyId, string bankName, string? accountName, string? accountNo, byte currency,
        bool isDefault, string? actingUserId)
    {
        _ = await _companyRepo.GetByIdAsync(companyId)
            ?? throw new InvalidOperationException("货代公司不存在");
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("开户银行不能为空", nameof(bankName));

        if (isDefault)
            await ClearDefaultBankAsync(companyId);

        var row = new FreightForwarderCompanyBank
        {
            Id = Guid.NewGuid().ToString(),
            FreightForwarderCompanyId = companyId.Trim(),
            BankName = bankName.Trim(),
            AccountName = string.IsNullOrWhiteSpace(accountName) ? null : accountName.Trim(),
            AccountNo = string.IsNullOrWhiteSpace(accountNo) ? null : accountNo.Trim(),
            Currency = currency,
            IsDefault = isDefault,
            IsDisabled = false,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
        };
        await _bankRepo.AddAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task<FreightForwarderCompanyBank> UpdateBankAsync(
        string bankId, string bankName, string? accountName, string? accountNo, byte currency,
        bool isDefault, bool isDisabled, string? actingUserId)
    {
        var row = await _bankRepo.GetByIdAsync(bankId)
            ?? throw new InvalidOperationException("货代收款账户不存在");
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("开户银行不能为空", nameof(bankName));

        if (isDefault && !row.IsDefault)
            await ClearDefaultBankAsync(row.FreightForwarderCompanyId);

        row.BankName = bankName.Trim();
        row.AccountName = string.IsNullOrWhiteSpace(accountName) ? null : accountName.Trim();
        row.AccountNo = string.IsNullOrWhiteSpace(accountNo) ? null : accountNo.Trim();
        row.Currency = currency;
        row.IsDefault = isDefault;
        row.IsDisabled = isDisabled;
        row.ModifyTime = DateTime.UtcNow;
        row.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _bankRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    public async Task DeleteBankAsync(string bankId, string? actingUserId)
    {
        var row = await _bankRepo.GetByIdAsync(bankId)
            ?? throw new InvalidOperationException("货代收款账户不存在");
        await _bankRepo.DeleteAsync(bankId);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task EnsureNotReferencedAsync(string companyId)
    {
        var receiptReferenced = (await _receiptRepo.FindAsNoTrackingAsync(r => r.FreightForwarderCompanyId == companyId)).Any();
        if (receiptReferenced)
            throw new InvalidOperationException("货代公司已被收款单引用，无法删除");

        var paymentReferenced = (await _ffPaymentRepo.FindAsNoTrackingAsync(p => p.FreightForwarderCompanyId == companyId)).Any();
        if (paymentReferenced)
            throw new InvalidOperationException("货代公司已被货代付款记录引用，无法删除");
    }

    private async Task ClearDefaultBankAsync(string companyId)
    {
        var banks = await _bankRepo.FindAsync(b => b.FreightForwarderCompanyId == companyId && b.IsDefault);
        foreach (var bank in banks)
        {
            bank.IsDefault = false;
            await _bankRepo.UpdateAsync(bank);
        }
    }
}
