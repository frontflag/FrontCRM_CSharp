namespace CRM.Core.Constants;

/// <summary>
/// 出库类型（<c>stock_out.StockOutType</c>、<c>packing.StockOutType</c> 共用）。
/// </summary>
public static class StockOutTypeCode
{
    public const short Sales = 10;
    public const short Customs = 20;
    public const short Return = 30;
    public const short Scrap = 40;

    /// <summary>移库/调拨虚拟出库（非装箱单业务枚举；列表与统计需排除）。</summary>
    public const short Transfer = 3;

    public static bool IsDefined(short value) =>
        value is Sales or Customs or Return or Scrap or Transfer;

    /// <summary>装箱/出库通知业务类型（不含移库 <see cref="Transfer"/>）。</summary>
    public static bool IsPackingBusinessType(short value) =>
        value is Sales or Customs or Return or Scrap;

    /// <summary>出库通知 <c>stockout_notify.StockOutType</c>；非法或未传时默认销售出库。</summary>
    public static short NormalizeForNotify(short value) =>
        IsPackingBusinessType(value) ? value : Sales;
}
