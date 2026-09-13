using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.RiskAlert;

public sealed class RiskAlertService : IRiskAlertService
{
    readonly ApplicationDbContext _db;
    readonly IDataPermissionService _dataPermission;
    readonly IFinanceExchangeRateService _exchangeRates;

    public RiskAlertService(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IFinanceExchangeRateService exchangeRates)
    {
        _db = db;
        _dataPermission = dataPermission;
        _exchangeRates = exchangeRates;
    }

    public async Task<RiskAlertSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var row = await GetOrCreateAsync(cancellationToken);
        return Map(row);
    }

    public async Task<RiskAlertSettingsDto> PutSettingsAsync(
        RiskAlertSettingsPutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!RiskAlertRules.TryNormalizeUsd(request.InventoryAmountUsdMax, out var invAmt, out var e1))
            throw new ArgumentException(e1);
        if (!RiskAlertRules.TryNormalizeDays(request.StockAgeDaysMax, out var stockAge, out var e2))
            throw new ArgumentException(e2);
        if (!RiskAlertRules.TryNormalizeUsd(request.ReceivableAmountUsdMax, out var recAmt, out var e3))
            throw new ArgumentException(e3);
        if (!RiskAlertRules.TryNormalizeUsd(request.CustomerReceivableUsdMax, out var custAmt, out var e4))
            throw new ArgumentException(e4);
        if (!RiskAlertRules.TryNormalizeDays(request.SoReceivableAgeDaysMax, out var soAge, out var e5))
            throw new ArgumentException(e5);

        var row = await GetOrCreateAsync(cancellationToken);
        row.InventoryAmountUsdMax = invAmt;
        row.StockAgeDaysMax = stockAge;
        row.ReceivableAmountUsdMax = recAmt;
        row.CustomerReceivableUsdMax = custAmt;
        row.SoReceivableAgeDaysMax = soAge;
        row.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task<RiskAlertDashboardDto> GetDashboardAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        var today = CommissionShanghai.Today();
        var items = new List<RiskAlertItemDto>(5);

        var needStock = RiskAlertRules.IsEnabled(settings.InventoryAmountUsdMax)
            || RiskAlertRules.IsEnabled(settings.StockAgeDaysMax);
        var needReceivable = RiskAlertRules.IsEnabled(settings.ReceivableAmountUsdMax)
            || RiskAlertRules.IsEnabled(settings.CustomerReceivableUsdMax)
            || RiskAlertRules.IsEnabled(settings.SoReceivableAgeDaysMax);

        var stockRows = needStock
            ? await LoadStockRowsAsync(userId, cancellationToken)
            : new List<StockRow>();
        var recRows = needReceivable
            ? await LoadReceivableRowsAsync(userId, cancellationToken)
            : new List<ReceivableRow>();

        items.Add(BuildInventoryAmount(settings, stockRows));
        items.Add(BuildStockAge(settings, stockRows, today));
        items.Add(BuildReceivableAmount(settings, recRows));
        items.Add(BuildCustomerReceivable(settings, recRows));
        items.Add(BuildSoReceivableAge(settings, recRows, today));

        var triggered = items.Where(x => x.Triggered).ToList();
        return new RiskAlertDashboardDto
        {
            ImmediateCount = triggered.Count,
            AnyEnabled = items.Any(x => x.Enabled),
            Items = triggered
        };
    }

    async Task<RiskAlertSetting> GetOrCreateAsync(CancellationToken ct)
    {
        var row = await _db.RiskAlertSettings
            .FirstOrDefaultAsync(x => x.Id == RiskAlertSettingIds.Default, ct);
        if (row != null)
            return row;

        row = new RiskAlertSetting
        {
            Id = RiskAlertSettingIds.Default,
            InventoryAmountUsdMax = 0m,
            StockAgeDaysMax = RiskAlertLimits.DefaultAgeDays,
            ReceivableAmountUsdMax = 0m,
            CustomerReceivableUsdMax = 0m,
            SoReceivableAgeDaysMax = RiskAlertLimits.DefaultAgeDays,
            CreateTime = DateTime.UtcNow,
            ModifyTime = DateTime.UtcNow
        };
        _db.RiskAlertSettings.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    static RiskAlertSettingsDto Map(RiskAlertSetting row) =>
        new()
        {
            InventoryAmountUsdMax = row.InventoryAmountUsdMax,
            StockAgeDaysMax = row.StockAgeDaysMax,
            ReceivableAmountUsdMax = row.ReceivableAmountUsdMax,
            CustomerReceivableUsdMax = row.CustomerReceivableUsdMax,
            SoReceivableAgeDaysMax = row.SoReceivableAgeDaysMax
        };

    static RiskAlertItemDto BuildInventoryAmount(RiskAlertSetting settings, IReadOnlyList<StockRow> rows)
    {
        var enabled = RiskAlertRules.IsEnabled(settings.InventoryAmountUsdMax);
        var actual = Math.Round(rows.Sum(x => x.AmountUsd), 2, MidpointRounding.AwayFromZero);
        return new RiskAlertItemDto
        {
            Code = RiskAlertItemCodes.InventoryAmount,
            Enabled = enabled,
            Triggered = enabled && RiskAlertRules.ExceedsAmount(actual, settings.InventoryAmountUsdMax),
            ActualUsd = actual,
            ThresholdUsd = settings.InventoryAmountUsdMax,
            HitCount = enabled ? 1 : 0
        };
    }

    static RiskAlertItemDto BuildStockAge(
        RiskAlertSetting settings,
        IReadOnlyList<StockRow> rows,
        DateOnly today)
    {
        var enabled = RiskAlertRules.IsEnabled(settings.StockAgeDaysMax);
        var aged = rows
            .Select(r => (Row: r, Age: RiskAlertRules.AgeDays(today, r.StockInDate)))
            .Where(x => x.Age >= 0 && RiskAlertRules.ExceedsDays(x.Age, settings.StockAgeDaysMax))
            .OrderByDescending(x => x.Age)
            .ThenByDescending(x => x.Row.AmountUsd)
            .ToList();
        var worst = aged.FirstOrDefault();
        return new RiskAlertItemDto
        {
            Code = RiskAlertItemCodes.StockAge,
            Enabled = enabled,
            Triggered = enabled && aged.Count > 0,
            ActualDays = worst.Row is null ? null : worst.Age,
            ThresholdDays = settings.StockAgeDaysMax,
            HitCount = aged.Count,
            SubjectId = worst.Row?.StockItemId,
            SubjectCode = worst.Row?.PurchasePn,
            SubjectName = worst.Row?.PurchaseBrand
        };
    }

    static RiskAlertItemDto BuildReceivableAmount(RiskAlertSetting settings, IReadOnlyList<ReceivableRow> rows)
    {
        var enabled = RiskAlertRules.IsEnabled(settings.ReceivableAmountUsdMax);
        var actual = Math.Round(rows.Sum(x => x.AmountUsd), 2, MidpointRounding.AwayFromZero);
        return new RiskAlertItemDto
        {
            Code = RiskAlertItemCodes.ReceivableAmount,
            Enabled = enabled,
            Triggered = enabled && RiskAlertRules.ExceedsAmount(actual, settings.ReceivableAmountUsdMax),
            ActualUsd = actual,
            ThresholdUsd = settings.ReceivableAmountUsdMax,
            HitCount = enabled ? 1 : 0
        };
    }

    static RiskAlertItemDto BuildCustomerReceivable(RiskAlertSetting settings, IReadOnlyList<ReceivableRow> rows)
    {
        var enabled = RiskAlertRules.IsEnabled(settings.CustomerReceivableUsdMax);
        var groups = rows
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Name = g.Select(x => x.CustomerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                Amount = Math.Round(g.Sum(x => x.AmountUsd), 2, MidpointRounding.AwayFromZero)
            })
            .Where(x => RiskAlertRules.ExceedsAmount(x.Amount, settings.CustomerReceivableUsdMax))
            .OrderByDescending(x => x.Amount)
            .ToList();
        var worst = groups.FirstOrDefault();
        return new RiskAlertItemDto
        {
            Code = RiskAlertItemCodes.CustomerReceivable,
            Enabled = enabled,
            Triggered = enabled && groups.Count > 0,
            ActualUsd = worst?.Amount,
            ThresholdUsd = settings.CustomerReceivableUsdMax,
            HitCount = groups.Count,
            SubjectId = worst?.CustomerId,
            SubjectName = worst?.Name
        };
    }

    static RiskAlertItemDto BuildSoReceivableAge(
        RiskAlertSetting settings,
        IReadOnlyList<ReceivableRow> rows,
        DateOnly today)
    {
        var enabled = RiskAlertRules.IsEnabled(settings.SoReceivableAgeDaysMax);
        var groups = rows
            .Select(r => (Row: r, Age: RiskAlertRules.AgeDays(today, r.AnchorDate)))
            .Where(x => x.Age >= 0)
            .GroupBy(x => x.Row.SellOrderId)
            .Select(g => new
            {
                SellOrderId = g.Key,
                Code = g.Select(x => x.Row.SellOrderCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                Age = g.Max(x => x.Age),
                Amount = Math.Round(g.Sum(x => x.Row.AmountUsd), 2, MidpointRounding.AwayFromZero)
            })
            .Where(x => RiskAlertRules.ExceedsDays(x.Age, settings.SoReceivableAgeDaysMax))
            .OrderByDescending(x => x.Age)
            .ThenByDescending(x => x.Amount)
            .ToList();
        var worst = groups.FirstOrDefault();
        return new RiskAlertItemDto
        {
            Code = RiskAlertItemCodes.SoReceivableAge,
            Enabled = enabled,
            Triggered = enabled && groups.Count > 0,
            ActualDays = worst?.Age,
            ThresholdDays = settings.SoReceivableAgeDaysMax,
            ActualUsd = worst?.Amount,
            HitCount = groups.Count,
            SubjectId = worst?.SellOrderId,
            SubjectCode = worst?.Code
        };
    }

    async Task<List<StockRow>> LoadStockRowsAsync(string userId, CancellationToken ct)
    {
        var q = _db.StockItems.AsNoTracking()
            .Where(si => !si.IsDeleted && si.QtyRepertory > 0)
            .Where(si => si.TransferType == null || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource);
        q = await _dataPermission.ApplyStockItemListDataScopeAsync(
            userId,
            q,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            ct);

        var raw = await (
            from si in q
            join sin in _db.StockIns.AsNoTracking() on si.StockInId equals sin.Id into sinJoin
            from sin in sinJoin.DefaultIfEmpty()
            join oi in _db.PurchaseOrderItems.AsNoTracking() on si.PurchaseOrderItemId equals oi.Id into oiJoin
            from oi in oiJoin.DefaultIfEmpty()
            select new
            {
                si.Id,
                si.PurchasePn,
                si.PurchaseBrand,
                si.QtyRepertory,
                si.PurchasePriceUsd,
                ConvertPrice = oi != null ? oi.ConvertPrice : 0m,
                HasStockIn = sin != null && !sin.IsDeleted,
                StockInDate = sin != null ? sin.StockInDate : (DateTime?)null
            }).ToListAsync(ct);

        return raw.Select(x => new StockRow
        {
            StockItemId = x.Id,
            PurchasePn = x.PurchasePn,
            PurchaseBrand = x.PurchaseBrand,
            AmountUsd = x.ConvertPrice > 0m
                ? x.QtyRepertory * x.ConvertPrice
                : x.QtyRepertory * x.PurchasePriceUsd,
            StockInDate = x.HasStockIn ? x.StockInDate : null
        }).ToList();
    }

    async Task<List<ReceivableRow>> LoadReceivableRowsAsync(string userId, CancellationToken ct)
    {
        var q = _db.FinanceReceivables.AsNoTracking()
            .Where(r => !r.IsDeleted && r.VerifiedToBe > 0m);
        q = await _dataPermission.ApplyFinanceReceivableListDataScopeAsync(
            userId, q, _db.SellOrders.AsNoTracking(), ct);

        var raw = await (
            from r in q
            join oi in _db.SellOrderItems.AsNoTracking() on r.SellOrderItemId equals oi.Id into oiJoin
            from oi in oiJoin.DefaultIfEmpty()
            select new
            {
                r.CustomerId,
                r.CustomerName,
                r.SellOrderId,
                r.SellOrderCode,
                r.VerifiedToBe,
                r.Currency,
                r.StockOutDate,
                r.CreateTime,
                Price = oi != null ? oi.Price : 0m,
                ConvertPrice = oi != null ? oi.ConvertPrice : 0m
            }).ToListAsync(ct);

        var rates = await _exchangeRates.GetCurrentAsync(ct);
        return raw.Select(r => new ReceivableRow
        {
            CustomerId = r.CustomerId,
            CustomerName = r.CustomerName,
            SellOrderId = r.SellOrderId,
            SellOrderCode = r.SellOrderCode,
            AmountUsd = FinanceAnalyticsMoneyBuilder.FromExtend(
                r.VerifiedToBe,
                r.Currency,
                r.Price,
                r.ConvertPrice,
                rates.UsdToCny,
                rates.UsdToHkd,
                rates.UsdToEur).UsdAmount,
            AnchorDate = r.StockOutDate ?? r.CreateTime
        }).ToList();
    }

    sealed class StockRow
    {
        public string? StockItemId { get; init; }
        public string? PurchasePn { get; init; }
        public string? PurchaseBrand { get; init; }
        public decimal AmountUsd { get; init; }
        public DateTime? StockInDate { get; init; }
    }

    sealed class ReceivableRow
    {
        public string CustomerId { get; init; } = string.Empty;
        public string? CustomerName { get; init; }
        public string SellOrderId { get; init; } = string.Empty;
        public string? SellOrderCode { get; init; }
        public decimal AmountUsd { get; init; }
        public DateTime? AnchorDate { get; init; }
    }
}
