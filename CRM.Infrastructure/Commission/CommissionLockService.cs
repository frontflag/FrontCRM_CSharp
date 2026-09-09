using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Commission;

public sealed class CommissionLockService : ICommissionLockService
{
    readonly ApplicationDbContext _db;
    readonly ICommissionDynamicCalculator _calculator;
    readonly ILogger<CommissionLockService> _logger;

    public CommissionLockService(
        ApplicationDbContext db,
        ICommissionDynamicCalculator calculator,
        ILogger<CommissionLockService> logger)
    {
        _db = db;
        _calculator = calculator;
        _logger = logger;
    }

    public async Task<CommissionLockRunResult> LockAsync(
        DateOnly lockDate,
        CancellationToken cancellationToken = default)
    {
        var resolved = CommissionTerm.ResolveLockDate(lockDate);
        await _calculator.RecalcAsync(resolved, cancellationToken);

        var window = CommissionTerm.GetWindow(resolved);
        var term = window.Term;
        var now = DateTime.UtcNow;

        var dynamics = await _db.CommissionDynamics.ToListAsync(cancellationToken);

        var existing = (await _db.CommissionLockeds.AsNoTracking()
                .Select(x => new { x.RoleType, x.StockOutItemId })
                .ToListAsync(cancellationToken))
            .Select(x => (x.RoleType, x.StockOutItemId))
            .ToHashSet();

        var insert = new List<CommissionLocked>();
        var delete = new List<CommissionDynamic>();
        var skipped = 0;
        foreach (var row in dynamics)
        {
            delete.Add(row);
            if (existing.Contains((row.RoleType, row.StockOutItemId)))
            {
                skipped++;
                continue;
            }

            insert.Add(Copy(row, term, resolved, now));
            existing.Add((row.RoleType, row.StockOutItemId));
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        if (insert.Count > 0)
            await _db.CommissionLockeds.AddRangeAsync(insert, cancellationToken);
        if (delete.Count > 0)
            _db.CommissionDynamics.RemoveRange(delete);
        await MarkPoolOfficialAsync(insert, now, cancellationToken);
        await TouchWatermarkAsync(term, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Commission lock lockDate={LockDate} term={Term} inserted={Inserted} skipped={Skipped}",
            resolved, term, insert.Count, skipped);

        return new CommissionLockRunResult
        {
            LockDate = resolved,
            Term = term,
            WindowFrom = window.From,
            WindowTo = window.To,
            Inserted = insert.Count,
            SkippedExisting = skipped
        };
    }

    static CommissionLocked Copy(CommissionDynamic src, string term, DateOnly lockDate, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            RoleType = src.RoleType,
            StockOutItemId = src.StockOutItemId,
            UserId = src.UserId,
            UserName = src.UserName,
            UserLevel = src.UserLevel,
            VersionId = src.VersionId,
            RatePoints = src.RatePoints,
            GpUsd = src.GpUsd,
            PeriodGpUsd = src.PeriodGpUsd,
            CommissionUsd = src.CommissionUsd,
            SellOrderId = src.SellOrderId,
            SellOrderCode = src.SellOrderCode,
            SellOrderItemId = src.SellOrderItemId,
            SellOrderItemCode = src.SellOrderItemCode,
            PurchaseOrderId = src.PurchaseOrderId,
            PurchaseOrderCode = src.PurchaseOrderCode,
            StockOutId = src.StockOutId,
            StockOutCode = src.StockOutCode,
            StockOutDate = src.StockOutDate,
            ReceiptDate = src.ReceiptDate,
            PoolDate = src.PoolDate,
            CalcMonth = src.CalcMonth,
            EntryKind = src.EntryKind,
            CalcDate = src.CalcDate,
            CalcAt = src.CalcAt,
            Term = term,
            LockDate = lockDate,
            LockedAt = now,
            CreateTime = now,
            ModifyTime = now
        };

    async Task MarkPoolOfficialAsync(
        List<CommissionLocked> insert,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (insert.Count == 0)
            return;

        var itemIds = insert.Select(x => x.StockOutItemId).Distinct().ToList();
        var pools = await _db.CommissionPools
            .Where(x => itemIds.Contains(x.StockOutItemId))
            .ToListAsync(cancellationToken);
        var byItem = pools.ToDictionary(x => x.StockOutItemId, StringComparer.OrdinalIgnoreCase);
        foreach (var row in insert)
        {
            if (!byItem.TryGetValue(row.StockOutItemId, out var pool))
                continue;
            if (row.RoleType == (short)CommissionRoleType.Sales)
                pool.SalesCommissionStatus = CommissionOfficialFlag.Locked;
            else if (row.RoleType == (short)CommissionRoleType.Purchase)
                pool.PurchaseCommissionStatus = CommissionOfficialFlag.Locked;
            pool.ModifyTime = now;
        }
    }

    async Task TouchWatermarkAsync(string term, CancellationToken cancellationToken)
    {
        var row = await _db.CommissionJobWatermarks
            .FirstOrDefaultAsync(x => x.Id == CommissionJobWatermarkCode.SeedId, cancellationToken);
        if (row == null)
        {
            row = new CommissionJobWatermark { Id = CommissionJobWatermarkCode.SeedId };
            await _db.CommissionJobWatermarks.AddAsync(row, cancellationToken);
        }

        row.LockTerm = term;
        row.ModifyTime = DateTime.UtcNow;
    }
}
