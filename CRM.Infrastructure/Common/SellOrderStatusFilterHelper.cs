using CRM.Core.Models.Sales;

namespace CRM.Infrastructure.Common;

/// <summary>销售订单主状态筛选项规范化：仅保留合法枚举值，去重排序。</summary>
internal static class SellOrderStatusFilterHelper
{
    private static readonly HashSet<short> Allowed = new()
    {
        (short)SellOrderMainStatus.New,
        (short)SellOrderMainStatus.PendingAudit,
        (short)SellOrderMainStatus.Approved,
        (short)SellOrderMainStatus.InProgress,
        (short)SellOrderMainStatus.Completed,
        (short)SellOrderMainStatus.AuditFailed,
        (short)SellOrderMainStatus.Cancelled
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
