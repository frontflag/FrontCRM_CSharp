using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

public interface IForceDeleteGuardService
{
    Task<ForceDeleteGuardResult> CanForceDeleteFinancePaymentAsync(string financePaymentId);
    Task<ForceDeleteGuardResult> CanForceDeleteFinanceReceiptAsync(string financeReceiptId);
    Task<ForceDeleteGuardResult> CanForceDeleteFinancePurchaseInvoiceAsync(string financePurchaseInvoiceId);
    Task<ForceDeleteGuardResult> CanForceDeleteFinanceSellInvoiceAsync(string financeSellInvoiceId);
    Task<ForceDeleteGuardResult> CanForceDeleteStockOutRequestAsync(string stockOutRequestId);
    Task<ForceDeleteGuardResult> CanForceDeleteStockOutAsync(string stockOutId);
}

public sealed class ForceDeleteGuardResult
{
    private ForceDeleteGuardResult(bool canDelete, IReadOnlyList<string> reasons)
    {
        CanDelete = canDelete;
        Reasons = reasons;
    }

    public bool CanDelete { get; }
    public IReadOnlyList<string> Reasons { get; }
    public string Message => Reasons.Count == 0 ? "OK" : string.Join("；", Reasons);

    public static ForceDeleteGuardResult Allow() => new(true, Array.Empty<string>());
    public static ForceDeleteGuardResult Deny(params string[] reasons) => new(false, reasons.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray());
    public static ForceDeleteGuardResult Deny(IEnumerable<string> reasons) => new(false, reasons.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray());
}
