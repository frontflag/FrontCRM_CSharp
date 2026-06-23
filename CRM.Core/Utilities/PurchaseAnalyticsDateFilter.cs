using CRM.Core.Models.Purchase;

namespace CRM.Core.Utilities;

/// <summary>采购看板与列表共用的日期、状态过滤。</summary>
public static class PurchaseAnalyticsDateFilter
{
    private const short StatusAuditFailed = -1;
    private const short StatusCancelled = -2;

    public static DateTime ToUtcDateStart(DateTime date) =>
        SalesAnalyticsDateFilter.ToUtcDateStart(date);

    public static DateTime ToUtcDateEndExclusive(DateTime dateInclusive) =>
        SalesAnalyticsDateFilter.ToUtcDateEndExclusive(dateInclusive);

    public static IQueryable<PurchaseOrder> ApplyAnalyticsStatusFilter(IQueryable<PurchaseOrder> q) =>
        q.Where(o => o.Status != StatusCancelled && o.Status != StatusAuditFailed);
}
