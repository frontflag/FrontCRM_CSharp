using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SystemLogs;

/// <summary>操作日志列表：<c>log_operation</c> 上 EF <c>CountAsync</c> + <c>Skip</c>/<c>Take</c>。</summary>
public sealed class OperationLogListQuery : IOperationLogQueryService
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public OperationLogListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();

    /// <inheritdoc />
    public async Task<OperationLogPagedResult> QueryAsync(OperationLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var q = _db.OperationLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.BizType))
        {
            var b = query.BizType.Trim();
            q = q.Where(o => o.BizType == b);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            var p = query.ActionType.Trim().ToLowerInvariant();
            q = q.Where(o => o.ActionType.ToLower().Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(query.RecordCode))
        {
            var p = query.RecordCode.Trim().ToLowerInvariant();
            q = q.Where(o => o.RecordCode != null && o.RecordCode.ToLower().Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(query.OperatorUserName))
        {
            var p = query.OperatorUserName.Trim().ToLowerInvariant();
            q = q.Where(o => o.OperatorUserName != null && o.OperatorUserName.ToLower().Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(query.Reason))
        {
            var p = query.Reason.Trim().ToLowerInvariant();
            q = q.Where(o => o.Reason != null && o.Reason.ToLower().Contains(p));
        }

        if (query.OperationTimeFrom is { } from)
        {
            var f = ToUtc(from);
            q = q.Where(o => o.OperationTime >= f);
        }

        if (query.OperationTimeTo is { } to)
        {
            var t = ToUtc(to);
            q = q.Where(o => o.OperationTime <= t);
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(o => o.OperationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(o => new OperationLogListItemDto
        {
            Id = o.Id,
            BizType = o.BizType,
            RecordId = o.RecordId,
            RecordCode = o.RecordCode,
            ActionType = o.ActionType,
            OperationTime = o.OperationTime,
            OperatorUserId = o.OperatorUserId,
            OperatorUserName = o.OperatorUserName,
            Reason = o.Reason,
            OperationDesc = o.OperationDesc
        }).ToList();

        return new OperationLogPagedResult
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }
}
