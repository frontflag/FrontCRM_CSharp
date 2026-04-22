using CRM.Core.Interfaces;
using CRM.Core.Models.Dtos;

namespace CRM.Core.Services;

/// <summary>分页查询统一操作日志表 log_operation。</summary>
public sealed class OperationLogQueryService : IOperationLogQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public OperationLogQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private static string SqlQ(string? s) => (s ?? "").Replace("'", "''");

    private static string ToPgTimestamptzUtc(DateTime dt)
    {
        var u = dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
        return u.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    public async Task<OperationLogPagedResult> QueryAsync(OperationLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "TRUE" };

        if (!string.IsNullOrWhiteSpace(query.BizType))
            where.Add($@"o.""BizType"" = '{SqlQ(query.BizType.Trim())}'");

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            var p = SqlQ(query.ActionType.Trim());
            where.Add($@"strpos(lower(coalesce(o.""ActionType"",'')), lower('{p}')) > 0");
        }

        if (!string.IsNullOrWhiteSpace(query.RecordCode))
        {
            var p = SqlQ(query.RecordCode.Trim());
            where.Add($@"strpos(lower(coalesce(o.""RecordCode"",'')), lower('{p}')) > 0");
        }

        if (!string.IsNullOrWhiteSpace(query.OperatorUserName))
        {
            var p = SqlQ(query.OperatorUserName.Trim());
            where.Add($@"strpos(lower(coalesce(o.""OperatorUserName"",'')), lower('{p}')) > 0");
        }

        if (!string.IsNullOrWhiteSpace(query.Reason))
        {
            var p = SqlQ(query.Reason.Trim());
            where.Add($@"strpos(lower(coalesce(o.""Reason"",'')), lower('{p}')) > 0");
        }

        if (query.OperationTimeFrom is { } from)
            where.Add($@"o.""OperationTime"" >= '{SqlQ(ToPgTimestamptzUtc(from))}'::timestamptz");

        if (query.OperationTimeTo is { } to)
            where.Add($@"o.""OperationTime"" <= '{SqlQ(ToPgTimestamptzUtc(to))}'::timestamptz");

        var whereSql = string.Join(" AND ", where);

        var countSql = $@"SELECT COUNT(*)::bigint AS ""Count"" FROM log_operation o WHERE {whereSql}";
        var countRows = await _unitOfWork.QueryAsync<CountRow>(countSql);
        var total = (int)Math.Min(int.MaxValue, countRows.FirstOrDefault()?.Count ?? 0L);

        var listSql = $@"
SELECT o.""Id"", o.""BizType"", o.""RecordId"", o.""RecordCode"", o.""ActionType"", o.""OperationTime"",
       o.""OperatorUserId"", o.""OperatorUserName"", o.""Reason"", o.""OperationDesc""
FROM log_operation o
WHERE {whereSql}
ORDER BY o.""OperationTime"" DESC
LIMIT {pageSize} OFFSET {offset}";

        var items = (await _unitOfWork.QueryAsync<OperationLogListItemDto>(listSql)).ToList();

        return new OperationLogPagedResult
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
