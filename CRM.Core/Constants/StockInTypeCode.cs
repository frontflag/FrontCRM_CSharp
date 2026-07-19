namespace CRM.Core.Constants;



/// <summary>

/// 入库类型（<c>stock_in.StockInType</c> 共用）。

/// </summary>

public static class StockInTypeCode

{

    public const short Purchase = 10;

    public const short Customs = 20;

    public const short Return = 30;

    public const short Scrap = 40;



    /// <summary>移库/调拨虚拟入库（非购销业务枚举；列表与统计需排除）。</summary>

    public const short Transfer = 3;



    public static bool IsDefined(short value) =>

        value is Purchase or Customs or Return or Scrap or Transfer;



    /// <summary>购销业务入库类型（不含移库 <see cref="Transfer"/>）。</summary>

    public static bool IsBusinessType(short value) =>

        value is Purchase or Customs or Return or Scrap;



    /// <summary>非法或未传时默认采购入库。</summary>

    public static short Normalize(short value) =>

        IsBusinessType(value) ? value : Purchase;

    /// <summary>入库通知 <c>stockin_notify.StockInType</c>；非法或未传时默认采购入库。</summary>
    public static short NormalizeForNotify(short value) => Normalize(value);

    /// <summary>
    /// 已过账入库单类型是否与到货通知类型一致（含历史 <c>stock_in.StockInType</c> 1→10 等；排除移库 3）。
    /// </summary>
    public static bool MatchesNoticeStockInType(short stockInType, short notifyStockInType)
    {
        if (stockInType == Transfer) return false;
        return Normalize(stockInType) == NormalizeForNotify(notifyStockInType);
    }
}

