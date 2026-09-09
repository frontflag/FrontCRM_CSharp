namespace CRM.Core.Constants;

/// <summary>出库单 <c>stock_out.Status</c>。</summary>
public static class StockOutStatusCode
{
    public const short Draft = 0;
    public const short Pending = 1;
    /// <summary>准备出库（已生成出库单，尚未标记完成）。</summary>
    public const short Ready = 2;
    public const short Cancelled = 3;
    /// <summary>出库完成。</summary>
    public const short Completed = 4;
}
