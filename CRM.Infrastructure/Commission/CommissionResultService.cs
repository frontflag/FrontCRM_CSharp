using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Commission;

public sealed class CommissionResultService : ICommissionResultService
{
    readonly ApplicationDbContext _db;
    readonly ICommissionDynamicCalculator _calculator;
    readonly ICommissionLockService _lockService;

    public CommissionResultService(
        ApplicationDbContext db,
        ICommissionDynamicCalculator calculator,
        ICommissionLockService lockService)
    {
        _db = db;
        _calculator = calculator;
        _lockService = lockService;
    }

    public CommissionOpenWindowDto GetOpenWindow(DateOnly? asOf = null)
    {
        var day = asOf ?? CommissionShanghai.Today();
        var w = CommissionTerm.GetOpenWindow(day);
        return new CommissionOpenWindowDto
        {
            From = w.From,
            To = w.To,
            Term = w.Term,
            Label = CommissionTerm.FormatTermLabel(w.Term)
        };
    }

    public Task<CommissionCalcRunResult> RecalcEstimatedAsync(
        DateOnly? calcDate = null,
        CancellationToken cancellationToken = default) =>
        _calculator.RecalcAsync(calcDate ?? CommissionShanghai.Today(), cancellationToken);

    public Task<CommissionLockRunResult> LockOfficialAsync(
        DateOnly? lockDate = null,
        CancellationToken cancellationToken = default) =>
        _lockService.LockAsync(lockDate ?? CommissionShanghai.Today(), cancellationToken);

    public async Task<IReadOnlyList<CommissionTermOptionDto>> ListOfficialTermsAsync(
        short roleType,
        CancellationToken cancellationToken = default)
    {
        var terms = await _db.CommissionLockeds.AsNoTracking()
            .Where(x => x.RoleType == roleType)
            .Select(x => x.Term)
            .Distinct()
            .ToListAsync(cancellationToken);
        return terms
            .OrderByDescending(x => x)
            .Select(t => new CommissionTermOptionDto
            {
                Term = t.Trim(),
                Label = CommissionTerm.FormatTermLabel(t.Trim())
            })
            .ToList();
    }

