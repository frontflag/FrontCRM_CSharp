using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>库存中心在库汇总：原币金额列顺序（RMB → USD → EUR → HKD，其余按枚举值升序补列）。</summary>
public static class InventoryOnHandCurrency
{
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
