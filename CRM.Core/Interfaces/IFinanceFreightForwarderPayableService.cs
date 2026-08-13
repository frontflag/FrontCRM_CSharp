using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public interface IFinanceFreightForwarderPayableService
{
    Task<PagedResult<FinanceFreightForwarderPayableListItem>> GetPagedAsync(
        FinanceFreightForwarderPayableQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<FinanceFreightForwarderPayableDetail?> GetDetailAsync(string receiptId, CancellationToken cancellationToken = default);

    Task<FinanceFreightForwarderPayment> CreatePaymentAsync(
        string receiptId, CreateFinanceFreightForwarderPaymentRequest request, string? actingUserId);

    Task<FinanceReceipt> UpdateReceiptFfCompanyAsync(
        string receiptId, string freightForwarderCompanyId, string? actingUserId);
}

public class FinanceFreightForwarderPayableListItem
{
    public string ReceiptId { get; set; } = string.Empty;
    public string FinanceReceiptCode { get; set; } = string.Empty;
    public short ReceiptStatus { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public string? FreightForwarderCompanyId { get; set; }
    public string? FreightForwarderCompanyName { get; set; }
    public decimal ReceiptAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public byte ReceiptCurrency { get; set; }
    public short PayableStatus { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public DateTime CreateTime { get; set; }
}

public class FinanceFreightForwarderPayableDetail
{
    public FinanceReceipt Receipt { get; set; } = null!;
    public string? FreightForwarderCompanyName { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public short PayableStatus { get; set; }
    public IReadOnlyList<FinanceFreightForwarderPayment> Payments { get; set; } = Array.Empty<FinanceFreightForwarderPayment>();
}

public class FinanceFreightForwarderPayableQueryRequest
{
    public string? Keyword { get; set; }
    public string? CustomerId { get; set; }
    public string? FreightForwarderCompanyId { get; set; }
    public short? PayableStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? CurrentUserId { get; set; }
}

public class CreateFinanceFreightForwarderPaymentRequest
{
    public string? FreightForwarderCompanyId { get; set; }
    public decimal PaymentAmount { get; set; }
    public byte PaymentCurrency { get; set; } = 1;
    public short PaymentMode { get; set; } = 1;
    public string? CompanyBankId { get; set; }
    public string? FfCompanyBankId { get; set; }
    public string? BankSlipNo { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Remark { get; set; }
}

public interface IFinanceFreightForwarderPayableListQuery
{
    Task<PagedResult<FinanceFreightForwarderPayableListItem>> GetPagedAsync(
        FinanceFreightForwarderPayableQueryRequest request,
        CancellationToken cancellationToken = default);
}
