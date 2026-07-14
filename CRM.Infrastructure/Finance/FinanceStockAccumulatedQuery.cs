using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Vendor;
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
                StockInId = sin.Id,
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
                StockInId = item.StockInId,
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

    public async Task<FinanceVendorAccumulatedListDto> GetVendorPageAsync(
        FinanceVendorAccumulatedQueryRequest request,
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
        var movements = await LoadVendorMovementsAsync(cancellationToken);
        var vendorKeys = movements.Inbound.Select(x => x.VendorId)
            .Concat(movements.Outbound.Select(x => x.VendorId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<FinanceVendorAccumulatedRowDto>();
        foreach (var vendorKey in vendorKeys)
        {
            var openingIn = movements.Inbound.Where(x =>
                string.Equals(x.VendorId, vendorKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate < monthStart).ToList();
            var openingOut = movements.Outbound.Where(x =>
                string.Equals(x.VendorId, vendorKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate < monthStart).ToList();
            var curIn = movements.Inbound.Where(x =>
                string.Equals(x.VendorId, vendorKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate >= monthStart
                && x.EffectiveDate < monthEnd).ToList();
            var curOut = movements.Outbound.Where(x =>
                string.Equals(x.VendorId, vendorKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate >= monthStart
                && x.EffectiveDate < monthEnd).ToList();

            var prvQty = openingIn.Sum(x => x.Qty) - openingOut.Sum(x => x.Qty);
            var prvAmt = openingIn.Sum(x => x.AmountUsd) - openingOut.Sum(x => x.AmountUsd);
            var inQty = curIn.Sum(x => x.Qty);
            var outQty = curOut.Sum(x => x.Qty);
            var inAmt = curIn.Sum(x => x.AmountUsd);
            var outAmt = curOut.Sum(x => x.AmountUsd);
            var balanceQty = prvQty + inQty - outQty;
            var balanceAmt = prvAmt + inAmt - outAmt;

            var hasCurrentActivity = inQty != 0 || outQty != 0;
            var hasOpening = prvQty != 0 || prvAmt != 0m;
            if (!hasCurrentActivity && !hasOpening)
                continue;

            rows.Add(new FinanceVendorAccumulatedRowDto
            {
                VendorId = vendorKey,
                PrvStockQty = prvQty,
                StockInQty = inQty,
                StockOutQty = outQty,
                BalanceStockQty = balanceQty,
                PrvAmountTotal = maskAmounts ? null : RoundUsd(prvAmt),
                CurrentStockInAmountTotal = maskAmounts ? null : RoundUsd(inAmt),
                CurrentStockOutAmountTotal = maskAmounts ? null : RoundUsd(outAmt),
                BalanceAmountTotal = maskAmounts ? null : RoundUsd(balanceAmt)
            });
        }

        var keyword = request.QueryKeywords?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.ToLowerInvariant();
            var matchedVendorIds = await _db.Vendors.AsNoTracking()
                .Where(v => !v.IsDeleted
                            && ((v.OfficialName != null && v.OfficialName.ToLower().Contains(k))
                                || (v.NickName != null && v.NickName.ToLower().Contains(k))
                                || (v.EnglishOfficialName != null && v.EnglishOfficialName.ToLower().Contains(k))
                                || (v.Code != null && v.Code.ToLower().Contains(k))))
                .Select(v => v.Id)
                .ToListAsync(cancellationToken);
            var matchedSet = new HashSet<string>(matchedVendorIds, StringComparer.OrdinalIgnoreCase);
            rows = rows.Where(r => !string.IsNullOrEmpty(r.VendorId) && matchedSet.Contains(r.VendorId)).ToList();
        }

        await ApplyVendorNamesAsync(rows, cancellationToken);

        var sorted = rows
            .OrderBy(r => string.IsNullOrEmpty(r.VendorId) ? 1 : 0)
            .ThenBy(r => r.VendorName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = sorted.Count;
        var pageItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new FinanceVendorAccumulatedListDto
        {
            Month = $"{year:0000}-{month:00}",
            MaskAmounts = maskAmounts,
            Items = pageItems,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<FinanceStockAccumulatedItemRowDto>> GetVendorItemPageAsync(
        FinanceVendorAccumulatedItemQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        if (!FinanceAccumulatedMonthBoundary.TryParseYearMonth(request.Month, out var year, out var month))
            throw new ArgumentException("请选择月份！", nameof(request));

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var targetVendorId = NormalizePartyId(request.VendorId);
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
        var hasStockInDateFilter = request.StockInTimeStart.HasValue || request.StockInTimeEnd.HasValue;

        var outboundItemIdsInMonth = await GetStockInItemIdsWithVendorOutboundInMonthAsync(
            targetVendorId,
            monthStart,
            monthEnd,
            cancellationToken);

        var baseQuery =
            from item in _db.StockInItems.AsNoTracking()
            join sin in _db.StockIns.AsNoTracking() on item.StockInId equals sin.Id
            join layer in _db.StockItems.AsNoTracking() on item.Id equals layer.StockInItemId into layers
            from layer in layers.Where(l => !l.IsDeleted).DefaultIfEmpty()
            where !item.IsDeleted
                  && !sin.IsDeleted
                  && sin.StockInType == StockInTypeCode.Purchase
                  && sin.Status == StockInHeaderStatusCode.Posted
                  && (string.IsNullOrEmpty(targetVendorId)
                      ? (sin.VendorId == null || sin.VendorId == "")
                        && (layer == null || layer.VendorId == null || layer.VendorId == "")
                      : (sin.VendorId != null && sin.VendorId != "" && EF.Functions.ILike(sin.VendorId, targetVendorId))
                        || ((sin.VendorId == null || sin.VendorId == "")
                            && layer != null
                            && layer.VendorId != null
                            && layer.VendorId != ""
                            && EF.Functions.ILike(layer.VendorId, targetVendorId)))
                  && (
                      (sin.StockInDate >= filterStart && sin.StockInDate < filterEnd)
                      || (outboundItemIdsInMonth.Contains(item.Id)
                          && (!hasStockInDateFilter
                              || (sin.StockInDate >= filterStart && sin.StockInDate < filterEnd))))
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
                StockInId = sin.Id,
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
        var outbound = await LoadOutboundForVendorItemsAsync(itemIds, targetVendorId, cancellationToken);

        var rows = pageItems.Select(item =>
        {
            var unitUsd = item.UnitUsd;
            var inboundInCalendarMonth = item.StockInTime >= monthStart && item.StockInTime < monthEnd;
            var currentInQty = inboundInCalendarMonth ? item.StockInQty : 0;
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
                StockInId = item.StockInId,
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

    public async Task<FinanceCustomerAccumulatedListDto> GetCustomerPageAsync(
        FinanceCustomerAccumulatedQueryRequest request,
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
        var movements = await LoadCustomerMovementsAsync(cancellationToken);
        var customerKeys = movements.Inbound.Select(x => x.CustomerId)
            .Concat(movements.Outbound.Select(x => x.CustomerId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = new List<FinanceCustomerAccumulatedRowDto>();
        foreach (var customerKey in customerKeys)
        {
            var openingIn = movements.Inbound.Where(x =>
                string.Equals(x.CustomerId, customerKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate < monthStart).ToList();
            var openingOut = movements.Outbound.Where(x =>
                string.Equals(x.CustomerId, customerKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate < monthStart).ToList();
            var curIn = movements.Inbound.Where(x =>
                string.Equals(x.CustomerId, customerKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate >= monthStart
                && x.EffectiveDate < monthEnd).ToList();
            var curOut = movements.Outbound.Where(x =>
                string.Equals(x.CustomerId, customerKey, StringComparison.OrdinalIgnoreCase)
                && x.EffectiveDate >= monthStart
                && x.EffectiveDate < monthEnd).ToList();

            var prvQty = openingIn.Sum(x => x.Qty) - openingOut.Sum(x => x.Qty);
            var prvAmt = openingIn.Sum(x => x.AmountUsd) - openingOut.Sum(x => x.AmountUsd);
            var inQty = curIn.Sum(x => x.Qty);
            var outQty = curOut.Sum(x => x.Qty);
            var inAmt = curIn.Sum(x => x.AmountUsd);
            var outAmt = curOut.Sum(x => x.AmountUsd);
            var balanceQty = prvQty + inQty - outQty;
            var balanceAmt = prvAmt + inAmt - outAmt;

            var hasCurrentActivity = inQty != 0 || outQty != 0;
            var hasOpening = prvQty != 0 || prvAmt != 0m;
            if (!hasCurrentActivity && !hasOpening)
                continue;

            rows.Add(new FinanceCustomerAccumulatedRowDto
            {
                CustomerId = customerKey,
                PrvStockQty = prvQty,
                StockInQty = inQty,
                StockOutQty = outQty,
                BalanceStockQty = balanceQty,
                PrvAmountTotal = maskAmounts ? null : RoundUsd(prvAmt),
                CurrentStockInAmountTotal = maskAmounts ? null : RoundUsd(inAmt),
                CurrentStockOutAmountTotal = maskAmounts ? null : RoundUsd(outAmt),
                BalanceAmountTotal = maskAmounts ? null : RoundUsd(balanceAmt)
            });
        }

        var keyword = request.QueryKeywords?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.ToLowerInvariant();
            var matchedCustomerIds = await _db.Customers.AsNoTracking()
                .Where(c => !c.IsDeleted
                            && ((c.OfficialName != null && c.OfficialName.ToLower().Contains(k))
                                || (c.NickName != null && c.NickName.ToLower().Contains(k))
                                || (c.EnglishOfficialName != null && c.EnglishOfficialName.ToLower().Contains(k))
                                || (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(k))))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            var matchedSet = new HashSet<string>(matchedCustomerIds, StringComparer.OrdinalIgnoreCase);
            rows = rows.Where(r => !string.IsNullOrEmpty(r.CustomerId) && matchedSet.Contains(r.CustomerId)).ToList();
        }

        await ApplyCustomerNamesAsync(rows, cancellationToken);

        var sorted = rows
            .OrderBy(r => string.IsNullOrEmpty(r.CustomerId) ? 1 : 0)
            .ThenBy(r => r.CustomerName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = sorted.Count;
        var pageItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new FinanceCustomerAccumulatedListDto
        {
            Month = $"{year:0000}-{month:00}",
            MaskAmounts = maskAmounts,
            Items = pageItems,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<FinanceStockAccumulatedItemRowDto>> GetCustomerItemPageAsync(
        FinanceCustomerAccumulatedItemQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default)
    {
        if (!FinanceAccumulatedMonthBoundary.TryParseYearMonth(request.Month, out var year, out var month))
            throw new ArgumentException("请选择月份！", nameof(request));

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var targetCustomerId = NormalizePartyId(request.CustomerId);
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
        var hasStockInDateFilter = request.StockInTimeStart.HasValue || request.StockInTimeEnd.HasValue;

        var outboundItemIdsInMonth = await GetStockInItemIdsWithCustomerOutboundInMonthAsync(
            targetCustomerId,
            monthStart,
            monthEnd,
            cancellationToken);

        var baseQuery =
            from item in _db.StockInItems.AsNoTracking()
            join sin in _db.StockIns.AsNoTracking() on item.StockInId equals sin.Id
            join layer in _db.StockItems.AsNoTracking() on item.Id equals layer.StockInItemId into layers
            from layer in layers.Where(l => !l.IsDeleted).DefaultIfEmpty()
            where !item.IsDeleted
                  && !sin.IsDeleted
                  && sin.StockInType == StockInTypeCode.Purchase
                  && sin.Status == StockInHeaderStatusCode.Posted
                  && (string.IsNullOrEmpty(targetCustomerId)
                      ? layer == null || layer.CustomerId == null || layer.CustomerId == ""
                      : layer != null
                        && layer.CustomerId != null
                        && layer.CustomerId != ""
                        && EF.Functions.ILike(layer.CustomerId, targetCustomerId))
                  && (
                      (sin.StockInDate >= filterStart && sin.StockInDate < filterEnd)
                      || (outboundItemIdsInMonth.Contains(item.Id)
                          && (!hasStockInDateFilter
                              || (sin.StockInDate >= filterStart && sin.StockInDate < filterEnd))))
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
                StockInId = sin.Id,
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
        var outbound = await LoadOutboundForCustomerItemsAsync(itemIds, targetCustomerId, cancellationToken);

        var rows = pageItems.Select(item =>
        {
            var unitUsd = item.UnitUsd;
            var inboundInCalendarMonth = item.StockInTime >= monthStart && item.StockInTime < monthEnd;
            var currentInQty = inboundInCalendarMonth ? item.StockInQty : 0;
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
                StockInId = item.StockInId,
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

    private async Task<MovementBundle> LoadVendorMovementsAsync(CancellationToken cancellationToken)
    {
        var inbound = await LoadInboundWithVendorAsync(cancellationToken);
        var outbound = await LoadOutboundWithVendorAsync(cancellationToken);
        return new MovementBundle(inbound, outbound);
    }

    private async Task<MovementBundle> LoadCustomerMovementsAsync(CancellationToken cancellationToken)
    {
        var inbound = await LoadInboundWithCustomerAsync(cancellationToken);
        var outbound = await LoadOutboundWithCustomerAsync(cancellationToken);
        return new MovementBundle(inbound, outbound);
    }

    private async Task<IReadOnlyList<MovementRow>> LoadInboundWithCustomerAsync(CancellationToken cancellationToken)
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
            select new
            {
                StockInItemId = item.Id,
                EffectiveDate = sin.StockInDate,
                Qty = item.Quantity,
                UnitUsd = layer != null ? layer.PurchasePriceUsd : 0m,
                CustomerId = layer != null ? layer.CustomerId : null
            }).ToListAsync(cancellationToken);

        return inbound.Select(x => new MovementRow
        {
            StockInItemId = x.StockInItemId,
            EffectiveDate = x.EffectiveDate,
            Qty = x.Qty,
            UnitUsd = x.UnitUsd,
            AmountUsd = x.Qty * x.UnitUsd,
            CustomerId = NormalizePartyId(x.CustomerId)
        }).ToList();
    }

    private async Task<IReadOnlyList<MovementRow>> LoadOutboundWithCustomerAsync(CancellationToken cancellationToken)
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
                UnitUsd = ext.PurchasePriceUsd,
                HeaderCustomerId = so.CustomerId
            }).ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return Array.Empty<MovementRow>();

        var itemIds = raw.Select(x => x.StockInItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var inboundCustomerLookup = await BuildInboundCustomerLookupAsync(itemIds, cancellationToken);
        var fallbackUnits = await BuildInboundUnitUsdLookupAsync(itemIds, cancellationToken);

        var result = new List<MovementRow>(raw.Count);
        foreach (var x in raw)
        {
            var inboundCustomer = inboundCustomerLookup.GetValueOrDefault(x.StockInItemId, string.Empty);
            var outboundCustomer = NormalizePartyId(x.HeaderCustomerId);
            if (!string.Equals(outboundCustomer, inboundCustomer, StringComparison.OrdinalIgnoreCase))
                continue;

            var unit = x.UnitUsd > 0m
                ? x.UnitUsd
                : fallbackUnits.GetValueOrDefault(x.StockInItemId, 0m);
            result.Add(new MovementRow
            {
                StockInItemId = x.StockInItemId,
                EffectiveDate = x.EffectiveDate,
                Qty = x.Qty,
                UnitUsd = unit,
                AmountUsd = x.Qty * unit,
                CustomerId = inboundCustomer
            });
        }

        return result;
    }

    private async Task<IReadOnlyList<MovementRow>> LoadInboundWithVendorAsync(CancellationToken cancellationToken)
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
            select new
            {
                StockInItemId = item.Id,
                EffectiveDate = sin.StockInDate,
                Qty = item.Quantity,
                UnitUsd = layer != null ? layer.PurchasePriceUsd : 0m,
                VendorId = sin.VendorId ?? (layer != null ? layer.VendorId : null)
            }).ToListAsync(cancellationToken);

        return inbound.Select(x => new MovementRow
        {
            StockInItemId = x.StockInItemId,
            EffectiveDate = x.EffectiveDate,
            Qty = x.Qty,
            UnitUsd = x.UnitUsd,
            AmountUsd = x.Qty * x.UnitUsd,
            VendorId = NormalizePartyId(x.VendorId)
        }).ToList();
    }

    private async Task<IReadOnlyList<MovementRow>> LoadOutboundWithVendorAsync(CancellationToken cancellationToken)
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
                UnitUsd = ext.PurchasePriceUsd,
                ExtVendorId = ext.VendorId
            }).ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return Array.Empty<MovementRow>();

        var itemIds = raw.Select(x => x.StockInItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var inboundVendorLookup = await BuildInboundVendorLookupAsync(itemIds, cancellationToken);
        var fallbackUnits = await BuildInboundUnitUsdLookupAsync(itemIds, cancellationToken);

        var result = new List<MovementRow>(raw.Count);
        foreach (var x in raw)
        {
            var inboundVendor = inboundVendorLookup.GetValueOrDefault(x.StockInItemId, string.Empty);
            var outboundVendor = NormalizePartyId(
                !string.IsNullOrWhiteSpace(x.ExtVendorId) ? x.ExtVendorId : inboundVendor);
            if (!string.Equals(outboundVendor, inboundVendor, StringComparison.OrdinalIgnoreCase))
                continue;

            var unit = x.UnitUsd > 0m
                ? x.UnitUsd
                : fallbackUnits.GetValueOrDefault(x.StockInItemId, 0m);
            result.Add(new MovementRow
            {
                StockInItemId = x.StockInItemId,
                EffectiveDate = x.EffectiveDate,
                Qty = x.Qty,
                UnitUsd = unit,
                AmountUsd = x.Qty * unit,
                VendorId = inboundVendor
            });
        }

        return result;
    }

    private async Task<Dictionary<string, string>> BuildInboundVendorLookupAsync(
        IReadOnlyList<string> stockInItemIds,
        CancellationToken cancellationToken)
    {
        if (stockInItemIds.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var rows = await (
            from item in _db.StockInItems.AsNoTracking()
            join sin in _db.StockIns.AsNoTracking() on item.StockInId equals sin.Id
            join layer in _db.StockItems.AsNoTracking() on item.Id equals layer.StockInItemId into layers
            from layer in layers.Where(l => !l.IsDeleted).DefaultIfEmpty()
            where !item.IsDeleted
                  && !sin.IsDeleted
                  && stockInItemIds.Contains(item.Id)
            select new
            {
                item.Id,
                VendorId = sin.VendorId ?? (layer != null ? layer.VendorId : null)
            }).ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.Id,
            x => NormalizePartyId(x.VendorId),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, string>> BuildInboundCustomerLookupAsync(
        IReadOnlyList<string> stockInItemIds,
        CancellationToken cancellationToken)
    {
        if (stockInItemIds.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var rows = await (
            from item in _db.StockInItems.AsNoTracking()
            join layer in _db.StockItems.AsNoTracking() on item.Id equals layer.StockInItemId into layers
            from layer in layers.Where(l => !l.IsDeleted).DefaultIfEmpty()
            where !item.IsDeleted
                  && stockInItemIds.Contains(item.Id)
            select new
            {
                item.Id,
                CustomerId = layer != null ? layer.CustomerId : null
            }).ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.Id,
            x => NormalizePartyId(x.CustomerId),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task ApplyVendorNamesAsync(
        IReadOnlyList<FinanceVendorAccumulatedRowDto> rows,
        CancellationToken cancellationToken)
    {
        var vendorIds = rows
            .Select(r => r.VendorId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (vendorIds.Count == 0)
            return;

        var vendors = await _db.Vendors.AsNoTracking()
            .Where(v => !v.IsDeleted && vendorIds.Contains(v.Id))
            .ToListAsync(cancellationToken);
        var nameMap = vendors.ToDictionary(
            v => v.Id,
            FormatVendorDisplayName,
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.VendorId))
                continue;
            row.VendorName = nameMap.GetValueOrDefault(row.VendorId);
        }
    }

    private async Task ApplyCustomerNamesAsync(
        IReadOnlyList<FinanceCustomerAccumulatedRowDto> rows,
        CancellationToken cancellationToken)
    {
        var customerIds = rows
            .Select(r => r.CustomerId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (customerIds.Count == 0)
            return;

        var customers = await _db.Customers.AsNoTracking()
            .Where(c => !c.IsDeleted && customerIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        var nameMap = customers.ToDictionary(
            c => c.Id,
            FormatCustomerDisplayName,
            StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.CustomerId))
                continue;
            row.CustomerName = nameMap.GetValueOrDefault(row.CustomerId);
        }
    }

    private async Task<List<string>> GetStockInItemIdsWithVendorOutboundInMonthAsync(
        string targetVendorId,
        DateTime monthStart,
        DateTime monthEnd,
        CancellationToken cancellationToken)
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
                  && (so.StockOutDate ?? so.CreateTime) >= monthStart
                  && (so.StockOutDate ?? so.CreateTime) < monthEnd
            select new
            {
                StockInItemId = ext.StockInItemId!,
                ExtVendorId = ext.VendorId
            }).ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return [];

        var itemIds = raw.Select(x => x.StockInItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var inboundVendorLookup = await BuildInboundVendorLookupAsync(itemIds, cancellationToken);

        return raw
            .Where(x =>
            {
                var inboundVendor = inboundVendorLookup.GetValueOrDefault(x.StockInItemId, string.Empty);
                var outboundVendor = NormalizePartyId(
                    !string.IsNullOrWhiteSpace(x.ExtVendorId) ? x.ExtVendorId : inboundVendor);
                if (!string.Equals(outboundVendor, inboundVendor, StringComparison.OrdinalIgnoreCase))
                    return false;
                return string.Equals(inboundVendor, targetVendorId, StringComparison.OrdinalIgnoreCase);
            })
            .Select(x => x.StockInItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<string>> GetStockInItemIdsWithCustomerOutboundInMonthAsync(
        string targetCustomerId,
        DateTime monthStart,
        DateTime monthEnd,
        CancellationToken cancellationToken)
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
                  && (so.StockOutDate ?? so.CreateTime) >= monthStart
                  && (so.StockOutDate ?? so.CreateTime) < monthEnd
            select new
            {
                StockInItemId = ext.StockInItemId!,
                HeaderCustomerId = so.CustomerId
            }).ToListAsync(cancellationToken);

        if (raw.Count == 0)
            return [];

        var itemIds = raw.Select(x => x.StockInItemId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var inboundCustomerLookup = await BuildInboundCustomerLookupAsync(itemIds, cancellationToken);

        return raw
            .Where(x =>
            {
                var inboundCustomer = inboundCustomerLookup.GetValueOrDefault(x.StockInItemId, string.Empty);
                var outboundCustomer = NormalizePartyId(x.HeaderCustomerId);
                if (!string.Equals(outboundCustomer, inboundCustomer, StringComparison.OrdinalIgnoreCase))
                    return false;
                return string.Equals(inboundCustomer, targetCustomerId, StringComparison.OrdinalIgnoreCase);
            })
            .Select(x => x.StockInItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveInboundVendorKey(string? sinVendorId, string? layerVendorId) =>
        NormalizePartyId(!string.IsNullOrWhiteSpace(sinVendorId) ? sinVendorId : layerVendorId);

    private static string NormalizePartyId(string? id) =>
        string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();

    private static string? FormatVendorDisplayName(VendorInfo vendor)
    {
        if (!string.IsNullOrWhiteSpace(vendor.OfficialName))
            return vendor.OfficialName.Trim();
        if (!string.IsNullOrWhiteSpace(vendor.NickName))
            return vendor.NickName.Trim();
        if (!string.IsNullOrWhiteSpace(vendor.EnglishOfficialName))
            return vendor.EnglishOfficialName.Trim();
        return string.IsNullOrWhiteSpace(vendor.Code) ? null : vendor.Code.Trim();
    }

    private static string? FormatCustomerDisplayName(CustomerInfo customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.OfficialName))
            return customer.OfficialName.Trim();
        if (!string.IsNullOrWhiteSpace(customer.NickName))
            return customer.NickName.Trim();
        if (!string.IsNullOrWhiteSpace(customer.EnglishOfficialName))
            return customer.EnglishOfficialName.Trim();
        return string.IsNullOrWhiteSpace(customer.CustomerCode) ? null : customer.CustomerCode.Trim();
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

    private async Task<IReadOnlyList<MovementRow>> LoadOutboundForVendorItemsAsync(
        IReadOnlyList<string> stockInItemIds,
        string targetVendorId,
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
                UnitUsd = ext.PurchasePriceUsd,
                ExtVendorId = ext.VendorId
            }).ToListAsync(cancellationToken);

        var inboundVendorLookup = await BuildInboundVendorLookupAsync(stockInItemIds, cancellationToken);
        var fallbackUnits = await BuildInboundUnitUsdLookupAsync(stockInItemIds, cancellationToken);

        var result = new List<MovementRow>(raw.Count);
        foreach (var x in raw)
        {
            var inboundVendor = inboundVendorLookup.GetValueOrDefault(x.StockInItemId, string.Empty);
            var outboundVendor = NormalizePartyId(
                !string.IsNullOrWhiteSpace(x.ExtVendorId) ? x.ExtVendorId : inboundVendor);
            if (!string.Equals(outboundVendor, inboundVendor, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(inboundVendor, targetVendorId, StringComparison.OrdinalIgnoreCase))
                continue;

            var unit = x.UnitUsd > 0m
                ? x.UnitUsd
                : fallbackUnits.GetValueOrDefault(x.StockInItemId, 0m);
            result.Add(new MovementRow
            {
                StockInItemId = x.StockInItemId,
                EffectiveDate = x.EffectiveDate,
                Qty = x.Qty,
                UnitUsd = unit,
                AmountUsd = x.Qty * unit,
                VendorId = inboundVendor
            });
        }

        return result;
    }

    private async Task<IReadOnlyList<MovementRow>> LoadOutboundForCustomerItemsAsync(
        IReadOnlyList<string> stockInItemIds,
        string targetCustomerId,
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
                UnitUsd = ext.PurchasePriceUsd,
                HeaderCustomerId = so.CustomerId
            }).ToListAsync(cancellationToken);

        var inboundCustomerLookup = await BuildInboundCustomerLookupAsync(stockInItemIds, cancellationToken);
        var fallbackUnits = await BuildInboundUnitUsdLookupAsync(stockInItemIds, cancellationToken);

        var result = new List<MovementRow>(raw.Count);
        foreach (var x in raw)
        {
            var inboundCustomer = inboundCustomerLookup.GetValueOrDefault(x.StockInItemId, string.Empty);
            var outboundCustomer = NormalizePartyId(x.HeaderCustomerId);
            if (!string.Equals(outboundCustomer, inboundCustomer, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(inboundCustomer, targetCustomerId, StringComparison.OrdinalIgnoreCase))
                continue;

            var unit = x.UnitUsd > 0m
                ? x.UnitUsd
                : fallbackUnits.GetValueOrDefault(x.StockInItemId, 0m);
            result.Add(new MovementRow
            {
                StockInItemId = x.StockInItemId,
                EffectiveDate = x.EffectiveDate,
                Qty = x.Qty,
                UnitUsd = unit,
                AmountUsd = x.Qty * unit,
                CustomerId = inboundCustomer
            });
        }

        return result;
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
        public string VendorId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
    }

    private sealed class StockInItemSnapshot
    {
        public string StockInItemId { get; set; } = string.Empty;
        public string StockInId { get; set; } = string.Empty;
        public string BillCode { get; set; } = string.Empty;
        public string? Pn { get; set; }
        public DateTime StockInTime { get; set; }
        public int StockInQty { get; set; }
        public decimal UnitUsd { get; set; }
    }
}
