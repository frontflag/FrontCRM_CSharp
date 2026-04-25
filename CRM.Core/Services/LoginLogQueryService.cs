using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;

namespace CRM.Core.Services;

public sealed class LoginLogQueryService : ILoginLogQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public LoginLogQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private static string SqlQ(string? s) => (s ?? "").Replace("'", "''");

    private static string ToPgTimestamptzUtc(DateTime dt)
    {
        var u = dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
        return u.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    public async Task<LoginLogPagedResult> QueryAsync(LoginLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "TRUE" };

        if (!string.IsNullOrWhiteSpace(query.UserId))
            where.Add($@"l.""UserId"" = '{SqlQ(query.UserId.Trim())}'");

        if (query.LoginAtFrom is { } from)
            where.Add($@"l.""LoginAt"" >= '{SqlQ(ToPgTimestamptzUtc(from))}'::timestamptz");

        if (query.LoginAtTo is { } to)
            where.Add($@"l.""LoginAt"" <= '{SqlQ(ToPgTimestamptzUtc(to))}'::timestamptz");

        var whereSql = string.Join(" AND ", where);

        var countSql = $@"SELECT COUNT(*)::bigint AS ""Count"" FROM log_login l WHERE {whereSql}";
        var countRows = await _unitOfWork.QueryAsync<CountRow>(countSql);
        var total = (int)Math.Min(int.MaxValue, countRows.FirstOrDefault()?.Count ?? 0L);

        var listSql = $@"
SELECT l.""Id"", l.""UserId"", l.""UserName"", l.""LoginAt"", l.""ClientIp"", l.""AddressLine"", l.""LoginMethod""
FROM log_login l
WHERE {whereSql}
ORDER BY l.""LoginAt"" DESC
LIMIT {pageSize} OFFSET {offset}";

        var items = (await _unitOfWork.QueryAsync<LoginLogListItemDto>(listSql)).ToList();

        return new LoginLogPagedResult
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    private sealed class CountRow
    {
        public long Count { get; set; }
    }
}
