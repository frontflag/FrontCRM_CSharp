using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public static class FinanceFreightForwarderPayableStatusHelper
{
    public static short Compute(decimal receiptAmount, decimal paidAmount)
    {
        if (paidAmount <= 0m)
            return FinanceFreightForwarderPayableStatusCodes.Pending;
        if (paidAmount >= receiptAmount)
            return FinanceFreightForwarderPayableStatusCodes.Completed;
        return FinanceFreightForwarderPayableStatusCodes.Partial;
    }

    public static decimal PendingAmount(decimal receiptAmount, decimal paidAmount) =>
        Math.Max(0m, receiptAmount - paidAmount);
}
