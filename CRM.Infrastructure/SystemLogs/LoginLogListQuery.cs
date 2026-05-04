using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SystemLogs;

/// <summary>登录日志列表：<c>log_login</c> 上 EF <c>CountAsync</c> + <c>Skip</c>/<c>Take</c>。</summary>
public sealed class LoginLogListQuery : ILoginLogQueryService
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;

    public LoginLogListQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();

    /// <inheritdoc />
    public async Task<LoginLogPagedResult> QueryAsync(LoginLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var q = _db.LoginLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            var uid = query.UserId.Trim();
            q = q.Where(l => l.UserId == uid);
        }

        if (query.LoginAtFrom is { } from)
        {
            var f = ToUtc(from);
            q = q.Where(l => l.LoginAt >= f);
        }

        if (query.LoginAtTo is { } to)
        {
            var t = ToUtc(to);
            q = q.Where(l => l.LoginAt <= t);
        }

        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderByDescending(l => l.LoginAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(l => new LoginLogListItemDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.UserName,
            LoginAt = l.LoginAt,
            ClientIp = l.ClientIp,
            AddressLine = l.AddressLine,
            LoginMethod = l.LoginMethod
        }).ToList();

        return new LoginLogPagedResult
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }
}
