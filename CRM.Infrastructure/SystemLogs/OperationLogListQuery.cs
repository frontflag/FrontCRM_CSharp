using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
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

        // SuperAdmin 敏感日志默认对系统操作日志隐藏（含 SA 本人）；仅 AllowSuperAdminBizType 时可见
        if (!query.AllowSuperAdminBizType)
        {
            q = q.Where(o => o.BizType != SuperAdminOperationLogCodes.BizType);
        }

        if (!string.IsNullOrWhiteSpace(query.BizType))
        {
            var b = query.BizType.Trim();
            if (!query.AllowSuperAdminBizType
                && string.Equals(b, SuperAdminOperationLogCodes.BizType, StringComparison.OrdinalIgnoreCase))
            {
                return new OperationLogPagedResult
                {
                    Total = 0,
                    Page = page,
                    PageSize = pageSize,
                    Items = Array.Empty<OperationLogListItemDto>()
                };
            }
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

        if (!string.IsNullOrWhiteSpace(query.RecordId))
        {
            var rid = query.RecordId.Trim();
            q = q.Where(o => o.RecordId == rid);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionTypePrefix))
        {
            var prefix = query.ActionTypePrefix.Trim();
            q = q.Where(o => o.ActionType.StartsWith(prefix));
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

        if (query.ExcludeExportLogs)
        {
            q = q.Where(o => o.ExtraInfo == null || !o.ExtraInfo.Contains("\"exportKind\""));
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
            OperationDesc = o.OperationDesc,
            ExtraInfo = o.ExtraInfo
        }).ToList();

        return new OperationLogPagedResult
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    /// <inheritdoc />
    public async Task<ExportLogPagedResult> QueryExportLogsAsync(ExportLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var q = _db.OperationLogs.AsNoTracking()
            .Where(o => o.ExtraInfo != null && o.ExtraInfo.Contains("\"exportKind\""));

        if (!string.IsNullOrWhiteSpace(query.ExportKind))
        {
            var kind = query.ExportKind.Trim();
            var needle = "\"exportKind\":\"" + kind + "\"";
            q = q.Where(o => o.ExtraInfo != null && o.ExtraInfo.Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(query.OperatorUserName))
        {
            var p = query.OperatorUserName.Trim().ToLowerInvariant();
            q = q.Where(o => o.OperatorUserName != null && o.OperatorUserName.ToLower().Contains(p));
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

        var items = rows.Select(o =>
        {
            var d = ExportKindCatalog.Hydrate(o.ExtraInfo);
            return new ExportLogListItemDto
            {
                Id = o.Id,
                OperationTime = o.OperationTime,
                OperatorUserName = o.OperatorUserName,
                ExportKind = d.ExportKind,
                ExportKindName = d.BusinessTypeName,
                PageTitle = d.PageTitle,
                PageUrl = d.PageUrl,
                FilterSummary = d.FilterSummary,
                ExportedCount = d.ExportedCount,
                SysRemark = d.SysRemark
            };
        }).ToList();

        return new ExportLogPagedResult
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }
}
