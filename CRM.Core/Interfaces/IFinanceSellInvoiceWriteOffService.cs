using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public interface IFinanceSellInvoiceWriteOffService
{
    Task<IReadOnlyList<FinanceSellInvoiceWriteOffCustomerSummary>> GetCustomerSummariesAsync(
        string? keyword, string? currentUserId, CancellationToken cancellationToken = default);

    Task<FinanceSellInvoiceWriteOffCandidates> GetCandidatesAsync(
        string customerId, byte currency, string? currentUserId, CancellationToken cancellationToken = default);

    Task<FinanceSellInvoiceWriteOffResult> ApplyAsync(
        FinanceSellInvoiceWriteOffRequest request, string? actingUserId, CancellationToken cancellationToken = default);

    /// <summary>按匹配流水与应收 verified_* 重算销项发票 Receive*（派生缓存）。</summary>
    Task RecalculateInvoiceReceiveProgressAsync(string financeSellInvoiceId, CancellationToken cancellationToken = default);

    /// <summary>收款核销后：找出匹配到这些应收的销项发票并重算 Receive*。</summary>
    Task RecalculateReceiveProgressForReceivablesAsync(
        IEnumerable<string> financeReceivableIds, CancellationToken cancellationToken = default);
}

public class FinanceSellInvoiceWriteOffCustomerSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public byte Currency { get; set; }
    public decimal PendingWriteOffTotal { get; set; }
    public int PendingInvoiceCount { get; set; }
    public DateTime? EarliestInvoiceDate { get; set; }
    public DateTime? LatestInvoiceDate { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
    public bool HasOpenReceivable { get; set; }
}

public class FinanceSellInvoiceWriteOffCandidates
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public byte Currency { get; set; }
    public List<FinanceSellInvoiceWriteOffInvoiceRow> Invoices { get; set; } = new();
    public List<FinanceSellInvoiceWriteOffReceivableRow> Receivables { get; set; } = new();
}

public class FinanceSellInvoiceWriteOffInvoiceRow
{
    public string Id { get; set; } = string.Empty;
    public string? InvoiceCode { get; set; }
    public string? InvoiceNo { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal MatchDone { get; set; }
    public decimal MatchToBe { get; set; }
    public short MatchStatus { get; set; }
    public byte Currency { get; set; }
    public short Type { get; set; }
    public short InvoiceStatus { get; set; }
    public short SellInvoiceType { get; set; }
}

public class FinanceSellInvoiceWriteOffReceivableRow
{
    public string FinanceReceivableId { get; set; } = string.Empty;
    public string? ReceivableCode { get; set; }
    public string? StockOutId { get; set; }
    public string? StockOutCode { get; set; }
    public DateTime? StockOutDate { get; set; }
    public string? SellOrderCode { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
    public string? FreightForwarderOrderNo { get; set; }
    public string? StockInCode { get; set; }
    public decimal Amount { get; set; }
    public decimal InvoiceMatchDone { get; set; }
    public decimal InvoiceMatchToBe { get; set; }
    public short InvoiceMatchStatus { get; set; }
    public byte Currency { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public int? StockOutTotalQuantity { get; set; }
    public decimal? StockOutTotalAmount { get; set; }
}

public class FinanceSellInvoiceWriteOffRequest
{
    public string FinanceSellInvoiceId { get; set; } = string.Empty;
    public List<FinanceSellInvoiceWriteOffAllocation> Allocations { get; set; } = new();
}

public class FinanceSellInvoiceWriteOffAllocation
{
    public string FinanceReceivableId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class FinanceSellInvoiceWriteOffResult
{
    public string FinanceSellInvoiceId { get; set; } = string.Empty;
    public decimal AppliedTotal { get; set; }
    public int AllocationCount { get; set; }
}
