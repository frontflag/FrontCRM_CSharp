using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed class IncentiveTargetService : IIncentiveTargetService
{
    readonly ApplicationDbContext _db;
    readonly IRbacService _rbac;

    public IncentiveTargetService(ApplicationDbContext db, IRbacService rbac)
    {
        _db = db;
        _rbac = rbac;
    }

    public async Task<IncentiveTargetMineDto> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var roleType = await ResolveRoleTypeAsync(userId);
        return await BuildAsync(userId, roleType, cancellationToken);
    }

    public async Task<IncentiveTargetMineDto> PutMineAsync(
        string userId,
        IncentiveTargetPutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IncentiveTargetRules.TryNormalizeTarget(request.TermTargetUsd, out var termTarget, out var termError))
            throw new ArgumentException(termError);
        if (!IncentiveTargetRules.TryNormalizeTarget(request.YearTargetUsd, out var yearTarget, out var yearError))
            throw new ArgumentException(yearError);

        var roleType = await ResolveRoleTypeAsync(userId);
        var today = CommissionShanghai.Today();
        var open = CommissionTerm.GetOpenWindow(today);
        var yearKey = today.Year.ToString("D4");

        await UpsertAsync(userId, roleType, IncentiveTargetPeriodKinds.Term, open.Term, termTarget, cancellationToken);
        await UpsertAsync(userId, roleType, IncentiveTargetPeriodKinds.Year, yearKey, yearTarget, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await BuildAsync(userId, roleType, cancellationToken);
    }

    async Task<short> ResolveRoleTypeAsync(string userId)
    {
        var summary = await _rbac.GetUserPermissionSummaryAsync(userId);
        return IncentiveTargetRules.RoleTypeFromIdentity(summary.IdentityType);
    }

    async Task<IncentiveTargetMineDto> BuildAsync(string userId, short roleType, CancellationToken ct)
    {
        var today = CommissionShanghai.Today();
        var open = CommissionTerm.GetOpenWindow(today);
        var yearKey = today.Year.ToString("D4");
        var termMonths = new[]
        {
            CommissionTerm.FormatMonth(open.From),
            CommissionTerm.FormatMonth(open.To)
        };

        var termActualTask = _db.CommissionDynamics
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RoleType == roleType && termMonths.Contains(x.CalcMonth))
            .SumAsync(x => (decimal?)x.CommissionUsd, ct);
        var yearDynTask = _db.CommissionDynamics
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RoleType == roleType && x.CalcMonth.StartsWith(yearKey))
            .SumAsync(x => (decimal?)x.CommissionUsd, ct);
        var yearLockTask = _db.CommissionLockeds
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RoleType == roleType && x.CalcMonth.StartsWith(yearKey))
            .SumAsync(x => (decimal?)x.CommissionUsd, ct);

        var keys = new[] { open.Term, yearKey };
        var targets = await _db.IncentiveTargets
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.RoleType == roleType && keys.Contains(x.PeriodKey))
            .ToListAsync(ct);

        var termTarget = targets.FirstOrDefault(x =>
            x.PeriodKind == IncentiveTargetPeriodKinds.Term && x.PeriodKey == open.Term);
        var yearTarget = targets.FirstOrDefault(x =>
            x.PeriodKind == IncentiveTargetPeriodKinds.Year && x.PeriodKey == yearKey);

        var termActual = await termActualTask ?? 0m;
        var yearActual = (await yearDynTask ?? 0m) + (await yearLockTask ?? 0m);
        var termAmt = HasAmount(termTarget) ? termTarget!.TargetAmountUsd : (decimal?)null;
        var yearAmt = HasAmount(yearTarget) ? yearTarget!.TargetAmountUsd : (decimal?)null;

        return new IncentiveTargetMineDto
        {
            RoleType = roleType,
            Term = new IncentiveTargetPeriodDto
            {
                PeriodKey = open.Term,
                From = open.From,
                To = open.To,
                Label = CommissionTerm.FormatTermLabel(open.Term),
                MonthSpan = IncentiveTargetRules.MonthSpan(open.From, open.To),
                HasTarget = termAmt is > 0m,
                TargetUsd = termAmt,
                ActualUsd = termActual,
                CompletionPct = IncentiveTargetRules.CompletionPct(termActual, termAmt)
            },
            Year = new IncentiveTargetYearDto
            {
                PeriodKey = yearKey,
                HasTarget = yearAmt is > 0m,
                TargetUsd = yearAmt,
                ActualUsd = yearActual,
                CompletionPct = IncentiveTargetRules.CompletionPct(yearActual, yearAmt)
            }
        };
    }

    async Task UpsertAsync(
        string userId,
        short roleType,
        short periodKind,
        string periodKey,
        decimal? target,
        CancellationToken ct)
    {
        var row = await _db.IncentiveTargets.FirstOrDefaultAsync(
            x => x.UserId == userId
                && x.RoleType == roleType
                && x.PeriodKind == periodKind
                && x.PeriodKey == periodKey,
            ct);

        if (target is null)
        {
            if (row != null)
            {
                row.IsDeleted = true;
                row.ModifyTime = DateTime.UtcNow;
            }

            return;
        }

        if (row == null)
        {
            _db.IncentiveTargets.Add(new IncentiveTarget
            {
                UserId = userId,
                RoleType = roleType,
                PeriodKind = periodKind,
                PeriodKey = periodKey,
                TargetAmountUsd = target.Value,
                CreateTime = DateTime.UtcNow,
                ModifyTime = DateTime.UtcNow
            });
            return;
        }

        row.TargetAmountUsd = target.Value;
        row.ModifyTime = DateTime.UtcNow;
    }

    static bool HasAmount(IncentiveTarget? row) => row is { TargetAmountUsd: > 0m };
}
