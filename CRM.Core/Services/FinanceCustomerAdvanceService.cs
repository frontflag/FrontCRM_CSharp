using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class FinanceCustomerAdvanceService : IFinanceCustomerAdvanceService
{
    private readonly IRepository<FinanceCustomerAdvance> _advanceRepo;
    private readonly IRepository<FinanceCustomerAdvanceLedger> _ledgerRepo;
    private readonly IRepository<FinanceReceipt> _receiptRepo;
    private readonly IRepository<FinanceReceiptItem> _receiptItemRepo;
    private readonly IFinanceCustomerAdvanceListQuery _listQuery;
    private readonly IUnitOfWork? _unitOfWork;

    public FinanceCustomerAdvanceService(
        IRepository<FinanceCustomerAdvance> advanceRepo,
        IRepository<FinanceCustomerAdvanceLedger> ledgerRepo,
        IRepository<FinanceReceipt> receiptRepo,
        IRepository<FinanceReceiptItem> receiptItemRepo,
        IFinanceCustomerAdvanceListQuery listQuery,
        IUnitOfWork? unitOfWork = null)
    {
        _advanceRepo = advanceRepo;
        _ledgerRepo = ledgerRepo;
        _receiptRepo = receiptRepo;
        _receiptItemRepo = receiptItemRepo;
        _listQuery = listQuery;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<FinanceCustomerAdvanceBalanceDto?> GetBalanceAsync(
        string customerId,
        short currency,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return null;

        var row = (await _advanceRepo.FindAsync(a =>
            !a.IsDeleted
            && a.CustomerId == customerId.Trim()
            && a.Currency == currency)).FirstOrDefault();

        if (row == null)
        {
            return new FinanceCustomerAdvanceBalanceDto
            {
                CustomerId = customerId.Trim(),
                Currency = currency,
                Balance = 0m,
                TotalIn = 0m,
                TotalApplied = 0m
            };
        }

        return new FinanceCustomerAdvanceBalanceDto
        {
            CustomerId = row.CustomerId,
            Currency = row.Currency,
            Balance = row.Balance,
            TotalIn = row.TotalIn,
            TotalApplied = row.TotalApplied
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceCustomerAdvanceBalanceDto>> GetBalancesForCustomerAsync(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Array.Empty<FinanceCustomerAdvanceBalanceDto>();

        var rows = (await _advanceRepo.FindAsync(a =>
            !a.IsDeleted && a.CustomerId == customerId.Trim())).ToList();

        return rows.Select(row => new FinanceCustomerAdvanceBalanceDto
        {
            CustomerId = row.CustomerId,
            Currency = row.Currency,
            Balance = row.Balance,
            TotalIn = row.TotalIn,
            TotalApplied = row.TotalApplied
        }).ToList();
    }

    /// <inheritdoc />
    public Task<PagedResult<FinanceCustomerAdvance>> GetPagedAsync(
        FinanceCustomerAdvanceQueryRequest request,
        CancellationToken cancellationToken = default) =>
        _listQuery.GetPagedAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<PagedResult<FinanceCustomerAdvanceLedger>> GetLedgerPagedAsync(
        FinanceCustomerAdvanceLedgerQueryRequest request,
        CancellationToken cancellationToken = default) =>
        _listQuery.GetLedgerPagedAsync(request, cancellationToken);

    /// <inheritdoc />
    public bool IsReceiptItemAlreadyCreditedToPool(FinanceReceiptItem item) =>
        item.AdvancePoolAmount > 0m;

    /// <inheritdoc />
    public async Task TryCreditExplicitAdvanceOnReceiptApprovedAsync(string receiptId, string? actingUserId = null)
    {
        if (string.IsNullOrWhiteSpace(receiptId))
            return;

        var receipt = await _receiptRepo.GetByIdAsync(receiptId.Trim());
        if (receipt == null)
            return;

        var items = (await _receiptItemRepo.FindAsync(i => i.FinanceReceiptId == receipt.Id)).ToList();
        foreach (var item in items.Where(i => i.ReceiptPurpose == FinanceReceiptPurposeCode.Advance))
        {
            if (IsReceiptItemAlreadyCreditedToPool(item))
                continue;

            var amount = item.ReceiptConvertAmount - item.VerifiedAmount - item.AdvancePoolAmount;
            if (amount <= 0m)
                continue;

            await CreditFromReceiptItemAsync(
                receipt,
                item,
                FinanceCustomerAdvanceLedgerTypeCode.In,
                actingUserId,
                "显式预收入账");

            item.AdvancePoolAmount += amount;
            item.ModifyTime = DateTime.UtcNow;
            await _receiptItemRepo.UpdateAsync(item);
        }

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task CreditFromReceiptItemAsync(
        FinanceReceipt receipt,
        FinanceReceiptItem item,
        short ledgerType,
        string? actingUserId = null,
        string? remark = null)
    {
        var amount = item.ReceiptConvertAmount - item.VerifiedAmount - item.AdvancePoolAmount;
        if (amount <= 0m)
            return;

        await CreditCoreAsync(
            receipt.CustomerId,
            receipt.CustomerName,
            receipt.ReceiptCurrency,
            amount,
            ledgerType,
            receipt.Id,
            item.Id,
            string.IsNullOrWhiteSpace(item.AdvanceSellOrderId) ? item.SellOrderId : item.AdvanceSellOrderId,
            null,
            null,
            remark,
            actingUserId,
            receipt);
    }

    /// <inheritdoc />
    public async Task CreditAutoInExcessAsync(
        FinanceReceipt receipt,
        FinanceReceiptItem item,
        decimal amount,
        string? actingUserId = null)
    {
        if (amount <= 0m)
            return;

        await CreditCoreAsync(
            receipt.CustomerId,
            receipt.CustomerName,
            receipt.ReceiptCurrency,
            amount,
            FinanceCustomerAdvanceLedgerTypeCode.AutoIn,
            receipt.Id,
            item.Id,
            string.IsNullOrWhiteSpace(item.AdvanceSellOrderId) ? item.SellOrderId : item.AdvanceSellOrderId,
            null,
            null,
            "超额收款转预收",
            actingUserId,
            receipt);

        item.AdvancePoolAmount += amount;
        item.ModifyTime = DateTime.UtcNow;
        await _receiptItemRepo.UpdateAsync(item);
    }

    /// <inheritdoc />
    public async Task ApplyFromPoolAsync(
        string customerId,
        short currency,
        decimal amount,
        FinanceReceivable receivable,
        string writeOffId,
        string? advanceSellOrderId,
        string? actingUserId = null,
        string? remark = null)
    {
        if (amount <= 0m)
            throw new ArgumentException("核销金额必须大于 0", nameof(amount));

        var advance = await GetOrCreateAdvanceAsync(
            customerId,
            receivable.CustomerName,
            currency,
            receivable.SalesUserId,
            actingUserId);
        if (advance.Balance + 0.0001m < amount)
            throw new InvalidOperationException($"客户预收余额不足（可用 {advance.Balance}，本次 {amount}）");

        advance.Balance -= amount;
        advance.TotalApplied += amount;
        advance.ModifyTime = DateTime.UtcNow;
        await _advanceRepo.UpdateAsync(advance);

        var ledgerId = Guid.NewGuid().ToString();
        await _ledgerRepo.AddAsync(new FinanceCustomerAdvanceLedger
        {
            Id = ledgerId,
            FinanceCustomerAdvanceId = advance.Id,
            CustomerId = customerId.Trim(),
            Currency = currency,
            LedgerType = FinanceCustomerAdvanceLedgerTypeCode.Apply,
            Amount = amount,
            FinanceReceivableId = receivable.Id,
            FinanceReceivableWriteOffId = writeOffId,
            SellOrderId = advanceSellOrderId,
            Remark = remark,
            OperatorUserId = ActingUserIdNormalizer.Normalize(actingUserId),
            CreateTime = DateTime.UtcNow
        });
    }

    private async Task CreditCoreAsync(
        string customerId,
        string? customerName,
        byte receiptCurrency,
        decimal amount,
        short ledgerType,
        string? receiptId,
        string? receiptItemId,
        string? sellOrderId,
        string? receivableId,
        string? writeOffId,
        string? remark,
        string? actingUserId,
        FinanceReceipt? receipt = null)
    {
        if (amount <= 0m)
            return;

        var currency = (short)receiptCurrency;
        var advance = await GetOrCreateAdvanceAsync(
            customerId,
            customerName,
            currency,
            receipt?.SalesUserId,
            actingUserId);
        advance.Balance += amount;
        advance.TotalIn += amount;
        advance.ModifyTime = DateTime.UtcNow;
        await _advanceRepo.UpdateAsync(advance);

        await _ledgerRepo.AddAsync(new FinanceCustomerAdvanceLedger
        {
            Id = Guid.NewGuid().ToString(),
            FinanceCustomerAdvanceId = advance.Id,
            CustomerId = customerId.Trim(),
            Currency = currency,
            LedgerType = ledgerType,
            Amount = amount,
            FinanceReceiptId = receiptId,
            FinanceReceiptItemId = receiptItemId,
            FinanceReceivableId = receivableId,
            FinanceReceivableWriteOffId = writeOffId,
            SellOrderId = string.IsNullOrWhiteSpace(sellOrderId) ? null : sellOrderId.Trim(),
            Remark = remark,
            OperatorUserId = ActingUserIdNormalizer.Normalize(actingUserId),
            CreateTime = DateTime.UtcNow
        });
    }

    private async Task<FinanceCustomerAdvance> GetOrCreateAdvanceAsync(
        string customerId,
        string? customerName,
        short currency,
        string? salesUserId = null,
        string? actingUserId = null)
    {
        var cid = customerId.Trim();
        var existing = (await _advanceRepo.FindAsync(a =>
            !a.IsDeleted && a.CustomerId == cid && a.Currency == currency)).FirstOrDefault();
        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(existing.SalesUserId) && !string.IsNullOrWhiteSpace(salesUserId))
            {
                existing.SalesUserId = salesUserId.Trim();
                existing.ModifyTime = DateTime.UtcNow;
                await _advanceRepo.UpdateAsync(existing);
            }
            return existing;
        }

        var row = new FinanceCustomerAdvance
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = cid,
            CustomerName = customerName,
            Currency = currency,
            SalesUserId = string.IsNullOrWhiteSpace(salesUserId) ? null : salesUserId.Trim(),
            Balance = 0m,
            TotalIn = 0m,
            TotalApplied = 0m,
            TotalRefund = 0m,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
        };
        await _advanceRepo.AddAsync(row);
        return row;
    }
}
