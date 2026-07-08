using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class FinanceFreightForwarderPayableService : IFinanceFreightForwarderPayableService
{
    private const short ReceiptApproved = 2;
    private const short ReceiptReceived = 3;

    private readonly IFinanceFreightForwarderPayableListQuery _listQuery;
    private readonly IRepository<FinanceReceipt> _receiptRepo;
    private readonly IRepository<FinanceFreightForwarderPayment> _paymentRepo;
    private readonly IRepository<FreightForwarderCompany> _companyRepo;
    private readonly IRepository<FreightForwarderCompanyBank> _bankRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IUnitOfWork _unitOfWork;

    public FinanceFreightForwarderPayableService(
        IFinanceFreightForwarderPayableListQuery listQuery,
        IRepository<FinanceReceipt> receiptRepo,
        IRepository<FinanceFreightForwarderPayment> paymentRepo,
        IRepository<FreightForwarderCompany> companyRepo,
        IRepository<FreightForwarderCompanyBank> bankRepo,
        IRepository<User> userRepo,
        IUnitOfWork unitOfWork)
    {
        _listQuery = listQuery;
        _receiptRepo = receiptRepo;
        _paymentRepo = paymentRepo;
        _companyRepo = companyRepo;
        _bankRepo = bankRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
    }

    public Task<PagedResult<FinanceFreightForwarderPayableListItem>> GetPagedAsync(
        FinanceFreightForwarderPayableQueryRequest request,
        CancellationToken cancellationToken = default) =>
        _listQuery.GetPagedAsync(request, cancellationToken);

    public async Task<FinanceFreightForwarderPayableDetail?> GetDetailAsync(
        string receiptId, CancellationToken cancellationToken = default)
    {
        var receipt = await _receiptRepo.GetByIdAsync(receiptId);
        if (receipt == null || !receipt.IsFreightForwarderPayment)
            return null;
        if (receipt.Status < ReceiptApproved || receipt.Status == 4)
            return null;

        var payments = (await _paymentRepo.FindAsync(p => p.FinanceReceiptId == receiptId))
            .OrderByDescending(p => p.CreateTime)
            .ThenBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await EnrichPaymentsAsync(payments);

        var paid = payments.Sum(p => p.PaymentAmount);
        var pending = FinanceFreightForwarderPayableStatusHelper.PendingAmount(receipt.ReceiptAmount, paid);
        var status = FinanceFreightForwarderPayableStatusHelper.Compute(receipt.ReceiptAmount, paid);

        string? companyName = null;
        if (!string.IsNullOrWhiteSpace(receipt.FreightForwarderCompanyId))
        {
            var company = await _companyRepo.GetByIdAsync(receipt.FreightForwarderCompanyId);
            companyName = company?.Cname;
            receipt.FreightForwarderCompanyName = companyName;
        }

        return new FinanceFreightForwarderPayableDetail
        {
            Receipt = receipt,
            FreightForwarderCompanyName = companyName,
            PaidAmount = paid,
            PendingAmount = pending,
            PayableStatus = status,
            Payments = payments
        };
    }

    public async Task<FinanceFreightForwarderPayment> CreatePaymentAsync(
        string receiptId, CreateFinanceFreightForwarderPaymentRequest request, string? actingUserId)
    {
        var receipt = await _receiptRepo.GetByIdAsync(receiptId)
            ?? throw new InvalidOperationException("收款单不存在");
        if (!receipt.IsFreightForwarderPayment)
            throw new InvalidOperationException("该收款单未标记货代付款");
        if (receipt.Status < ReceiptApproved || receipt.Status == 4)
            throw new InvalidOperationException("收款单未审核通过，不可登记货代付款");

        var companyId = string.IsNullOrWhiteSpace(request.FreightForwarderCompanyId)
            ? receipt.FreightForwarderCompanyId
            : request.FreightForwarderCompanyId.Trim();
        if (string.IsNullOrWhiteSpace(companyId))
            throw new InvalidOperationException("首次付款前必须选择货代公司");

        _ = await _companyRepo.GetByIdAsync(companyId)
            ?? throw new InvalidOperationException("货代公司不存在");

        if (string.IsNullOrWhiteSpace(receipt.FreightForwarderCompanyId))
        {
            receipt.FreightForwarderCompanyId = companyId;
            receipt.ModifyTime = DateTime.UtcNow;
            receipt.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _receiptRepo.UpdateAsync(receipt);
        }
        else if (!string.Equals(receipt.FreightForwarderCompanyId, companyId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("货代公司与收款单已选货代不一致");
        }

        if (request.PaymentAmount <= 0m)
            throw new ArgumentException("付款金额必须大于 0", nameof(request.PaymentAmount));

        var existing = await _paymentRepo.FindAsync(p => p.FinanceReceiptId == receiptId);
        var paid = existing.Sum(p => p.PaymentAmount);
        var pending = FinanceFreightForwarderPayableStatusHelper.PendingAmount(receipt.ReceiptAmount, paid);
        if (request.PaymentAmount > pending)
            throw new InvalidOperationException($"付款金额不能超过待付余额 {pending:F2}");

        if (!string.IsNullOrWhiteSpace(request.FfCompanyBankId))
        {
            var bank = await _bankRepo.GetByIdAsync(request.FfCompanyBankId)
                ?? throw new InvalidOperationException("货代收款账户不存在");
            if (!string.Equals(bank.FreightForwarderCompanyId, companyId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("货代收款账户与货代公司不匹配");
            if (bank.IsDisabled)
                throw new InvalidOperationException("货代收款账户已停用");
        }

        var payment = new FinanceFreightForwarderPayment
        {
            Id = Guid.NewGuid().ToString(),
            FinanceReceiptId = receiptId,
            FreightForwarderCompanyId = companyId,
            PaymentAmount = request.PaymentAmount,
            PaymentCurrency = request.PaymentCurrency > 0 ? request.PaymentCurrency : receipt.ReceiptCurrency,
            PaymentMode = request.PaymentMode > 0 ? request.PaymentMode : (short)1,
            CompanyBankId = string.IsNullOrWhiteSpace(request.CompanyBankId) ? null : request.CompanyBankId.Trim(),
            FfCompanyBankId = string.IsNullOrWhiteSpace(request.FfCompanyBankId) ? null : request.FfCompanyBankId.Trim(),
            BankSlipNo = string.IsNullOrWhiteSpace(request.BankSlipNo) ? null : request.BankSlipNo.Trim(),
            PaymentDate = PostgreSqlDateTime.ToUtc(request.PaymentDate),
            PaymentUserId = ActingUserIdNormalizer.Normalize(actingUserId),
            Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
        };

        await _paymentRepo.AddAsync(payment);
        await _unitOfWork.SaveChangesAsync();
        return payment;
    }

    public async Task<FinanceReceipt> UpdateReceiptFfCompanyAsync(
        string receiptId, string freightForwarderCompanyId, string? actingUserId)
    {
        var receipt = await _receiptRepo.GetByIdAsync(receiptId)
            ?? throw new InvalidOperationException("收款单不存在");
        if (!receipt.IsFreightForwarderPayment)
            throw new InvalidOperationException("该收款单未标记货代付款");

        var companyId = freightForwarderCompanyId.Trim();
        if (string.IsNullOrWhiteSpace(companyId))
            throw new ArgumentException("货代公司不能为空", nameof(freightForwarderCompanyId));

        _ = await _companyRepo.GetByIdAsync(companyId)
            ?? throw new InvalidOperationException("货代公司不存在");

        var payments = (await _paymentRepo.FindAsync(p => p.FinanceReceiptId == receiptId)).ToList();
        if (payments.Count > 0
            && !string.IsNullOrWhiteSpace(receipt.FreightForwarderCompanyId)
            && !string.Equals(receipt.FreightForwarderCompanyId, companyId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("已有付款记录，不可修改货代公司");
        }

        receipt.FreightForwarderCompanyId = companyId;
        receipt.ModifyTime = DateTime.UtcNow;
        receipt.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _receiptRepo.UpdateAsync(receipt);
        await _unitOfWork.SaveChangesAsync();
        return receipt;
    }

    private async Task EnrichPaymentsAsync(IReadOnlyList<FinanceFreightForwarderPayment> payments)
    {
        if (payments.Count == 0) return;

        var companyIds = payments.Select(p => p.FreightForwarderCompanyId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var companies = (await _companyRepo.FindAsync(c => companyIds.Contains(c.Id))).ToList();
        var companyMap = companies.ToDictionary(c => c.Id, c => c.Cname, StringComparer.OrdinalIgnoreCase);

        var bankIds = payments
            .Select(p => p.FfCompanyBankId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var banks = bankIds.Count == 0
            ? new List<FreightForwarderCompanyBank>()
            : (await _bankRepo.FindAsync(b => bankIds.Contains(b.Id))).ToList();
        var bankMap = banks.ToDictionary(b => b.Id, b => b.BankName, StringComparer.OrdinalIgnoreCase);

        var userIds = payments
            .Select(p => p.PaymentUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : (await _userRepo.FindAsync(u => userIds.Contains(u.Id))).ToList();
        var userMap = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Id))
            .ToDictionary(
                u => u.Id.Trim(),
                u => EntityLookupService.FormatUserLoginName(u) ?? u.Id,
                StringComparer.OrdinalIgnoreCase);

        foreach (var p in payments)
        {
            if (companyMap.TryGetValue(p.FreightForwarderCompanyId, out var cname))
                p.FreightForwarderCompanyName = cname;
            if (!string.IsNullOrWhiteSpace(p.FfCompanyBankId)
                && bankMap.TryGetValue(p.FfCompanyBankId, out var bname))
                p.FfCompanyBankName = bname;
            if (!string.IsNullOrWhiteSpace(p.PaymentUserId)
                && userMap.TryGetValue(p.PaymentUserId, out var uname))
                p.PaymentUserName = uname;
        }
    }
}
