using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.WorkCalendar;

public sealed class WorkCalendarStampWriter : IWorkCalendarStampWriter
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<WorkCalendarStampWriter> _logger;

    public WorkCalendarStampWriter(ApplicationDbContext db, ILogger<WorkCalendarStampWriter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task TrySetRfqAssignedAtAsync(
        string rfqId,
        DateTime utc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rfqId))
            return;
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE public.rfq SET assigned_at = {utc} WHERE rfq_id = {rfqId} AND assigned_at IS NULL""",
                cancellationToken);
        }
        catch (Exception ex) when (WorkCalendarPostgres.IsMissingRelationOrColumn(ex))
        {
            _logger.LogWarning(ex, "未写入 rfq.assigned_at（列尚未就绪） RfqId={RfqId}", rfqId);
        }
    }

    public async Task TrySetSellOrderApprovedAtAsync(
        string sellOrderId,
        DateTime utc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellOrderId))
            return;
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE public.sellorder SET approved_at = {utc} WHERE "SellOrderId" = {sellOrderId} AND approved_at IS NULL""",
                cancellationToken);
        }
        catch (Exception ex) when (WorkCalendarPostgres.IsMissingRelationOrColumn(ex))
        {
            _logger.LogWarning(ex, "未写入 sellorder.approved_at（列尚未就绪） SellOrderId={SellOrderId}", sellOrderId);
        }
    }
}
