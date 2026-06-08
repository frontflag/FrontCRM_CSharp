using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>货代单号展示拼接（出库/拣货等多 PO 场景）。</summary>
public static class FreightForwarderOrderNoDisplay
{
    public static string JoinDistinct(IEnumerable<string?> values)
    {
        var list = values
            .Select(PurchaseOrderFreightForwarderOrderNoRules.Normalize)
            .Where(v => v != null)
            .Select(v => v!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return list.Count == 0 ? string.Empty : string.Join(", ", list);
    }
}
