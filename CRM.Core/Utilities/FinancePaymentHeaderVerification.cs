using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>付款单整单核销状态：无明细由调用方视为未核销；有明细时看 min/max。</summary>
public static class FinancePaymentHeaderVerification
{
    public static short Resolve(short minStatus, short maxStatus)
    {
        if (minStatus == maxStatus
            && (minStatus == FinanceVerificationStatusCode.Pending
                || minStatus == FinanceVerificationStatusCode.Complete))
            return minStatus;
        return FinanceVerificationStatusCode.Partial;
    }
}
