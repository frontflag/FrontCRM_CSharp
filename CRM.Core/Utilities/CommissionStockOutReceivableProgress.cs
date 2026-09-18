using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public readonly record struct CommissionStockOutReceivableFact(
    short VerificationStatus,
    decimal Amount,
    IReadOnlyList<CommissionWriteOffStep> Steps);

/// <summary>
/// 提成入期看该出库行对应应收（出库单 × 销售明细），不按销售明细全额收款进度。
/// 同一出库+同一销售明细共享一条应收，不做拣货行拆款。
/// </summary>
public static class CommissionStockOutReceivableProgress
{
    public static (short Status, DateOnly? ReceiptDate) Resolve(
        IReadOnlyList<CommissionStockOutReceivableFact>? receivables,
        DateOnly? stockOutDate,
        decimal lineReceivableAmount)
    {
        if (receivables == null || receivables.Count == 0)
        {
            if (lineReceivableAmount <= 0m)
                return (FinanceVerificationStatusCode.Complete, stockOutDate);
            return (FinanceVerificationStatusCode.Pending, null);
        }

        var status = CombineStatus(receivables);
        if (status != FinanceVerificationStatusCode.Complete)
            return (status, null);

        DateOnly? date = null;
        foreach (var recv in receivables)
        {
            var stepDate = ResolveCompleteDate(recv, stockOutDate);
            if (!stepDate.HasValue)
                return (status, null);
            date = date.HasValue && date.Value > stepDate.Value ? date : stepDate;
        }

        return (status, date);
    }

    static DateOnly? ResolveCompleteDate(CommissionStockOutReceivableFact recv, DateOnly? stockOutDate)
    {
        var steps = recv.Steps ?? [];
        if (recv.Amount <= 0m && steps.Count == 0)
            return stockOutDate;

        return CommissionReceiptDateReplay.Resolve(recv.Amount, steps);
    }

    static short CombineStatus(IReadOnlyList<CommissionStockOutReceivableFact> receivables)
    {
        var min = receivables[0].VerificationStatus;
        var max = min;
        for (var i = 1; i < receivables.Count; i++)
        {
            var s = receivables[i].VerificationStatus;
            if (s < min) min = s;
            if (s > max) max = s;
        }

        return FinancePaymentHeaderVerification.Resolve(min, max);
    }
}
