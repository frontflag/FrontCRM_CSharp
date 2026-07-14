using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

public sealed class FinanceStockAccumulatedQuery : IFinanceStockAccumulatedQuery
{
    private const int MaxPageSize = 100;
    private const short StockOutPosted = 2;
    private const short StockOutFinished = 4;

    private readonly ApplicationDbContext _db;

    public FinanceStockAccumulatedQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<FinanceStockAccumulatedSearchOptionsDto> GetSearchOptionsAsync(CancellationToken cancellationToken = default)
    {
        var years = await (
            from sin in _db.StockIns.AsNoTracking()
            where !sin.IsDeleted
                  && sin.StockInType == StockInTypeCode.Purchase
                  && sin.Status == StockInHeaderStatusCode.Posted
            select sin.StockInDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(cancellationToken);

        if (years.Count == 0)
            years.Add(DateTime.UtcNow.Year);

        return new FinanceStockAccumulatedSearchOptionsDto
        {
            Years = years.Select(y => y.ToString()).ToList()
        };
    }

    public async Task<FinanceStockAccumulatedListDto> GetStockSummaryAsync(
        int year,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        var movements = await LoadMovementsAsync(cancellationToken);
        var yearStart = FinanceAccumulatedMonthBoundary.YearStartUtc(year);
        var openingIn = movements.Inbound.Where(x => x.EffectiveDate < yearStart).ToList();
        var openingOut = movements.Outbound.Where(x => x.EffectiveDate < yearStart).ToList();

        var prvQty = openingIn.Sum(x => x.Qty) - openingOut.Sum(x => x.Qty);
        var prvAmt = openingIn.Sum(x => x.AmountUsd) - openingOut.Sum(x => x.AmountUsd);

        var rows = new List<FinanceStockAccumulatedMonthRowDto>(12);
        for (var month = 1; month <= 12; month++)
        {
            var (monthStart, monthEnd) = FinanceAccumulatedMonthBoundary.MonthRangeUtc(year, month);
            var curIn = movements.Inbound.Where(x => x.EffectiveDate >= monthStart && x.EffectiveDate < monthEnd).ToList();
            var curOut = movements.Outbound.Where(x => x.EffectiveDate >= monthStart && x.EffectiveDate < monthEnd).ToList();

            var inQty = curIn.Sum(x => x.Qty);
            var outQty = curOut.Sum(x => x.Qty);
            var inAmt = curIn.Sum(x => x.AmountUsd);
            var outAmt = curOut.Sum(x => x.AmountUsd);
            var balanceQty = prvQty + inQty - outQty;
            var balanceAmt = prvAmt + inAmt - outAmt;

            rows.Add(new FinanceStockAccumulatedMonthRowDto
            {
                YearMonth = $"{year:0000}-{month:00}",
                PrvStockQty = prvQty,
                StockInQty = inQty,
                StockOutQty = outQty,
                BalanceStockQty = balanceQty,
                PrvAmountTotal = maskAmounts ? null : RoundUsd(prvAmt),
                CurrentStockInAmountTotal = maskAmounts ? null : RoundUsd(inAmt),
                CurrentStockOutAmountTotal = maskAmounts ? null : RoundUsd(outAmt),
                BalanceAmountTotal = maskAmounts ? null : RoundUsd(balanceAmt)
            });

            prvQty = balanceQty;
            prvAmt = balanceAmt;
        }

        return new FinanceStockAccumulatedListDto
        {
            Year = year.ToString(),
            MaskAmounts = maskAmounts,
            Items = rows
        };
    }

    public async Task<PagedResult<FinanceStockAccumulatedItemRowDto>> GetStockItemPageAsync(
        FinanceStockAccumulatedItemQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        if (!FinanceAccumulatedMonthBoundary.TryParseYearMonth(request.Month, out var year, out var month))
            throw new ArgumentException("请选择月份！", nameof(request));

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (monthStart, monthEnd) = FinanceAccumulatedMonthBoundary.MonthRangeUtc(year, month);
        var filterStart = monthStart;
        var filterEnd = monthEnd;
        if (request.StockInTimeStart.HasValue)
        {
            var s = SalesAnalyticsDateFilter.ToUtcDateStart(request.StockInTimeStart.Value);
            if (s > filterStart)
                filterStart = s;
        }

        if (request.StockInTimeEnd.HasValue)
        {
            var e = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.StockInTimeEnd.Value);
            if (e < filterEnd)
                filterEnd = e;
        }

        var keywords = request.QueryKeywords?.Trim();
        var pn = request.Pn?.Trim();
        var stockInCode = request.StockInCode?.Trim();

        var baseQuery =
            from item in _db.StockInItems.AsNoTracking()
            join sin in _db.StockIns.AsNoTracking() on item.StockInId equals sin.Id
            join layer in _db.StockItems.AsNoTracking() on item.Id equals layer.StockInItemId into layers
            from layer in layers.Where(l => !l.IsDeleted).DefaultIfEmpty()
            where !item.IsDeleted
                  && !sin.IsDeleted
                  && sin.StockInType == StockInTypeCode.Purchase
                  && sin.Status == StockInHeaderStatusCode.Posted
                  && sin.StockInDate >= filterStart
                  && sin.StockInDate < filterEnd
                  && (string.IsNullOrWhiteSpace(keywords)
                      || (item.PurchasePn != null && EF.Functions.ILike(item.PurchasePn, $"%{keywords}%"))
                      || (layer != null && layer.PurchasePn != null && EF.Functions.ILike(layer.PurchasePn, $"%{keywords}%")))
                  && (string.IsNullOrWhiteSpace(pn)
                      || (item.PurchasePn != null && EF.Functions.ILike(item.PurchasePn, pn))
                      || (layer != null && layer.PurchasePn != null && EF.Functions.ILike(layer.PurchasePn, pn)))
                  && (string.IsNullOrWhiteSpace(stockInCode)
                      || (sin.StockInCode != null && EF.Functions.ILike(sin.StockInCode, $"%{stockInCode}%")))
            orderby sin.StockInDate descending, sin.StockInCode, item.StockInItemCode
            select new StockInItemSnapshot
            {
                StockInItemId = item.Id,
                BillCode = sin.StockInCode,
                Pn = item.PurchasePn ?? (layer != null ? layer.PurchasePn : null),
                StockInTime = sin.StockInDate,
                StockInQty = item.Quantity,
                UnitUsd = layer != null ? layer.PurchasePriceUsd : 0m
            };

        var total = await baseQuery.CountAsync(cancellationToken);
        var pageItems = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (pageItems.Count == 0)
        {
            return new PagedResult<FinanceStockAccumulatedItemRowDto>
            {
                Items = Array.Empty<FinanceStockAccumulatedItemRowDto>(),
                TotalCount = total,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        var itemIds = pageItems.Select(x => x.StockInItemId).ToList();
        var outbound = await LoadOutboundForItemsAsync(itemIds, cancellationToken);

        var rows = pageItems.Select(item =>
        {
            var unitUsd = item.UnitUsd;
            var currentInQty = item.StockInQty;
            var currentInAmt = currentInQty * unitUsd;

            var priorOut = outbound.Where(x =>
                    x.StockInItemId == item.StockInItemId && x.EffectiveDate < monthStart)
                .ToList();
            var currentOut = outbound.Where(x =>
                    x.StockInItemId == item.StockInItemId
                    && x.EffectiveDate >= monthStart
                    && x.EffectiveDate < monthEnd)
                .ToList();

            var priorOutQty = priorOut.Sum(x => x.Qty);
            var currentOutQty = currentOut.Sum(x => x.Qty);
            var priorOutAmt = priorOut.Sum(x => x.AmountUsd);
            var currentOutAmt = currentOut.Sum(x => x.AmountUsd);

            var prvQty = 0 - priorOutQty;
            var prvAmt = 0m - priorOutAmt;
            var balanceQty = prvQty + currentInQty - currentOutQty;
            var balanceAmt = prvAmt + currentInAmt - currentOutAmt;

            return new FinanceStockAccumulatedItemRowDto
            {
                StockInItemId = item.StockInItemId,
                BillCode = item.BillCode,
                Pn = item.Pn,
                StockInTime = item.StockInTime,
                StockInQty = currentInQty,
                StockOutQty = currentOutQty,
                PrvQty = prvQty,
                BalanceQty = balanceQty,
                PrvAmountTotal = maskAmounts ? null : RoundUsd(prvAmt),
                CurrentStockInAmountTotal = maskAmounts ? null : RoundUsd(currentInAmt),
                CurrentStockOutAmountTotal = maskAmounts ? null : RoundUsd(currentOutAmt),
                BalanceAmountTotal = maskAmounts ? null : RoundUsd(balanceAmt)
            };
        }).ToList();

        return new PagedResult<FinanceStockAccumulatedItemRowDto>
        {
            Items = rows,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    private async Task<MovementBundle> LoadMovementsAsync(CancellationToken cancellationToken)
    {
        var inbound = await (
            from item in _db.StockInItems.AsNoTracking()
            join sin in _db.StockIns.AsNoTracking() on item.StockInId equals sin.Id
            join layer in _db.StockItems.AsNoTracking() on item.Id equals layer.StockInItemId into layers
            from layer in layers.Where(l => !l.IsDeleted).DefaultIfEmpty()
            where !item.IsDeleted
                  && !sin.IsDeleted
                  && sin.StockInType == StockInTypeCode.Purchase
                  && sin.Status == StockInHeaderStatusCode.Posted
            select new MovementRow
            {
                StockInItemId = item.Id,
                EffectiveDate = sin.StockInDate,
                Qty = item.Quantity,
                UnitUsd = layer != null ? layer.PurchasePriceUsd : 0m
            }).ToListAsync(cancellationToken);

        foreach (var row in inbound)
            row.AmountUsd = row.Qty * row.UnitUsd;

        var outbound = await LoadAllOutboundAsync(cancellationToken);
        return new MovementBundle(inbound, outbound);
    }

    private async Task<IReadOnlyList<MovementRow>> LoadAllOutboundAsync(CancellationToken cancellationToken)
    {
        var raw = await (
            from ext in _db.StockOutItemExtends.AsNoTracking()
            join soi in _db.StockOutItems.AsNoTracking() on ext.Id equals soi.Id
            join so in _db.StockOuts.AsNoTracking() on soi.StockOutId equals so.Id
            where !ext.IsDeleted
                  && !soi.IsDeleted
                  && !so.IsDeleted
                  && ext.StockInItemId != null
                  && (so.Status == StockOutPosted || so.Status == StockOutFinished)
                  && (so.StockOutType == StockOutTypeCode.Sales || so.StockOutType == StockOutTypeCode.LegacySales)
            select new
            {
                StockInItemId = ext.StockInItemId!,
                EffectiveDate = so.StockOutDate ?? so.CreateTime,
                Qty = ext.QtyStockOut > 0 ? ext.QtyStockOut : (soi.ActualQty > 0 ? soi.ActualQty : soi.Quantity),
                UnitUsd = ext.PurchasePriceUsd
            }).ToListAsync(cancellationToken);

        var fallbackUnits = await BuildInboundUnitUsdLookupAsync(
            raw.Select(x => x.StockInItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            cancellationToken);

        return raw.Select(x =>
        {
            var unit = x.UnitUsd > 0m
                ? x.UnitUsd
                : fallbackUnits.GetValueOrDefault(x.StockInItemId, 0m);
            return new MovementRow
            {
                StockInItemId = x.StockInItemId,
                EffectiveDate = x.EffectiveDate,
                Qty = x.Qty,
                UnitUsd = unit,
                AmountUsd = x.Qty * unit
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<MovementRow>> LoadOutboundForItemsAsync(
        IReadOnlyList<string> stockInItemIds,
        CancellationToken cancellationToken)
    {
        if (stockInItemIds.Count == 0)
            return Array.Empty<MovementRow>();

        var raw = await (
            from ext in _db.StockOutItemExtends.AsNoTracking()
            join soi in _db.StockOutItems.AsNoTracking() on ext.Id equals soi.Id
            join so in _db.StockOuts.AsNoTracking() on soi.StockOutId equals so.Id
            where !ext.IsDeleted
                  && !soi.IsDeleted
                  && !so.IsDeleted
                  && ext.StockInItemId != null
                  && stockInItemIds.Contains(ext.StockInItemId!)
                  && (so.Status == StockOutPosted || so.Status == StockOutFinished)
                  && (so.StockOutType == StockOutTypeCode.Sales || so.StockOutType == StockOutTypeCode.LegacySales)
            select new
            {
                StockInItemId = ext.StockInItemId!,
                EffectiveDate = so.StockOutDate ?? so.CreateTime,
                Qty = ext.QtyStockOut > 0 ? ext.QtyStockOut : (soi.ActualQty > 0 ? soi.ActualQty : soi.Quantity),
                UnitUsd = ext.PurchasePriceUsd
            }).ToListAsync(cancellationToken);

        var fallbackUnits = await BuildInboundUnitUsdLookupAsync(stockInItemIds, cancellationToken);
        return raw.Select(x =>
        {
            var unit = x.UnitUsd > 0m
                ? x.UnitUsd
                : fallbackUnits.GetValueOrDefault(x.StockInItemId, 0m);
            return new MovementRow
            {
                StockInItemId = x.StockInItemId,
                EffectiveDate = x.EffectiveDate,
                Qty = x.Qty,
                UnitUsd = unit,
                AmountUsd = x.Qty * unit
            };
        }).ToList();
    }

    private async Task<Dictionary<string, decimal>> BuildInboundUnitUsdLookupAsync(
        IReadOnlyList<string> stockInItemIds,
        CancellationToken cancellationToken)
    {
        if (stockInItemIds.Count == 0)
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        var rows = await (
            from layer in _db.StockItems.AsNoTracking()
            where !layer.IsDeleted && stockInItemIds.Contains(layer.StockInItemId)
            select new { layer.StockInItemId, layer.PurchasePriceUsd })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.StockInItemId,
            x => x.PurchasePriceUsd,
            StringComparer.OrdinalIgnoreCase);
    }

    private static decimal RoundUsd(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed class MovementBundle
    {
        public MovementBundle(IReadOnlyList<MovementRow> inbound, IReadOnlyList<MovementRow> outbound)
        {
            Inbound = inbound;
            Outbound = outbound;
        }

        public IReadOnlyList<MovementRow> Inbound { get; }
        public IReadOnlyList<MovementRow> Outbound { get; }
    }

    private sealed class MovementRow
    {
        public string StockInItemId { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public int Qty { get; set; }
        public decimal UnitUsd { get; set; }
        public decimal AmountUsd { get; set; }
    }

    private sealed class StockInItemSnapshot
    {
        public string StockInItemId { get; set; } = string.Empty;
        public string BillCode { get; set; } = string.Empty;
        public string? Pn { get; set; }
        public DateTime StockInTime { get; set; }
        public int StockInQty { get; set; }
        public decimal UnitUsd { get; set; }
    }
}
