using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public class FinanceCustomerAdvanceQueryRequest
{
    public string? Keyword { get; set; }
    public string? CustomerId { get; set; }
    public short? Currency { get; set; }
    public bool? OnlyPositiveBalance { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? CurrentUserId { get; set; }
}

public class FinanceCustomerAdvanceLedgerQueryRequest
{
    public string? CustomerId { get; set; }
    public short? Currency { get; set; }
    public short? LedgerType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? CurrentUserId { get; set; }
}

public class FinanceCustomerAdvanceBalanceDto
{
    public string CustomerId { get; set; } = string.Empty;
    public short Currency { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalApplied { get; set; }
}

public class CreditReceiptItemRemainderToPoolResult
{
    public decimal CreditedAmount { get; set; }
    public decimal RemainingAfter { get; set; }
}

public class FinanceReceivableWriteOffSoMismatch
{
    public string FinanceReceivableId { get; set; } = string.Empty;
    public string? ReceivableSellOrderId { get; set; }
    public string? AdvanceSellOrderId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IFinanceCustomerAdvanceListQuery
{
    Task<PagedResult<FinanceCustomerAdvance>> GetPagedAsync(
        FinanceCustomerAdvanceQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceCustomerAdvanceLedger>> GetLedgerPagedAsync(
        FinanceCustomerAdvanceLedgerQueryRequest request,
        CancellationToken cancellationToken = default);
}

public interface IFinanceCustomerAdvanceService
{
    Task<FinanceCustomerAdvanceBalanceDto?> GetBalanceAsync(
        string customerId,
        short currency,
        CancellationToken cancellationToken = default);

    Task CreditFromReceiptItemAsync(
        FinanceReceipt receipt,
        FinanceReceiptItem item,
        short ledgerType,
        string? actingUserId = null,
        string? remark = null);

    Task ApplyFromPoolAsync(
        string customerId,
        short currency,
        decimal amount,
        FinanceReceivable receivable,
        string writeOffId,
        string? advanceSellOrderId,
        string? actingUserId = null,
        string? remark = null);

    Task TryCreditExplicitAdvanceOnReceiptApprovedAsync(string receiptId, string? actingUserId = null);

    Task CreditAutoInExcessAsync(
        FinanceReceipt receipt,
        FinanceReceiptItem item,
        decimal amount,
        string? actingUserId = null);

    /// <summary>将收款明细可核销余额（全部或指定金额）手动转入客户预收池。</summary>
    Task<CreditReceiptItemRemainderToPoolResult> CreditReceiptItemRemainderToAdvancePoolAsync(
        string receiptItemId,
        decimal? amount = null,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceCustomerAdvanceBalanceDto>> GetBalancesForCustomerAsync(string customerId);

    Task<PagedResult<FinanceCustomerAdvance>> GetPagedAsync(
        FinanceCustomerAdvanceQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceCustomerAdvanceLedger>> GetLedgerPagedAsync(
        FinanceCustomerAdvanceLedgerQueryRequest request,
        CancellationToken cancellationToken = default);

    bool IsReceiptItemAlreadyCreditedToPool(FinanceReceiptItem item);
}
