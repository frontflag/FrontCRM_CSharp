using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public class FinanceReceivableWriteOffAllocation
{
    public string FinanceReceiptItemId { get; set; } = string.Empty;
    public string FinanceReceivableId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class FinanceReceivableWriteOffRequest
{
    public List<FinanceReceivableWriteOffAllocation> Allocations { get; set; } = new();
    public List<FinanceAdvancePoolAllocation> AdvancePoolAllocations { get; set; } = new();
    public bool ConfirmSoMismatch { get; set; }
}

public class FinanceReceiptItemWriteOffCandidate
{
    public FinanceReceiptItem Item { get; set; } = null!;
    public string FinanceReceiptCode { get; set; } = string.Empty;
    public short ReceiptStatus { get; set; }
    public decimal RemainingAmount { get; set; }
    public short ReceiptPurpose { get; set; }
    public string? AdvanceSellOrderId { get; set; }
}

public class FinanceReceivableWriteOffCandidates
{
    public List<FinanceReceiptItemWriteOffCandidate> ReceiptItems { get; set; } = new();
    public List<FinanceReceivable> Receivables { get; set; } = new();
    public List<FinanceCustomerAdvanceBalanceDto> AdvanceBalances { get; set; } = new();
}

public interface IFinanceReceivableService
{
    Task TryEnsureFromStockOutAsync(string stockOutId, string? actingUserId = null, CancellationToken cancellationToken = default);

    Task TrySoftDeleteForStockOutAsync(string stockOutId, string? actingUserId = null, CancellationToken cancellationToken = default);

    void AssertStockOutCanVoid(FinanceReceivable? receivable);

    Task<PagedResult<FinanceReceivable>> GetPagedAsync(FinanceReceivableQueryRequest request, CancellationToken cancellationToken = default);

    Task<FinanceReceivableWriteOffCandidates> GetWriteOffCandidatesAsync(string customerId, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<FinanceReceivableWriteOffResult> ApplyWriteOffAsync(FinanceReceivableWriteOffRequest request, string? actingUserId = null, CancellationToken cancellationToken = default);
}
