namespace CRM.Infrastructure.Common;

/// <summary>采购订单主状态筛选项规范化：仅保留合法取值，去重排序。</summary>
internal static class PurchaseOrderStatusFilterHelper
{
    /// <summary>与列表筛选项一致：0/1/2/10/20/30/50/100/-1/-2。</summary>
    private static readonly HashSet<short> Allowed = new()
    {
        0, 1, 2, 10, 20, 30, 50, 100, -1, -2
    };

    public static List<short> Normalize(IEnumerable<short>? values)
    {
        if (values == null) return new List<short>();
        return values
            .Where(Allowed.Contains)
            .Distinct()
            .OrderBy(v => v)
            .ToList();
    }
}
