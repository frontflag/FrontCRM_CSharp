namespace CRM.Core.Constants;

/// <summary>入库单头表 <c>stock_in.Status</c>。</summary>
public static class StockInHeaderStatusCode
{
    public const short Draft = 0;
    public const short Pending = 1;
    /// <summary>已入库 / 已过账。</summary>
    public const short Posted = 2;
    public const short Cancelled = 3;
}
