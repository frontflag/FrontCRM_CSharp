namespace CRM.Core.Constants;

/// <summary>货代付款台账状态（计算字段，不落库）。</summary>
public static class FinanceFreightForwarderPayableStatusCodes
{
    public const short Pending = 10;
    public const short Partial = 20;
    public const short Completed = 30;
}
