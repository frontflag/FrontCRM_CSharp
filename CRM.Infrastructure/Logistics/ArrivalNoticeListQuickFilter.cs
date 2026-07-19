using CRM.Core.Constants;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Logistics;

internal static class ArrivalNoticeListQuickFilter
{
    private const short StatusNotArrived = 10;
    private const short StatusPendingQc = 20;
    private const short StatusQcDone = 30;
    private const short StatusStockedIn = 100;
    private const short StockInPosted = StockInHeaderStatusCode.Posted;
    private const short PurchaseStockInType = StockInTypeCode.Purchase;

    public static IQueryable<StockInNotify> Apply(ApplicationDbContext db, IQueryable<StockInNotify> q, string preset)
    {
        if (!ArrivalNoticeListQuickFilterCodes.IsKnown(preset))
            return q;

        var today = DateTime.UtcNow.Date;

        return preset.Trim() switch
        {
            ArrivalNoticeListQuickFilterCodes.OverdueAll => ApplyOverdueAll(q, today),
            ArrivalNoticeListQuickFilterCodes.Overdue1Day => ApplyOverdue1Day(q, today),
            ArrivalNoticeListQuickFilterCodes.Overdue3Days => ApplyOverdueRange(q, today.AddDays(-3), today.AddDays(-1)),
            ArrivalNoticeListQuickFilterCodes.Overdue1Week => ApplyOverdueRange(q, today.AddDays(-7), today.AddDays(-1)),

            ArrivalNoticeListQuickFilterCodes.ExpectedToday => WhereExpectedOnDay(q, today),
            ArrivalNoticeListQuickFilterCodes.ExpectedTomorrow => WhereExpectedOnDay(q, today.AddDays(1)),
            ArrivalNoticeListQuickFilterCodes.ExpectedWithin3Days => WhereExpectedInRange(q, today, today.AddDays(3)),
            ArrivalNoticeListQuickFilterCodes.ExpectedWithin7Days => WhereExpectedInRange(q, today, today.AddDays(7)),

            ArrivalNoticeListQuickFilterCodes.NotArrived => q.Where(x => x.Status == StatusNotArrived),

            ArrivalNoticeListQuickFilterCodes.ArrivedToday => WhereActualOnDay(q, today, minStatus: StatusPendingQc),
            ArrivalNoticeListQuickFilterCodes.ArrivedTodayYesterday => WhereActualInRange(
                q, today.AddDays(-1), today, minStatus: StatusPendingQc),
            ArrivalNoticeListQuickFilterCodes.ArrivedWithin3Days => WhereActualInRange(
                q, today.AddDays(-2), today, minStatus: StatusPendingQc),
            ArrivalNoticeListQuickFilterCodes.ArrivedWithin7Days => WhereActualInRange(
                q, today.AddDays(-6), today, minStatus: StatusPendingQc),
            ArrivalNoticeListQuickFilterCodes.ArrivedWithin30Days => WhereActualInRange(
                q, today.AddDays(-29), today, minStatus: StatusPendingQc),

            ArrivalNoticeListQuickFilterCodes.TypePurchase => q.Where(x => x.StockInType == PurchaseStockInType),
            ArrivalNoticeListQuickFilterCodes.TypeCustoms => q.Where(x => x.StockInType == StockInTypeCode.Customs),

            ArrivalNoticeListQuickFilterCodes.TodoPendingQc => q.Where(x => x.Status == StatusPendingQc),
            ArrivalNoticeListQuickFilterCodes.TodoPendingStockIn => ApplyTodoPendingStockIn(db, q),

            ArrivalNoticeListQuickFilterCodes.StatusQcDone => q.Where(x => x.Status == StatusQcDone),
            ArrivalNoticeListQuickFilterCodes.StatusStockedIn => q.Where(x => x.Status == StatusStockedIn),

            _ => q
        };
    }

