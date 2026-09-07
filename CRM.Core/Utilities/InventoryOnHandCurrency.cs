using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>库存中心在库汇总：原币金额列顺序（RMB → USD → EUR → HKD，其余按枚举值升序补列）。</summary>
public static class InventoryOnHandCurrency
{
    /// <summary>分布/排行合成币种：各原币折成美元后再加总。非数字，避免被当成原币过滤。</summary>
    public const string ConvertedUsdKey = "usdConverted";

    /// <summary>后端默认标签；前端用 i18n 覆盖。</summary>
    public const string ConvertedUsdLabel = "折算USD";

    public static bool IsConvertedUsdKey(string? key) =>
        string.Equals(key, ConvertedUsdKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>折算美金单价：PO 行 convert_price &gt; 0 优先，否则入库快照 PurchasePriceUsd。不用查询日财务汇率。</summary>
    public static decimal UnitUsd(decimal convertPrice, decimal purchasePriceUsd) =>
        convertPrice > 0m ? convertPrice : purchasePriceUsd;

    public static short Normalize(short raw) =>
        raw is >= (short)CurrencyCode.RMB and <= (short)CurrencyCode.GBP
            ? raw
            : (short)CurrencyCode.RMB;

    public static IReadOnlyList<short> OrderPresent(IEnumerable<short> currencies)
    {
        var set = new HashSet<short>();
        foreach (var c in currencies)
            set.Add(Normalize(c));

        return set
            .OrderBy(c => c)
            .ToList();
    }
}
