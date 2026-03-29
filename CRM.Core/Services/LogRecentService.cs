using CRM.Core.Interfaces;
using CRM.Core.Models.Logs;

namespace CRM.Core.Services
{
    public class LogRecentService : ILogRecentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LogRecentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static string SqlQ(string? s) => (s ?? "").Replace("'", "''");

        public async Task RecordAsync(string userId, string bizType, string recordId, string? recordCode, string openKind)
        {
            var uid = SqlQ(userId);
            var biz = SqlQ(bizType);
            var rid = SqlQ(recordId);
            var kind = SqlQ(openKind);
            var codeSql = string.IsNullOrWhiteSpace(recordCode) ? "NULL" : $"'{SqlQ(recordCode)}'";

            var insert = $@"
INSERT INTO public.log_recent (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""AccessedAt"", ""UserId"", ""OpenKind"")
VALUES (gen_random_uuid()::text, '{biz}', '{rid}', {codeSql}, NOW(), '{uid}', '{kind}')
ON CONFLICT (""UserId"", ""BizType"", ""RecordId"") DO UPDATE SET
  ""RecordCode"" = EXCLUDED.""RecordCode"",
  ""AccessedAt"" = EXCLUDED.""AccessedAt"",
  ""OpenKind"" = EXCLUDED.""OpenKind"";";
            await _unitOfWork.ExecuteAsync(insert);

            var trim = $@"
DELETE FROM public.log_recent a
WHERE a.""UserId"" = '{uid}' AND a.""BizType"" = '{biz}'
AND NOT EXISTS (
  SELECT 1 FROM (
    SELECT ""Id"" FROM public.log_recent
    WHERE ""UserId"" = '{uid}' AND ""BizType"" = '{biz}'
    ORDER BY ""AccessedAt"" DESC
    LIMIT 100
  ) keep WHERE keep.""Id"" = a.""Id""
);";
            await _unitOfWork.ExecuteAsync(trim);
        }

        public async Task<IReadOnlyList<LogRecentItem>> ListAsync(string userId, string bizType, int take)
        {
            if (take < 1) take = 20;
            if (take > 100) take = 100;
            var uid = SqlQ(userId);
            var biz = SqlQ(bizType);
            var sql = $@"
SELECT ""RecordId"", ""RecordCode"", ""AccessedAt"", ""OpenKind""
FROM public.log_recent
WHERE ""UserId"" = '{uid}' AND ""BizType"" = '{biz}'
ORDER BY ""AccessedAt"" DESC
LIMIT {take}";
            var rows = await _unitOfWork.QueryAsync<LogRecentItem>(sql);
            return rows.ToList();
        }
    }
}