    private static IQueryable<StockInNotify> ApplyOverdueAll(IQueryable<StockInNotify> q, DateTime today) =>
        q.Where(x => x.Status == StatusNotArrived
                     && x.ExpectedArrivalDate.HasValue
                     && x.ExpectedArrivalDate.Value < today);

    private static IQueryable<StockInNotify> ApplyOverdue1Day(IQueryable<StockInNotify> q, DateTime today)
    {
        var yesterday = today.AddDays(-1);
        var dayEnd = today;
        return q.Where(x => x.Status == StatusNotArrived
                            && x.ExpectedArrivalDate.HasValue
                            && x.ExpectedArrivalDate.Value >= yesterday
                            && x.ExpectedArrivalDate.Value < dayEnd);
    }

    private static IQueryable<StockInNotify> ApplyOverdueRange(
        IQueryable<StockInNotify> q,
        DateTime fromInclusive,
        DateTime toInclusive) =>
        q.Where(x => x.Status == StatusNotArrived
                     && x.ExpectedArrivalDate.HasValue
                     && x.ExpectedArrivalDate.Value >= fromInclusive
                     && x.ExpectedArrivalDate.Value < toInclusive.AddDays(1));

    private static IQueryable<StockInNotify> WhereExpectedOnDay(IQueryable<StockInNotify> q, DateTime day)
    {
        var end = day.AddDays(1);
        return q.Where(x => x.ExpectedArrivalDate.HasValue
                            && x.ExpectedArrivalDate.Value >= day
                            && x.ExpectedArrivalDate.Value < end);
    }

    private static IQueryable<StockInNotify> WhereExpectedInRange(
        IQueryable<StockInNotify> q,
        DateTime fromInclusive,
        DateTime toInclusive)
    {
        var end = toInclusive.AddDays(1);
        return q.Where(x => x.ExpectedArrivalDate.HasValue
                            && x.ExpectedArrivalDate.Value >= fromInclusive
                            && x.ExpectedArrivalDate.Value < end);
    }

    private static IQueryable<StockInNotify> WhereActualOnDay(
        IQueryable<StockInNotify> q,
        DateTime day,
        short minStatus)
    {
        var end = day.AddDays(1);
        return q.Where(x => x.Status >= minStatus
                            && x.ActualArrivalDate.HasValue
                            && x.ActualArrivalDate.Value >= day
                            && x.ActualArrivalDate.Value < end);
    }

    private static IQueryable<StockInNotify> WhereActualInRange(
        IQueryable<StockInNotify> q,
        DateTime fromInclusive,
        DateTime toInclusive,
        short minStatus)
    {
        var end = toInclusive.AddDays(1);
        return q.Where(x => x.Status >= minStatus
                            && x.ActualArrivalDate.HasValue
                            && x.ActualArrivalDate.Value >= fromInclusive
                            && x.ActualArrivalDate.Value < end);
    }

    private static IQueryable<StockInNotify> ApplyTodoPendingStockIn(ApplicationDbContext db, IQueryable<StockInNotify> q) =>
        q.Where(n => n.Status == StatusQcDone && !db.StockIns.Any(si =>
            si.Status == StockInPosted
            && si.StockInType != StockInTypeCode.Transfer
            && (si.StockInType == StockInTypeCode.Purchase
                || si.StockInType == StockInTypeCode.Customs
                || si.StockInType == StockInTypeCode.Return
                || si.StockInType == StockInTypeCode.Scrap
                    ? si.StockInType
                    : PurchaseStockInType)
               == (n.StockInType == StockInTypeCode.Purchase
                   || n.StockInType == StockInTypeCode.Customs
                   || n.StockInType == StockInTypeCode.Return
                   || n.StockInType == StockInTypeCode.Scrap
                       ? n.StockInType
                       : PurchaseStockInType)
            && (
                (si.SourceId != null && si.SourceId == n.Id)
                || db.QCInfos.Any(qc =>
                    qc.StockInNotifyId == n.Id
                    && (
                        (si.QcId != null && si.QcId == qc.Id)
                        || (qc.StockInId != null && qc.StockInId == si.Id)
                    ))
            )));
}
