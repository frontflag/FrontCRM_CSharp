using CRM.Core.Models.Sales;

namespace CRM.Core.Utilities;

/// <summary>销售看板与列表共用的日期、状态过滤（与 <see cref="Infrastructure.SalesOrders.SalesOrderListQuery"/> 日期上界对齐）。</summary>
public static class SalesAnalyticsDateFilter
{
    /// <summary>日历日边界转 UTC 午夜（Npgsql timestamptz 要求）。</summary>
    public static DateTime ToUtcDateStart(DateTime date) =>
        PostgreSqlDateTime.ToUtc(date.Date);

    /// <summary>含当日在内的区间上界（次日 UTC 零点，用于 &lt; 比较）。</summary>
    public static DateTime ToUtcDateEndExclusive(DateTime dateInclusive) =>
        PostgreSqlDateTime.ToUtc(dateInclusive.Date.AddDays(1));

    public static IQueryable<SellOrder> ApplyCreateTimeRange(
        IQueryable<SellOrder> q,
        DateTime dateFrom,
        DateTime dateToInclusive)
    {
        var from = ToUtcDateStart(dateFrom);
        var endExclusive = ToUtcDateEndExclusive(dateToInclusive);
        return q.Where(o => o.CreateTime >= from && o.CreateTime < endExclusive);
    }

    /// <summary>列表默认包含全部状态；看板排除取消/审核失败。</summary>
    public static IQueryable<SellOrder> ApplyAnalyticsStatusFilter(IQueryable<SellOrder> q) =>
        q.Where(o =>
            o.Status != SellOrderMainStatus.Cancelled
            && o.Status != SellOrderMainStatus.AuditFailed);
}