    public async Task<CommissionPaged<CommissionSummaryDto>> ListSummaryAsync(
        CommissionResultQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = await ApplyAsync(query, cancellationToken);
        var grouped = q
            .GroupBy(x => x.UserId)
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.CalcMonth).ThenByDescending(x => x.PoolDate).First();
                return new CommissionSummaryDto
                {
                    UserId = g.Key,
                    UserName = latest.UserName,
                    UserLevel = latest.UserLevel,
                    LineCount = g.Count(),
                    MonthCount = g.Select(x => x.CalcMonth).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().Count(),
                    PeriodGpUsd = decimal.Round(g.Sum(x => CommissionLadderRules.CalcGp(x.GpUsd)), 2, MidpointRounding.AwayFromZero),
                    CommissionUsd = g.Sum(x => x.CommissionUsd)
                };
            })
            .ToList();

        if (!query.Official)
        {
            var roster = await LoadActiveRoleUsersAsync(query.RoleType, query.UserId, cancellationToken);
            grouped = CommissionSummaryRoster.Merge(grouped, roster, query.Keyword);
        }
        else
        {
            grouped = grouped
                .OrderByDescending(x => x.CommissionUsd)
                .ThenBy(x => x.UserName)
                .ToList();
        }

        return Page(grouped, query.Page, query.PageSize);
    }

    public async Task<CommissionPersonMonthsDto> ListMonthsAsync(
        CommissionPersonQuery query,
        CancellationToken cancellationToken = default)
    {
        var (userId, userName, userLevel, page, size) = await ResolvePersonAsync(query, cancellationToken);
        var lines = await LoadPersonResultLinesAsync(query, userId, cancellationToken);
        var (qualifyMap, activeVersionId) = await LoadFirstTierThresholdsAsync(query.RoleType, lines, cancellationToken);
        var months = lines
            .GroupBy(x => ResolveLineCalcMonth(x))
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.PoolDate).First();
                var monthGp = decimal.Round(g.Sum(x => CommissionLadderRules.CalcGp(x.GpUsd)), 2, MidpointRounding.AwayFromZero);
                return new CommissionMonthDto
                {
                    UserId = userId,
                    UserName = latest.UserName,
                    UserLevel = latest.UserLevel,
                    CalcMonth = g.Key,
                    Qualified = g.Any(x => x.RatePoints > 0m),
                    MonthGpUsd = monthGp,
                    RatePoints = latest.RatePoints,
                    CommissionUsd = g.Sum(x => x.CommissionUsd),
                    LineCount = g.Count(),
                    QualifyGpUsd = ResolveFirstTierThreshold(qualifyMap, latest.VersionId, activeVersionId, latest.UserLevel),
                    Term = latest.Term
                };
            })
            .OrderByDescending(x => x.CalcMonth)
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            months = months
                .Where(x => x.CalcMonth.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var paged = Page(months, page, size);
        return new CommissionPersonMonthsDto
        {
            UserId = userId,
            UserName = userName,
            UserLevel = userLevel,
            Items = paged.Items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalLineCount = months.Sum(x => x.LineCount),
            TotalCommissionUsd = decimal.Round(months.Sum(x => x.CommissionUsd), 2, MidpointRounding.AwayFromZero)
        };
    }

    public async Task<CommissionPaged<CommissionLineDto>> ListLinesAsync(
        CommissionResultQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = (await ApplyAsync(query, cancellationToken))
            .OrderByDescending(x => x.PoolDate)
            .ThenByDescending(x => x.StockOutCode)
            .ToList();
        return Page(q, query.Page, query.PageSize);
    }

    public async Task<CommissionPersonDetailDto> ListPersonLinesAsync(
        CommissionPersonQuery query,
        CancellationToken cancellationToken = default)
    {
        var (userId, userName, userLevel, page, size) = await ResolvePersonAsync(query, cancellationToken);
        var calcMonth = NormalizeCalcMonth(query.CalcMonth);
        if (calcMonth == null)
            throw new ArgumentOutOfRangeException(nameof(query.CalcMonth), "须指定计算月");

        var lines = (await LoadPersonResultLinesAsync(query, userId, cancellationToken))
            .Where(x => string.Equals(x.CalcMonth, calcMonth, StringComparison.Ordinal))
            .Select(x => new CommissionPersonLineDto
            {
                StockOutItemId = x.StockOutItemId,
                StockOutId = x.StockOutId,
                StockOutCode = x.StockOutCode,
                StockOutDate = x.StockOutDate,
                UserId = x.UserId,
                UserName = x.UserName,
                UserLevel = x.UserLevel,
                ReceiptWriteOffDone = true,
                ReceiptProgressStatus = 2,
                ReceiptDate = x.ReceiptDate,
                DaysSinceReceipt = null,
                InCommission = true,
                GpUsd = x.GpUsd,
                RatePoints = x.RatePoints,
                CommissionUsd = x.CommissionUsd,
                CalcMonth = x.CalcMonth,
                EntryKind = x.EntryKind,
                SellOrderId = x.SellOrderId,
                SellOrderCode = x.SellOrderCode,
                PurchaseOrderId = x.PurchaseOrderId,
                PurchaseOrderCode = x.PurchaseOrderCode,
                Term = x.Term
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            lines = lines
                .Where(x => x.StockOutCode.Contains(kw, StringComparison.OrdinalIgnoreCase)
                            || (x.SellOrderCode != null && x.SellOrderCode.Contains(kw, StringComparison.OrdinalIgnoreCase))
                            || (x.PurchaseOrderCode != null && x.PurchaseOrderCode.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        lines = lines
            .OrderByDescending(x => x.StockOutDate)
            .ThenByDescending(x => x.StockOutCode)
            .ToList();

        var paged = Page(lines, page, size);
        return new CommissionPersonDetailDto
        {
            UserId = userId,
            UserName = userName,
            UserLevel = userLevel,
            Items = paged.Items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    async Task<(string UserId, string UserName, short UserLevel, int Page, int Size)> ResolvePersonAsync(
        CommissionPersonQuery query,
        CancellationToken cancellationToken)
    {
        if (!CommissionLadder.IsRoleType(query.RoleType))
            throw new ArgumentOutOfRangeException(nameof(query.RoleType), "类型须为业务员或采购员");
        if (string.IsNullOrWhiteSpace(query.UserId))
            throw new ArgumentOutOfRangeException(nameof(query.UserId), "须指定人员");

        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;
        var userId = query.UserId.Trim();
        var user = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.UserName, u.Level })
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        var userName = string.IsNullOrWhiteSpace(user.UserName) ? user.Id : user.UserName.Trim();
        var userLevel = UserLevelCode.IsValid(user.Level) ? user.Level : UserLevelCode.Default;
        return (userId, userName, userLevel, page, size);
    }

    async Task<(Dictionary<(string VersionId, short UserLevel), decimal?> Map, string? ActiveVersionId)>
        LoadFirstTierThresholdsAsync(
            short roleType,
            IReadOnlyList<CommissionLineDto> lines,
            CancellationToken cancellationToken)
    {
        var versionIds = lines
            .Select(x => x.VersionId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var activeId = await _db.CommissionRateVersions.AsNoTracking()
            .Where(x => x.RoleType == roleType && x.Status == CommissionRateVersionStatus.Active)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(activeId)
            && !versionIds.Contains(activeId, StringComparer.Ordinal))
            versionIds.Add(activeId);
        if (versionIds.Count == 0)
            return (new Dictionary<(string, short), decimal?>(), activeId);

        var rates = await _db.CommissionRates.AsNoTracking()
            .Where(x => versionIds.Contains(x.VersionId))
            .Select(x => new { x.VersionId, x.UserLevel, x.Threshold1 })
            .ToListAsync(cancellationToken);
        var map = new Dictionary<(string VersionId, short UserLevel), decimal?>();
        foreach (var rate in rates)
            map[(rate.VersionId, rate.UserLevel)] = rate.Threshold1;
        return (map, activeId);
    }

    static decimal? ResolveFirstTierThreshold(
        Dictionary<(string VersionId, short UserLevel), decimal?> map,
        string? versionId,
        string? activeVersionId,
        short userLevel)
    {
        if (!string.IsNullOrWhiteSpace(versionId)
            && map.TryGetValue((versionId, userLevel), out var stored))
            return stored;
        if (!string.IsNullOrWhiteSpace(activeVersionId)
            && map.TryGetValue((activeVersionId, userLevel), out var active))
            return active;
        return null;
    }

    async Task<List<CommissionLineDto>> LoadPersonResultLinesAsync(
        CommissionPersonQuery query,
        string userId,
        CancellationToken cancellationToken)
    {
        var resultQuery = new CommissionResultQuery
        {
            RoleType = query.RoleType,
            Official = query.Official,
            UserId = userId,
            Term = query.Term,
            Page = 1,
            PageSize = 200
        };
        return await ApplyAsync(resultQuery, cancellationToken);
    }

    static string? NormalizeCalcMonth(string? value)
    {
        var raw = value?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (raw.Length == 6 && raw.All(char.IsDigit))
            raw = $"{raw[..4]}-{raw[4..]}";
        return CommissionBaseOtherRules.CalcMonthStart(raw) == null ? null : raw;
    }

    static string ResolveLineCalcMonth(CommissionLineDto x) =>
        ResolveStoredCalcMonth(x.CalcMonth, x.EntryKind, x.StockOutDate, x.ReceiptDate, x.PoolDate);

    static string ResolveStoredCalcMonth(
        string? stored,
        short entryKind,
        DateOnly? stockOutDate,
        DateOnly receiptDate,
        DateOnly poolDate) =>
        CommissionBaseOtherRules.ResolveCalcMonth(
            stored,
            (CommissionEntryKind)entryKind,
            stockOutDate,
            receiptDate,
            poolDate);

    async Task<IReadOnlyList<CommissionRosterPerson>> LoadActiveRoleUsersAsync(
        short roleType,
        string? onlyUserId,
        CancellationToken cancellationToken)
    {
        var departments = await _db.RbacDepartments.AsNoTracking()
            .Where(d => d.Status == 1)
            .ToListAsync(cancellationToken);
        var roleDeptIds = (roleType == (short)CommissionRoleType.Purchase
                ? departments.Where(PurchasingDepartmentRules.IsPurchaseDepartmentForRfqBuyer)
                : departments.Where(SalesDepartmentRules.IsSalesDepartment))
            .Select(d => d.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleDeptIds.Count == 0)
            return [];

        var userIds = await _db.RbacUserDepartments.AsNoTracking()
            .Where(x => roleDeptIds.Contains(x.DepartmentId))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(onlyUserId))
            userIds = userIds.Where(id => string.Equals(id, onlyUserId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (userIds.Count == 0)
            return [];

        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id)
                        && u.IsActive
                        && u.Status == UserAccountStatus.Active)
            .Select(u => new { u.Id, u.UserName, u.Level })
            .ToListAsync(cancellationToken);
        return users
            .Select(u => new CommissionRosterPerson(
                u.Id,
                string.IsNullOrWhiteSpace(u.UserName) ? u.Id : u.UserName.Trim(),
                UserLevelCode.IsValid(u.Level) ? u.Level : UserLevelCode.Default))
            .ToList();
    }

    async Task<List<CommissionLineDto>> ApplyAsync(CommissionResultQuery query, CancellationToken cancellationToken)
    {
        if (!CommissionLadder.IsRoleType(query.RoleType))
            throw new ArgumentOutOfRangeException(nameof(query.RoleType), "类型须为业务员或采购员");

        var page = query.Page < 1 ? 1 : query.Page;
        var size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;
        query = new CommissionResultQuery
        {
            RoleType = query.RoleType,
            Official = query.Official,
            Keyword = query.Keyword,
            UserId = query.UserId,
            Term = query.Term,
            PoolDateFrom = query.PoolDateFrom,
            PoolDateTo = query.PoolDateTo,
            Page = page,
            PageSize = size
        };

        if (query.Official)
            return await QueryLockedAsync(query, cancellationToken);
        return await QueryDynamicAsync(query, cancellationToken);
    }

    async Task<List<CommissionLineDto>> QueryDynamicAsync(CommissionResultQuery query, CancellationToken cancellationToken)
    {
        var q = _db.CommissionDynamics.AsNoTracking().Where(x => x.RoleType == query.RoleType);
        q = ApplyCommon(q, query);
        var rows = await q.ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    async Task<List<CommissionLineDto>> QueryLockedAsync(CommissionResultQuery query, CancellationToken cancellationToken)
    {
        var term = query.Term?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            term = await _db.CommissionLockeds.AsNoTracking()
                .Where(x => x.RoleType == query.RoleType)
                .Select(x => x.Term)
                .Distinct()
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var q = _db.CommissionLockeds.AsNoTracking().Where(x => x.RoleType == query.RoleType);
        if (!string.IsNullOrWhiteSpace(term))
            q = q.Where(x => x.Term == term);
        q = ApplyCommon(q, query);
        var rows = await q.ToListAsync(cancellationToken);
        return rows.Select(x => ToDto(x)).ToList();
    }

    static IQueryable<T> ApplyCommon<T>(IQueryable<T> q, CommissionResultQuery query)
        where T : class
    {
        if (q is IQueryable<CommissionDynamic> dyn)
        {
            IQueryable<CommissionDynamic> d = dyn;
            if (!string.IsNullOrWhiteSpace(query.UserId))
                d = d.Where(x => x.UserId == query.UserId);
            if (query.PoolDateFrom.HasValue)
                d = d.Where(x => x.PoolDate >= query.PoolDateFrom.Value);
            if (query.PoolDateTo.HasValue)
                d = d.Where(x => x.PoolDate <= query.PoolDateTo.Value);
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim();
                d = d.Where(x =>
                    x.UserName.Contains(kw)
                    || x.StockOutCode.Contains(kw)
                    || (x.SellOrderCode != null && x.SellOrderCode.Contains(kw))
                    || (x.PurchaseOrderCode != null && x.PurchaseOrderCode.Contains(kw)));
            }

            return (IQueryable<T>)d;
        }

        if (q is IQueryable<CommissionLocked> locked)
        {
            IQueryable<CommissionLocked> d = locked;
            if (!string.IsNullOrWhiteSpace(query.UserId))
                d = d.Where(x => x.UserId == query.UserId);
            if (query.PoolDateFrom.HasValue)
                d = d.Where(x => x.PoolDate >= query.PoolDateFrom.Value);
            if (query.PoolDateTo.HasValue)
                d = d.Where(x => x.PoolDate <= query.PoolDateTo.Value);
            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim();
                d = d.Where(x =>
                    x.UserName.Contains(kw)
                    || x.StockOutCode.Contains(kw)
                    || (x.SellOrderCode != null && x.SellOrderCode.Contains(kw))
                    || (x.PurchaseOrderCode != null && x.PurchaseOrderCode.Contains(kw)));
            }

            return (IQueryable<T>)d;
        }

        return q;
    }

    static CommissionLineDto ToDto(CommissionDynamic x) => new()
    {
        Id = x.Id,
        RoleType = x.RoleType,
        StockOutItemId = x.StockOutItemId,
        UserId = x.UserId,
        UserName = x.UserName,
        UserLevel = x.UserLevel,
        VersionId = x.VersionId,
        RatePoints = x.RatePoints,
        GpUsd = x.GpUsd,
        PeriodGpUsd = x.PeriodGpUsd,
        CommissionUsd = x.CommissionUsd,
        SellOrderId = x.SellOrderId,
        SellOrderCode = x.SellOrderCode,
        SellOrderItemId = x.SellOrderItemId,
        SellOrderItemCode = x.SellOrderItemCode,
        PurchaseOrderId = x.PurchaseOrderId,
        PurchaseOrderCode = x.PurchaseOrderCode,
        StockOutId = x.StockOutId,
        StockOutCode = x.StockOutCode,
        StockOutDate = x.StockOutDate,
        ReceiptDate = x.ReceiptDate,
        PoolDate = x.PoolDate,
        CalcMonth = ResolveStoredCalcMonth(x.CalcMonth, x.EntryKind, x.StockOutDate, x.ReceiptDate, x.PoolDate),
        EntryKind = x.EntryKind
    };

    static CommissionLineDto ToDto(CommissionLocked x) => new()
    {
        Id = x.Id,
        RoleType = x.RoleType,
        StockOutItemId = x.StockOutItemId,
        UserId = x.UserId,
        UserName = x.UserName,
        UserLevel = x.UserLevel,
        VersionId = x.VersionId,
        RatePoints = x.RatePoints,
        GpUsd = x.GpUsd,
        PeriodGpUsd = x.PeriodGpUsd,
        CommissionUsd = x.CommissionUsd,
        SellOrderId = x.SellOrderId,
        SellOrderCode = x.SellOrderCode,
        SellOrderItemId = x.SellOrderItemId,
        SellOrderItemCode = x.SellOrderItemCode,
        PurchaseOrderId = x.PurchaseOrderId,
        PurchaseOrderCode = x.PurchaseOrderCode,
        StockOutId = x.StockOutId,
        StockOutCode = x.StockOutCode,
        StockOutDate = x.StockOutDate,
        ReceiptDate = x.ReceiptDate,
        PoolDate = x.PoolDate,
        CalcMonth = ResolveStoredCalcMonth(x.CalcMonth, x.EntryKind, x.StockOutDate, x.ReceiptDate, x.PoolDate),
        EntryKind = x.EntryKind,
        Term = x.Term.Trim()
    };

    static CommissionPaged<T> Page<T>(List<T> items, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;
        var total = items.Count;
        var slice = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new CommissionPaged<T>
        {
            Items = slice,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
