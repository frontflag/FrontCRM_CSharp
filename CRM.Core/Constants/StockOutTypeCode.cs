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

    /// <summary>历史 <c>stock_out.StockOutType</c> 销售出库（迁移 <c>20260531120000</c> 前为 1）。</summary>
    public const short LegacySales = 1;

    /// <summary>移库/调拨虚拟出库（非装箱单业务枚举；列表与统计需排除）。</summary>
    public const short Transfer = 3;

    public static bool IsDefined(short value) =>
        value is Sales or Customs or Return or Scrap or Transfer or LegacySales;

    /// <summary>是否为销售出库（含历史类型 1）。仅用于内存判断；EF/FindAsync 谓词请内联比较 <see cref="Sales"/> / <see cref="LegacySales"/>。</summary>
    public static bool IsSalesStockOut(short value) =>
        value is Sales or LegacySales;

    /// <summary>
    /// 出库明细列表筛选：销售含历史 1；其它类型按业务码精确匹配。
    /// 仅用于内存判断；EF 谓词请内联。
    /// </summary>
    public static bool MatchesItemListFilter(short stored, short requested)
    {
        if (IsSalesStockOut(requested) || NormalizeForNotify(requested) == Sales)
            return IsSalesStockOut(stored);
        return stored == NormalizeForNotify(requested);
    }

    /// <summary>装箱/出库通知业务类型（不含移库 <see cref="Transfer"/>）。</summary>
    public static bool IsPackingBusinessType(short value) =>
        value is Sales or Customs or Return or Scrap;

    /// <summary>出库通知 <c>stockout_notify.StockOutType</c>；非法或未传时默认销售出库。</summary>
    public static short NormalizeForNotify(short value) =>
        IsPackingBusinessType(value) ? value : Sales;
}
