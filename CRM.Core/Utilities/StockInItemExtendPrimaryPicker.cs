using CRM.Core.Models.Inventory;

namespace CRM.Core.Utilities;

/// <summary>按入库单聚合「首行」扩展行（CreateTime、Id 序），用于列表/详情等头表级展示与解析。</summary>
public static class StockInItemExtendPrimaryPicker
{
    public static StockInItemExtend? PickPrimary(IEnumerable<StockInItemExtend>? rows)
    {
        if (rows == null)
            return null;
        var list = rows as IList<StockInItemExtend> ?? rows.ToList();
        if (list.Count == 0)
            return null;
        return list.OrderBy(x => x.CreateTime).ThenBy(x => x.Id, StringComparer.Ordinal).First();
    }

    public static Dictionary<string, StockInItemExtend?> PrimaryByStockInId(IEnumerable<StockInItemExtend> all)
    {
        var d = new Dictionary<string, StockInItemExtend?>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in all.GroupBy(e => (e.StockInId ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(g.Key))
                continue;
            d[g.Key] = PickPrimary(g);
        }

        return d;
    }
}
