using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Commission;

public sealed class CommissionDynamicCalculator : ICommissionDynamicCalculator
{
    readonly ApplicationDbContext _db;
    readonly CommissionPoolQuery _pool;
    readonly ILogger<CommissionDynamicCalculator> _logger;

    public CommissionDynamicCalculator(
        ApplicationDbContext db,
        CommissionPoolQuery pool,
        ILogger<CommissionDynamicCalculator> logger)
    {
        _db = db;
        _pool = pool;
        _logger = logger;
    }

    public async Task<CommissionCalcRunResult> RecalcAsync(
        DateOnly calcDate,
        CancellationToken cancellationToken = default)
    {
        var lockDate = CommissionTerm.GetNextLockDate(calcDate);
        var lockedKeys = (await _db.CommissionLockeds.AsNoTracking()
                .Select(x => new { x.RoleType, x.StockOutItemId })
                .ToListAsync(cancellationToken))
            .Select(x => (x.RoleType, x.StockOutItemId))
            .ToHashSet();

        var facts = await _pool.LoadAsync(_db, cancellationToken);
        await ReconcilePoolAsync(facts, lockedKeys, cancellationToken);

        var pools = await _db.CommissionPools.AsNoTracking().ToListAsync(cancellationToken);
        var work = new List<WorkLine>();
        foreach (var row in pools)
        {
            TryAdd(work, (short)CommissionRoleType.Sales, row.SalesUserId, row, lockDate, lockedKeys);
            if (row.PurchaseCommissionStatus != CommissionOfficialFlag.NotApplicable)
                TryAdd(work, (short)CommissionRoleType.Purchase, row.PurchaseUserId, row, lockDate, lockedKeys);
        }

        var userIds = work.Select(x => x.UserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? []
            : await _db.Users.AsNoTracking()
                .Where(x => userIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
        var userMap = users.ToDictionary(x => x.Id);
        var histories = userIds.Count == 0
            ? []
            : await _db.UserLevelHistories.AsNoTracking()
                .Where(x => userIds.Contains(x.UserId))
                .ToListAsync(cancellationToken);
        var historyByUser = histories
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UserLevelChangePoint>)g
                    .Select(h => new UserLevelChangePoint(h.ChangeTime, h.OldLevel, h.NewLevel))
                    .ToList());

        var versions = await _db.CommissionRateVersions.AsNoTracking()
            .Where(x => x.Status == CommissionRateVersionStatus.Active)
            .ToListAsync(cancellationToken);
        var versionByRole = versions.ToDictionary(x => x.RoleType, x => x.Id);
        var versionIds = versions.Select(x => x.Id).ToList();
        var rates = versionIds.Count == 0
            ? []
            : await _db.CommissionRates.AsNoTracking()
                .Where(x => versionIds.Contains(x.VersionId))
                .ToListAsync(cancellationToken);
        var rateMap = rates.ToDictionary(x => (x.VersionId, x.UserLevel));

        var monthGp = work
            .GroupBy(x => (x.RoleType, x.UserId, x.CalcMonth))
            .ToDictionary(g => g.Key, g => g.Sum(x => CommissionLadderRules.CalcGp(x.GpUsd)));

        var now = DateTime.UtcNow;
        var entities = new List<CommissionDynamic>(work.Count);
        foreach (var line in work)
        {
            userMap.TryGetValue(line.UserId, out var user);
            historyByUser.TryGetValue(line.UserId, out var hist);
            var asOf = CommissionBaseOtherRules.CalcMonthStart(line.CalcMonth) ?? calcDate;
            var level = UserLevelAsOf.Resolve(user?.Level ?? UserLevelCode.Default, hist ?? [], asOf);
            var period = monthGp[(line.RoleType, line.UserId, line.CalcMonth)];
            versionByRole.TryGetValue(line.RoleType, out var versionId);
            CommissionLadderSlot[] slots = [];
            if (!string.IsNullOrWhiteSpace(versionId)
                && rateMap.TryGetValue((versionId, level), out var rate))
            {
                slots = rate.GetSlots();
            }

            var points = CommissionLadderRules.ResolveMonthlyPoints(slots, period);
            var calcGp = CommissionLadderRules.CalcGp(line.GpUsd);
            var commission = decimal.Round(calcGp * points / 100m, 2, MidpointRounding.AwayFromZero);
            entities.Add(new CommissionDynamic
            {
                Id = Guid.NewGuid().ToString(),
                RoleType = line.RoleType,
                StockOutItemId = line.StockOutItemId,
                UserId = line.UserId,
                UserName = (user?.UserName ?? line.UserId).Trim(),
                UserLevel = level,
                VersionId = versionId,
                RatePoints = points,
                GpUsd = line.GpUsd,
                PeriodGpUsd = decimal.Round(period, 2, MidpointRounding.AwayFromZero),
                CommissionUsd = commission,
                SellOrderId = line.Pool.SellOrderId,
                SellOrderCode = line.Pool.SellOrderCode,
                SellOrderItemId = line.Pool.SellOrderItemId,
                SellOrderItemCode = line.Pool.SellOrderItemCode,
                PurchaseOrderId = line.Pool.PurchaseOrderId,
                PurchaseOrderCode = line.Pool.PurchaseOrderCode,
                StockOutId = line.Pool.StockOutId,
                StockOutCode = line.Pool.StockOutCode,
                StockOutDate = line.Pool.StockOutDate,
                ReceiptDate = line.Pool.ReceiptDate!.Value,
                PoolDate = line.PoolDate,
                CalcMonth = line.CalcMonth,
                EntryKind = (short)line.Kind,
                CalcDate = calcDate,
                CalcAt = now,
                CreateTime = now,
                ModifyTime = now
            });
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var old = await _db.CommissionDynamics.ToListAsync(cancellationToken);
        _db.CommissionDynamics.RemoveRange(old);
        await _db.SaveChangesAsync(cancellationToken);
        if (entities.Count > 0)
            await _db.CommissionDynamics.AddRangeAsync(entities, cancellationToken);
        await TouchWatermarkAsync(calcDate, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Commission dynamic recalc calcDate={CalcDate} lockDate={LockDate} sales={Sales} purchase={Purchase}",
            calcDate,
            lockDate,
            entities.Count(x => x.RoleType == (short)CommissionRoleType.Sales),
            entities.Count(x => x.RoleType == (short)CommissionRoleType.Purchase));

        return new CommissionCalcRunResult
        {
            CalcDate = calcDate,
            SalesCount = entities.Count(x => x.RoleType == (short)CommissionRoleType.Sales),
            PurchaseCount = entities.Count(x => x.RoleType == (short)CommissionRoleType.Purchase)
        };
    }

    async Task ReconcilePoolAsync(
        IReadOnlyList<CommissionPoolCandidate> facts,
        HashSet<(short RoleType, string StockOutItemId)> lockedKeys,
        CancellationToken cancellationToken)
    {
        var existing = await _db.CommissionPools.ToDictionaryAsync(x => x.StockOutItemId, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var fact in facts)
        {
            var purchaseNa = string.IsNullOrWhiteSpace(fact.PurchaseOrderItemId);
            var salesLocked = lockedKeys.Contains(((short)CommissionRoleType.Sales, fact.StockOutItemId));
            var purchaseLocked = lockedKeys.Contains(((short)CommissionRoleType.Purchase, fact.StockOutItemId));

            if (!existing.TryGetValue(fact.StockOutItemId, out var row))
            {
                row = new CommissionPool
                {
                    Id = Guid.NewGuid().ToString(),
                    StockOutItemId = fact.StockOutItemId,
                    CreateTime = now
                };
                await _db.CommissionPools.AddAsync(row, cancellationToken);
                existing[fact.StockOutItemId] = row;
            }

            CopyFacts(row, fact);
            if (row.SalesCommissionStatus != CommissionOfficialFlag.Locked)
                row.SalesCommissionStatus = salesLocked ? CommissionOfficialFlag.Locked : CommissionOfficialFlag.Open;
            if (row.PurchaseCommissionStatus != CommissionOfficialFlag.Locked)
            {
                row.PurchaseCommissionStatus = purchaseLocked
                    ? CommissionOfficialFlag.Locked
                    : purchaseNa
                        ? CommissionOfficialFlag.NotApplicable
                        : CommissionOfficialFlag.Open;
            }

            row.ModifyTime = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    static void CopyFacts(CommissionPool row, CommissionPoolCandidate fact)
    {
        row.StockOutId = fact.StockOutId;
        row.StockOutCode = fact.StockOutCode;
        row.StockOutDate = fact.StockOutDate;
        row.SalesUserId = fact.SalesUserId;
        row.PurchaseUserId = fact.PurchaseUserId;
        row.GpUsd = fact.GpUsd;
        row.ReceiptProgressStatus = fact.ReceiptProgressStatus;
        row.ReceiptDate = fact.ReceiptDate;
        row.SellOrderId = fact.SellOrderId;
        row.SellOrderCode = fact.SellOrderCode;
        row.SellOrderItemId = fact.SellOrderItemId;
        row.SellOrderItemCode = fact.SellOrderItemCode;
        row.PurchaseOrderId = fact.PurchaseOrderId;
        row.PurchaseOrderCode = fact.PurchaseOrderCode;
        row.PurchaseOrderItemId = fact.PurchaseOrderItemId;
        row.PurchaseOrderItemCode = fact.PurchaseOrderItemCode;
    }

    static void TryAdd(
        List<WorkLine> work,
        short roleType,
        string? userId,
        CommissionPool row,
        DateOnly lockDate,
        HashSet<(short RoleType, string StockOutItemId)> lockedKeys)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;
        if (lockedKeys.Contains((roleType, row.StockOutItemId)))
            return;
        var flag = roleType == (short)CommissionRoleType.Sales
            ? row.SalesCommissionStatus
            : row.PurchaseCommissionStatus;
        if (flag is CommissionOfficialFlag.Locked or CommissionOfficialFlag.NotApplicable)
            return;
        if (row.ReceiptProgressStatus != 2 || !row.ReceiptDate.HasValue)
            return;

        var kind = CommissionBaseOtherRules.Classify(row.StockOutDate, row.ReceiptDate, lockDate);
        var calcMonth = CommissionBaseOtherRules.CalcMonth(kind, row.StockOutDate, row.ReceiptDate);
        if (kind == CommissionEntryKind.None || string.IsNullOrWhiteSpace(calcMonth))
            return;

        var poolDate = kind == CommissionEntryKind.Base
            ? row.StockOutDate ?? row.ReceiptDate.Value
            : row.ReceiptDate.Value;
        work.Add(new WorkLine
        {
            RoleType = roleType,
            UserId = userId.Trim(),
            StockOutItemId = row.StockOutItemId,
            GpUsd = row.GpUsd,
            Kind = kind,
            CalcMonth = calcMonth,
            PoolDate = poolDate,
            Pool = row
        });
    }

    async Task TouchWatermarkAsync(DateOnly calcDate, CancellationToken cancellationToken)
    {
        var row = await _db.CommissionJobWatermarks
            .FirstOrDefaultAsync(x => x.Id == CommissionJobWatermarkCode.SeedId, cancellationToken);
        if (row == null)
        {
            row = new CommissionJobWatermark { Id = CommissionJobWatermarkCode.SeedId };
            await _db.CommissionJobWatermarks.AddAsync(row, cancellationToken);
        }

        row.DynamicJobDate = calcDate;
        row.ModifyTime = DateTime.UtcNow;
    }

    sealed class WorkLine
    {
        public short RoleType { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string StockOutItemId { get; init; } = string.Empty;
        public decimal GpUsd { get; init; }
        public CommissionEntryKind Kind { get; init; }
        public string CalcMonth { get; init; } = string.Empty;
        public DateOnly PoolDate { get; init; }
        public required CommissionPool Pool { get; init; }
    }
}
