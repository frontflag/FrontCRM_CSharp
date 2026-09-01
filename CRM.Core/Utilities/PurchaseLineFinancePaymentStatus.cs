using CRM.Core.Models.Finance;

namespace CRM.Core.Utilities;

/// <summary>
/// 采购明细财务付款状态：已核销合计 vs 行采购总额（qty×cost）。
/// 0 未付款 / 1 部分付款 / 2 付款完成。
/// </summary>
public static class PurchaseLineFinancePaymentStatus
{
    public const short Unpaid = 0;
    public const short Partial = 1;
    public const short Complete = 2;

    private const short PaymentCancelled = -2;
    private const short PaymentAuditFailed = -1;

    public static short FromPaidVersusLineTotal(decimal paidAmount, decimal lineTotal)
    {
        var paid = Math.Round(paidAmount, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(lineTotal, 2, MidpointRounding.AwayFromZero);
        if (paid <= 0m) return Unpaid;
        if (paid + 0.0001m >= total) return Complete;
        return Partial;
    }

    public static decimal SumVerificationDoneOnValidPayments(
        IEnumerable<FinancePaymentItem> payItems,
        IEnumerable<FinancePayment> payments)
    {
        var validIds = payments
            .Where(p => p.Status != PaymentCancelled && p.Status != PaymentAuditFailed)
            .Select(p => p.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return payItems
            .Where(pi =>
                !string.IsNullOrWhiteSpace(pi.FinancePaymentId)
                && validIds.Contains(pi.FinancePaymentId))
            .Sum(pi => pi.VerificationDone);
    }

    public static short HeaderFromLineStatuses(IEnumerable<short> lineStatuses)
    {
        var list = lineStatuses as IReadOnlyList<short> ?? lineStatuses.ToList();
        if (list.Count == 0) return Unpaid;
        if (list.All(x => x == Complete)) return Complete;
        if (list.Any(x => x > Unpaid)) return Partial;
        return Unpaid;
    }
}
