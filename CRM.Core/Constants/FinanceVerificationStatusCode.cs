namespace CRM.Core.Constants;

/// <summary>财务核销状态（应收/收款明细等共用）：0=未核销 1=部分核销 2=核销完成。</summary>
public static class FinanceVerificationStatusCode
{
    public const short Pending = 0;
    public const short Partial = 1;
    public const short Complete = 2;

    public static short Resolve(decimal total, decimal done)
    {
        if (done <= 0m)
            return Pending;
        if (total > 0m && done + 0.0001m >= total)
            return Complete;
        return Partial;
    }
}
