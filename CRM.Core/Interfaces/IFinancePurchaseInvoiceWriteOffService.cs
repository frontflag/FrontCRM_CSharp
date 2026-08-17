using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public interface IFinancePurchaseInvoiceWriteOffService
{
    Task<IReadOnlyList<FinancePurchaseInvoiceWriteOffVendorSummary>> GetVendorSummariesAsync(
        string? keyword, string? currentUserId, CancellationToken cancellationToken = default);

    Task<FinancePurchaseInvoiceWriteOffCandidates> GetCandidatesAsync(
        string vendorId, byte currency, string? currentUserId, CancellationToken cancellationToken = default);

    Task<FinancePurchaseInvoiceWriteOffResult> ApplyAsync(
        FinancePurchaseInvoiceWriteOffRequest request, string? actingUserId, CancellationToken cancellationToken = default);

    /// <summary>整票反核销：软删该发票全部有效核销流水，并重算发票/入库匹配与付款缓存。</summary>
    Task<FinancePurchaseInvoiceWriteOffReverseResult> ReverseByInvoiceAsync(
        string invoiceId, string? actingUserId, CancellationToken cancellationToken = default);
}

public class FinancePurchaseInvoiceWriteOffVendorSummary
{
    public string VendorId { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public string? VendorEnglishName { get; set; }
    public byte Currency { get; set; }
    public decimal PendingWriteOffTotal { get; set; }
    public int PendingInvoiceCount { get; set; }
    public DateTime? EarliestInvoiceDate { get; set; }
    public DateTime? LatestInvoiceDate { get; set; }
    public bool HasOpenStockIn { get; set; }
}

public class FinancePurchaseInvoiceWriteOffCandidates
{
    public string VendorId { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public string? VendorEnglishName { get; set; }
    public byte Currency { get; set; }
    public List<FinancePurchaseInvoiceWriteOffInvoiceRow> Invoices { get; set; } = new();
    public List<FinancePurchaseInvoiceWriteOffStockInRow> StockIns { get; set; } = new();
}

public class FinancePurchaseInvoiceWriteOffInvoiceRow
{
    public string Id { get; set; } = string.Empty;
    public string? InvoiceCode { get; set; }
    public string? InvoiceNo { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public decimal InvoiceAmount { get; set; }
    public decimal VerifiedDone { get; set; }
    public decimal VerifiedToBe { get; set; }
    public short VerificationStatus { get; set; }
    public byte Currency { get; set; }
    public byte ConfirmStatus { get; set; }
    public short RedInvoiceStatus { get; set; }
}

public class FinancePurchaseInvoiceWriteOffStockInRow
{
    public string StockInId { get; set; } = string.Empty;
    public string? StockInCode { get; set; }
    public DateTime? StockInDate { get; set; }
    public byte Currency { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal InvoiceMatchDone { get; set; }
    public decimal InvoiceMatchToBe { get; set; }
    public short InvoiceMatchStatus { get; set; }
    public int TotalQuantity { get; set; }
    public string? FreightForwarderOrderNo { get; set; }
    public string? PurchaseOrderCodes { get; set; }
    public string? PurchaseUserId { get; set; }
    public string? PurchaseUserName { get; set; }
    public string? VendorName { get; set; }
    public string? VendorEnglishName { get; set; }
    public List<FinancePurchaseInvoiceWriteOffStockInItemRow> Items { get; set; } = new();
}

public class FinancePurchaseInvoiceWriteOffStockInItemRow
{
    public string StockInItemId { get; set; } = string.Empty;
    public string? StockInItemCode { get; set; }
    public decimal Amount { get; set; }
    public decimal InvoiceMatchDone { get; set; }
    public decimal InvoiceMatchToBe { get; set; }
    public short InvoiceMatchStatus { get; set; }
    public byte? Currency { get; set; }
    public string? PurchaseOrderItemId { get; set; }
    public string? PurchaseOrderItemCode { get; set; }
    public string? PurchaseOrderCode { get; set; }
    public string? PurchaseUserId { get; set; }
    public string? PurchaseUserName { get; set; }
    public string? FreightForwarderOrderNo { get; set; }
}

public class FinancePurchaseInvoiceWriteOffRequest
{
    public string FinancePurchaseInvoiceId { get; set; } = string.Empty;
    public List<FinancePurchaseInvoiceWriteOffAllocation> Allocations { get; set; } = new();
}

public class FinancePurchaseInvoiceWriteOffAllocation
{
    public string StockInItemId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class FinancePurchaseInvoiceWriteOffResult
{
    public string FinancePurchaseInvoiceId { get; set; } = string.Empty;
    public decimal AppliedTotal { get; set; }
    public int AllocationCount { get; set; }
}

public class FinancePurchaseInvoiceWriteOffReverseResult
{
    public string FinancePurchaseInvoiceId { get; set; } = string.Empty;
    public int WriteOffCount { get; set; }
    public decimal ReversedTotal { get; set; }
    public List<string> StockInCodes { get; set; } = new();
}
