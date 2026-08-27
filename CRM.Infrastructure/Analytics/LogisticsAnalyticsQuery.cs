using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Purchase;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Analytics;

public sealed class LogisticsAnalyticsQuery : ILogisticsAnalyticsQuery
{
    private const int RankingTopN = 10;
    private const short PoItemCancelled = -2;
    /// <summary>出库单「出库完成」（列表 status=4）。</summary>
    private const short StockOutFinishedStatus = 4;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public LogisticsAnalyticsQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<LogisticsAnalyticsDashboardDto> GetDashboardAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadStockRowsAsync(scope, cancellationToken);
        var snapshot = BuildSnapshot(scope, rows);
        var todo = await BuildTodoAsync(scope, cancellationToken);
        var flow = await BuildFlowAsync(scope, cancellationToken);
        var rankings = BuildRankings(scope, rows);

        return new LogisticsAnalyticsDashboardDto
        {
            ScopeContext = scope.ScopeContext,
            Snapshot = snapshot,
            Todo = todo,
            Flow = flow,
            Rankings = rankings
        };
    }

    public async Task<IReadOnlyList<LogisticsAnalyticsTrendPointDto>> GetTrendsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var stockInRows = await LoadStockInFlowQtyRowsAsync(scope, cancellationToken);
        var stockOutRows = await LoadStockOutFlowQtyRowsAsync(scope, cancellationToken);
        var pendingByPeriod = await LoadPendingStockInByPeriodAsync(scope, cancellationToken);

        var periods = BuildPeriodKeys(scope.DateFrom, scope.TrendDateTo, scope.GroupBy);
        var result = new List<LogisticsAnalyticsTrendPointDto>();

        foreach (var period in periods)
        {
            var (start, end) = ParsePeriodRange(period, scope.GroupBy);
            pendingByPeriod.TryGetValue(period, out var pendingQty);

            result.Add(new LogisticsAnalyticsTrendPointDto
            {
                Period = period,
                StockInQty = SumQtyInRange(stockInRows, start, end),
                StockOutQty = SumQtyInRange(stockOutRows, start, end),
                PendingStockInQty = pendingQty
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>> GetBreakdownsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadStockRowsAsync(scope, cancellationToken);
        var buckets = new Dictionary<string, (string Label, int Qty)>(StringComparer.OrdinalIgnoreCase)
        {
            ["0_30"] = ("0–30天", 0),
            ["31_90"] = ("31–90天", 0),
            ["91_180"] = ("91–180天", 0),
            ["181_365"] = ("181–365天", 0),
            ["365_plus"] = ("365天以上", 0)
        };

        foreach (var row in rows)
        {
            var key = ClassifyAgeBucket(row.AgeDays);
            var b = buckets[key];
            buckets[key] = (b.Label, b.Qty + row.Qty);
        }

        var items = buckets.Select(kv => new SalesAnalyticsBreakdownItemDto
        {
            Key = kv.Key,
            Label = kv.Value.Label,
            Value = kv.Value.Qty,
            Ratio = 0
        }).ToList();
        ApplyRatios(items);

        return new List<SalesAnalyticsBreakdownGroupDto>
        {
            new() { GroupKey = "ageBucket", GroupLabel = "库龄分布（按数量）", Items = items }
        };
    }

    public async Task<LogisticsAnalyticsCustomerMatrixDto> GetCustomerMatrixAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var rows = await LoadStockRowsAsync(scope, cancellationToken);
        var subject = scope.MatrixSubject!;

        var anchorGroups = rows.GroupBy(r => ResolveCustomerAnchorKey(r)).ToList();
        var matrixRows = new List<LogisticsAnalyticsMatrixRowDto>();

        foreach (var anchor in anchorGroups.OrderByDescending(g => g.Sum(x => x.AmountUsd)))
        {
            var anchorRows = anchor.ToList();
            var (anchorId, anchorName) = ResolveCustomerAnchorDisplay(anchor.Key, anchorRows);

            var children = anchorRows
                .GroupBy(r => ResolveSubjectKey(r, subject))
                .Select(g =>
                {
                    var list = g.ToList();
                    var label = ResolveSubjectLabel(list, subject, g.Key);
                    return new LogisticsAnalyticsMatrixChildDto
                    {
                        SubjectKey = g.Key,
                        SubjectLabel = label,
                        OnHandQty = list.Sum(x => x.Qty),
                        OnHandAmountUsd = scope.MaskAmounts ? null : list.Sum(x => x.AmountUsd),
                        WeightedAvgAgeDays = WeightedAvgAge(list)
                    };
                })
                .OrderByDescending(c => c.OnHandAmountUsd ?? c.OnHandQty)
                .ToList();

            matrixRows.Add(new LogisticsAnalyticsMatrixRowDto
            {
                AnchorCustomerId = anchorId,
                AnchorCustomerName = anchorName,
                OnHandQty = anchorRows.Sum(x => x.Qty),
                OnHandAmountUsd = scope.MaskAmounts ? null : anchorRows.Sum(x => x.AmountUsd),
                WeightedAvgAgeDays = WeightedAvgAge(anchorRows),
                Children = children
            });
        }

        return new LogisticsAnalyticsCustomerMatrixDto
        {
            InventoryType = scope.InventoryType,
            MatrixSubject = subject,
            Rows = matrixRows
        };
    }

    private async Task<List<StockAnalyticsRow>> LoadStockRowsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var q = _db.StockItems.AsNoTracking()
            .Where(si => !si.IsDeleted && si.QtyRepertory > 0)
            .Where(si => si.TransferType == null || si.TransferType != StockItemTransferTypeCodes.ManualTransferSource);

        q = await _dataPermission.ApplyStockItemListDataScopeAsync(
            scope.Summary.UserId,
            q,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        q = ApplyInventoryTypeFilter(q, scope.InventoryType);

        if (!string.IsNullOrWhiteSpace(scope.WarehouseId))
            q = q.Where(si => si.WarehouseId == scope.WarehouseId);

        q = ApplyOwnershipLens(q, scope);
        if (scope.AccessMode != LogisticsAnalyticsAccessModes.SalesPurchaseOnly
            && scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            q = ApplyDepartmentLens(q, scope);
        }

        var joined = from si in q
                     join sin in _db.StockIns.AsNoTracking() on si.StockInId equals sin.Id
                     where !sin.IsDeleted
                     join oi in _db.PurchaseOrderItems.AsNoTracking() on si.PurchaseOrderItemId equals oi.Id into oiJoin
                     from oi in oiJoin.DefaultIfEmpty()
                     select new StockAnalyticsRow
                     {
                         Qty = si.QtyRepertory,
                         // 优先 PO 行 convert_price（历史成交）；否则用入库快照 PurchasePriceUsd
                         AmountUsd = oi != null && oi.ConvertPrice > 0m
                             ? si.QtyRepertory * oi.ConvertPrice
                             : si.QtyRepertory * si.PurchasePriceUsd,
                         StockInDate = sin.StockInDate,
                         CustomerId = si.CustomerId,
                         CustomerName = si.CustomerName,
                         SalespersonId = si.SalespersonId,
                         SalespersonName = si.SalespersonName,
                         VendorId = si.VendorId,
                         VendorName = si.VendorName,
                         PurchaserId = si.PurchaserId,
                         PurchaserName = si.PurchaserName,
                         Brand = si.PurchaseBrand
                     };

        var list = await joined.ToListAsync(cancellationToken);
        var asOf = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateTo).Date;
        foreach (var row in list)
            row.AgeDays = Math.Max(0, (asOf - row.StockInDate.Date).Days);

        return list;
    }

    private async Task<LogisticsAnalyticsFlowDto> BuildFlowAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var stockInAmount = await LoadStockInFlowMoneyAsync(scope, cancellationToken);
        var stockOutAmount = await LoadStockOutFlowMoneyAsync(scope, cancellationToken);
        return new LogisticsAnalyticsFlowDto
        {
            StockInAmount = stockInAmount,
            StockOutAmount = stockOutAmount
        };
    }

    private IQueryable<CRM.Core.Models.Inventory.StockItem> LoadFlowStockItems(LogisticsAnalyticsResolvedScope scope)
    {
        var q = _db.StockItems.AsNoTracking().Where(si => !si.IsDeleted);
        q = ApplyInventoryTypeFilter(q, scope.InventoryType);
        q = ApplyOwnershipLens(q, scope);
        if (scope.AccessMode != LogisticsAnalyticsAccessModes.SalesPurchaseOnly
            && scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            q = ApplyDepartmentLens(q, scope);
        }

        return q;
    }

    private async Task<SalesAnalyticsMoneyDto> LoadStockInFlowMoneyAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.TrendDateTo);

        var stockIns = _db.StockIns.AsNoTracking()
            .Where(s => !s.IsDeleted
                && s.Status == StockInHeaderStatusCode.Posted
                && (s.StockInType == StockInTypeCode.Purchase || s.StockInType == StockInTypeCode.LegacyPurchase)
                && s.StockInDate >= dateFrom
                && s.StockInDate < dateEnd);

        stockIns = await _dataPermission.ApplyStockInListDataScopeAsync(
            scope.Summary.UserId,
            stockIns,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.StockInItemExtends.AsNoTracking(),
            _db.PurchaseOrderItems.AsNoTracking(),
            _db.PurchaseOrders.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(scope.WarehouseId))
            stockIns = stockIns.Where(s => s.WarehouseId == scope.WarehouseId);

        var stockItems = LoadFlowStockItems(scope);
        var rows = await (
            from st in stockItems
            join sii in _db.StockInItems.AsNoTracking() on st.StockInItemId equals sii.Id
            join sin in stockIns on sii.StockInId equals sin.Id
            where !sii.IsDeleted
            select new FlowMoneyRow
            {
                Currency = sii.Currency ?? st.PurchaseCurrency,
                LocalAmount = sii.Quantity * (sii.Price != 0m ? sii.Price : st.PurchasePrice),
                UsdAmount = sii.Quantity * st.PurchasePriceUsd
            }).ToListAsync(cancellationToken);

        return BuildFlowMoney(rows, scope.MaskAmounts);
    }

    private async Task<SalesAnalyticsMoneyDto> LoadStockOutFlowMoneyAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.TrendDateTo);

        var stockOuts = _db.StockOuts.AsNoTracking()
            .Where(o => !o.IsDeleted
                && o.Status == StockOutFinishedStatus
                && (o.StockOutType == StockOutTypeCode.Sales || o.StockOutType == StockOutTypeCode.LegacySales)
                && o.StockOutDate != null
                && o.StockOutDate >= dateFrom
                && o.StockOutDate < dateEnd);

        stockOuts = await _dataPermission.ApplyStockOutListDataScopeAsync(
            scope.Summary.UserId,
            stockOuts,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(scope.WarehouseId))
            stockOuts = stockOuts.Where(o => o.WarehouseId == scope.WarehouseId);

        var stockItems = LoadFlowStockItems(scope);
        var rows = await (
            from item in _db.StockOutItems.AsNoTracking()
            join o in stockOuts on item.StockOutId equals o.Id
            join ext in _db.StockOutItemExtends.AsNoTracking() on item.Id equals ext.Id
            join st in stockItems on ext.StockItemId equals st.Id
            where !item.IsDeleted && !ext.IsDeleted
            select new FlowMoneyRow
            {
                Currency = ext.SalesCurrency ?? st.SalesCurrency ?? (short)CurrencyCode.RMB,
                LocalAmount = item.Quantity * (
                    ext.SalesPrice != null && ext.SalesPrice > 0m
                        ? ext.SalesPrice.Value
                        : item.Price),
                UsdAmount = item.Quantity * (ext.SalesPriceUsd ?? 0m)
            }).ToListAsync(cancellationToken);

        return BuildFlowMoney(rows, scope.MaskSalesAmounts);
    }

    private async Task<List<FlowQtyRow>> LoadStockInFlowQtyRowsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.TrendDateTo);

        var stockIns = _db.StockIns.AsNoTracking()
            .Where(s => !s.IsDeleted
                && s.Status == StockInHeaderStatusCode.Posted
                && (s.StockInType == StockInTypeCode.Purchase || s.StockInType == StockInTypeCode.LegacyPurchase)
                && s.StockInDate >= dateFrom
                && s.StockInDate < dateEnd);

        stockIns = await _dataPermission.ApplyStockInListDataScopeAsync(
            scope.Summary.UserId,
            stockIns,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.StockInItemExtends.AsNoTracking(),
            _db.PurchaseOrderItems.AsNoTracking(),
            _db.PurchaseOrders.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(scope.WarehouseId))
            stockIns = stockIns.Where(s => s.WarehouseId == scope.WarehouseId);

        var stockItems = LoadFlowStockItems(scope);
        return await (
            from st in stockItems
            join sii in _db.StockInItems.AsNoTracking() on st.StockInItemId equals sii.Id
            join sin in stockIns on sii.StockInId equals sin.Id
            where !sii.IsDeleted
            select new FlowQtyRow
            {
                OccurredOn = sin.StockInDate,
                Quantity = sii.Quantity
            }).ToListAsync(cancellationToken);
    }

    private async Task<List<FlowQtyRow>> LoadStockOutFlowQtyRowsAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.TrendDateTo);

        var stockOuts = _db.StockOuts.AsNoTracking()
            .Where(o => !o.IsDeleted
                && o.Status == StockOutFinishedStatus
                && (o.StockOutType == StockOutTypeCode.Sales || o.StockOutType == StockOutTypeCode.LegacySales)
                && o.StockOutDate != null
                && o.StockOutDate >= dateFrom
                && o.StockOutDate < dateEnd);

        stockOuts = await _dataPermission.ApplyStockOutListDataScopeAsync(
            scope.Summary.UserId,
            stockOuts,
            _db.SellOrders.AsNoTracking(),
            _db.SellOrderItems.AsNoTracking(),
            _db.Customers.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(scope.WarehouseId))
            stockOuts = stockOuts.Where(o => o.WarehouseId == scope.WarehouseId);

        var stockItems = LoadFlowStockItems(scope);
        return await (
            from item in _db.StockOutItems.AsNoTracking()
            join o in stockOuts on item.StockOutId equals o.Id
            join ext in _db.StockOutItemExtends.AsNoTracking() on item.Id equals ext.Id
            join st in stockItems on ext.StockItemId equals st.Id
            where !item.IsDeleted && !ext.IsDeleted
            select new FlowQtyRow
            {
                OccurredOn = o.StockOutDate!.Value,
                Quantity = item.Quantity
            }).ToListAsync(cancellationToken);
    }

    private static int SumQtyInRange(IReadOnlyList<FlowQtyRow> rows, DateTime start, DateTime end)
    {
        var sum = rows
            .Where(r => r.OccurredOn >= start && r.OccurredOn < end)
            .Sum(r => r.Quantity);
        return (int)Math.Round(sum, MidpointRounding.AwayFromZero);
    }

    private static SalesAnalyticsMoneyDto BuildFlowMoney(IReadOnlyList<FlowMoneyRow> rows, bool maskAmounts)
    {
        if (maskAmounts)
            return MapFlowMoney(FinanceAnalyticsMoneyBuilder.Empty(true));

        var normalized = rows.Select(r => new FinanceAnalyticsMoneyBuilder.Row
        {
            Currency = r.Currency > 0 ? r.Currency : (short)CurrencyCode.RMB,
            LocalAmount = r.LocalAmount,
            UsdAmount = r.UsdAmount
        });

        return MapFlowMoney(FinanceAnalyticsMoneyBuilder.Build(normalized, maskAmounts: false));
    }

    private static SalesAnalyticsMoneyDto MapFlowMoney(FinanceAnalyticsMoneyDto built) =>
        new()
        {
            TotalUsd = built.TotalUsd,
            ByCurrency = built.ByCurrency
                .Select(c => new SalesAnalyticsCurrencyAmountDto
                {
                    Currency = c.Currency,
                    CurrencyLabel = c.CurrencyLabel,
                    Amount = c.Amount
                })
                .ToList()
        };

    private static IQueryable<CRM.Core.Models.Inventory.StockItem> ApplyInventoryTypeFilter(
        IQueryable<CRM.Core.Models.Inventory.StockItem> q,
        string inventoryType) =>
        inventoryType switch
        {
            LogisticsAnalyticsInventoryTypes.CustomerOrder =>
                q.Where(si => si.StockType == StockInventoryTypeCodes.CustomerOrder),
            LogisticsAnalyticsInventoryTypes.PurchaseStock =>
                q.Where(si => si.StockType == StockInventoryTypeCodes.Stocking),
            _ => q
        };

    private static IQueryable<CRM.Core.Models.Inventory.StockItem> ApplyOwnershipLens(
        IQueryable<CRM.Core.Models.Inventory.StockItem> q,
        LogisticsAnalyticsResolvedScope scope)
    {
        if (scope.AccessMode == LogisticsAnalyticsAccessModes.SalesPurchaseOnly)
        {
            if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
            {
                var uid = scope.Summary.UserId;
                return q.Where(si => si.SalespersonId == uid || si.PurchaserId == uid);
            }

            if (scope.ViewLevel == SalesAnalyticsViewLevels.Department && scope.SalesPurchaseLensUserIds.Count > 0)
            {
                var ids = scope.SalesPurchaseLensUserIds.ToList();
                return q.Where(si =>
                    (si.SalespersonId != null && ids.Contains(si.SalespersonId))
                    || (si.PurchaserId != null && ids.Contains(si.PurchaserId)));
            }

            return q;
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
        {
            if (!string.IsNullOrWhiteSpace(scope.OwnerUserId))
            {
                var uid = scope.OwnerUserId.Trim();
                return q.Where(si => si.SalespersonId == uid || si.PurchaserId == uid);
            }

            if (scope.Summary.LogisticsDataScope == 1)
            {
                var uid = scope.Summary.UserId;
                return q.Where(si => si.SalespersonId == uid || si.PurchaserId == uid);
            }
        }

        return q;
    }

    private IQueryable<CRM.Core.Models.Inventory.StockItem> ApplyDepartmentLens(
        IQueryable<CRM.Core.Models.Inventory.StockItem> q,
        LogisticsAnalyticsResolvedScope scope)
    {
        var deptId = scope.DepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            deptId = scope.Summary.PrimaryDepartmentId;

        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(si =>
                (si.SalespersonId != null && !withPrimary.Contains(si.SalespersonId))
                || (si.PurchaserId != null && !withPrimary.Contains(si.PurchaserId))
                || (si.SalespersonId == null && si.PurchaserId == null));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);

        return q.Where(si =>
            (si.SalespersonId != null && userIdsInDept.Contains(si.SalespersonId))
            || (si.PurchaserId != null && userIdsInDept.Contains(si.PurchaserId)));
    }

    private static LogisticsAnalyticsSnapshotDto BuildSnapshot(
        LogisticsAnalyticsResolvedScope scope,
        IReadOnlyList<StockAnalyticsRow> rows)
    {
        var totalQty = rows.Sum(r => r.Qty);
        return new LogisticsAnalyticsSnapshotDto
        {
            InventoryType = scope.InventoryType,
            OnHandQty = totalQty,
            OnHandAmountUsd = scope.MaskAmounts ? null : rows.Sum(r => r.AmountUsd),
            WeightedAvgAgeDays = WeightedAvgAge(rows),
            SubjectCounts = new LogisticsAnalyticsSubjectCountsDto
            {
                Customer = rows.Where(r => !string.IsNullOrWhiteSpace(r.CustomerId)).Select(r => r.CustomerId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Salesperson = rows.Where(r => !string.IsNullOrWhiteSpace(r.SalespersonId)).Select(r => r.SalespersonId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Vendor = rows.Where(r => !string.IsNullOrWhiteSpace(r.VendorId)).Select(r => r.VendorId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Purchaser = rows.Where(r => !string.IsNullOrWhiteSpace(r.PurchaserId)).Select(r => r.PurchaserId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                Brand = rows.Where(r => !string.IsNullOrWhiteSpace(r.Brand)).Select(r => r.Brand!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            }
        };
    }

    private async Task<LogisticsAnalyticsTodoDto> BuildTodoAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var orders = _db.PurchaseOrders.AsNoTracking();
        orders = PurchaseAnalyticsDateFilter.ApplyAnalyticsStatusFilter(orders);
        orders = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(scope.Summary.UserId, orders, cancellationToken);
        orders = ApplyPoInventoryTypeFilter(orders, scope);

        if (scope.AccessMode == LogisticsAnalyticsAccessModes.SalesPurchaseOnly)
        {
            if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal)
            {
                var uid = scope.Summary.UserId;
                orders = orders.Where(o => o.PurchaseUserId == uid);
            }
            else if (scope.ViewLevel == SalesAnalyticsViewLevels.Department && scope.SalesPurchaseLensUserIds.Count > 0)
            {
                var ids = scope.SalesPurchaseLensUserIds.ToList();
                orders = orders.Where(o => o.PurchaseUserId != null && ids.Contains(o.PurchaseUserId));
            }
        }
        else if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal && !string.IsNullOrWhiteSpace(scope.OwnerUserId))
        {
            orders = orders.Where(o => o.PurchaseUserId == scope.OwnerUserId);
        }
        else if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
            if (!string.IsNullOrWhiteSpace(deptId))
            {
                var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
                    .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                    .Select(ud => ud.UserId);
                orders = orders.Where(o => o.PurchaseUserId != null && userIdsInDept.Contains(o.PurchaseUserId));
            }
        }

        var pending = await (
            from poi in _db.PurchaseOrderItems.AsNoTracking()
            join ext in _db.PurchaseOrderItemExtends.AsNoTracking() on poi.Id equals ext.Id
            join o in orders on poi.PurchaseOrderId equals o.Id
            where !poi.IsDeleted && !ext.IsDeleted && poi.Status != PoItemCancelled
            let remaining = poi.Qty - ext.QtyReceiveTotal
            where remaining > 0
            select remaining
        ).SumAsync(cancellationToken);

        return new LogisticsAnalyticsTodoDto
        {
            PendingStockInQty = (int)Math.Round(pending, MidpointRounding.AwayFromZero)
        };
    }

    private async Task<Dictionary<string, int>> LoadPendingStockInByPeriodAsync(
        LogisticsAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        _ = scope;
        _ = cancellationToken;
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    private static IQueryable<PurchaseOrder> ApplyPoInventoryTypeFilter(
        IQueryable<PurchaseOrder> q,
        LogisticsAnalyticsResolvedScope scope) =>
        scope.InventoryType switch
        {
            LogisticsAnalyticsInventoryTypes.CustomerOrder => q.Where(o => o.Type == StockInventoryTypeCodes.CustomerOrder),
            LogisticsAnalyticsInventoryTypes.PurchaseStock => q.Where(o => o.Type == StockInventoryTypeCodes.Stocking),
            _ => q
        };

    private static SalesAnalyticsRankingsDto BuildRankings(
        LogisticsAnalyticsResolvedScope scope,
        IReadOnlyList<StockAnalyticsRow> rows)
    {
        if (rows.Count == 0)
            return new SalesAnalyticsRankingsDto();

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Company)
        {
            var customers = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.CustomerId))
                .GroupBy(r => r.CustomerId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.First().CustomerName?.Trim() ?? g.Key,
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.AmountUsd),
                    OrderCount = g.Sum(x => x.Qty)
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();
            return new SalesAnalyticsRankingsDto { Primary = customers };
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            var salespeople = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.SalespersonId))
                .GroupBy(r => r.SalespersonId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new SalesAnalyticsRankingRowDto
                {
                    Id = g.Key,
                    Name = g.First().SalespersonName?.Trim() ?? g.Key,
                    Amount = scope.MaskAmounts ? null : g.Sum(x => x.AmountUsd),
                    OrderCount = g.Sum(x => x.Qty)
                })
                .OrderByDescending(x => x.Amount ?? x.OrderCount)
                .Take(RankingTopN)
                .ToList();
            return new SalesAnalyticsRankingsDto { Primary = salespeople };
        }

        var vendors = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.VendorId))
            .GroupBy(r => r.VendorId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new SalesAnalyticsRankingRowDto
            {
                Id = g.Key,
                Name = g.First().VendorName?.Trim() ?? g.Key,
                Amount = scope.MaskAmounts ? null : g.Sum(x => x.AmountUsd),
                OrderCount = g.Sum(x => x.Qty)
            })
            .OrderByDescending(x => x.Amount ?? x.OrderCount)
            .Take(RankingTopN)
            .ToList();
        return new SalesAnalyticsRankingsDto { Primary = vendors };
    }

    private static string ResolveCustomerAnchorKey(StockAnalyticsRow row) =>
        string.IsNullOrWhiteSpace(row.CustomerId)
            ? LogisticsAnalyticsScopeValidator.UnassignedCustomerAnchorId
            : row.CustomerId.Trim();

    private static (string? Id, string Name) ResolveCustomerAnchorDisplay(
        string anchorKey,
        IReadOnlyList<StockAnalyticsRow> rows)
    {
        if (string.Equals(anchorKey, LogisticsAnalyticsScopeValidator.UnassignedCustomerAnchorId, StringComparison.OrdinalIgnoreCase))
            return (null, LogisticsAnalyticsScopeValidator.UnassignedCustomerDisplayName);

        var name = rows.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.CustomerName))?.CustomerName?.Trim();
        return (anchorKey, name ?? anchorKey);
    }

    private static string ResolveSubjectKey(StockAnalyticsRow row, string subject) => subject switch
    {
        LogisticsAnalyticsMatrixSubjects.Salesperson =>
            string.IsNullOrWhiteSpace(row.SalespersonId) ? "__none__" : row.SalespersonId.Trim(),
        LogisticsAnalyticsMatrixSubjects.Vendor =>
            string.IsNullOrWhiteSpace(row.VendorId) ? "__none__" : row.VendorId.Trim(),
        LogisticsAnalyticsMatrixSubjects.Purchaser =>
            string.IsNullOrWhiteSpace(row.PurchaserId) ? "__none__" : row.PurchaserId.Trim(),
        LogisticsAnalyticsMatrixSubjects.Brand =>
            string.IsNullOrWhiteSpace(row.Brand) ? "__none__" : row.Brand.Trim(),
        _ => "__none__"
    };

    private static string ResolveSubjectLabel(
        IReadOnlyList<StockAnalyticsRow> rows,
        string subject,
        string key)
    {
        if (key == "__none__")
            return subject switch
            {
                LogisticsAnalyticsMatrixSubjects.Salesperson => "未分配销售员",
                LogisticsAnalyticsMatrixSubjects.Vendor => "未分配供应商",
                LogisticsAnalyticsMatrixSubjects.Purchaser => "未分配采购员",
                LogisticsAnalyticsMatrixSubjects.Brand => "未填品牌",
                _ => "未分配"
            };

        return subject switch
        {
            LogisticsAnalyticsMatrixSubjects.Salesperson =>
                rows.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.SalespersonName))?.SalespersonName?.Trim() ?? key,
            LogisticsAnalyticsMatrixSubjects.Vendor =>
                rows.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.VendorName))?.VendorName?.Trim() ?? key,
            LogisticsAnalyticsMatrixSubjects.Purchaser =>
                rows.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.PurchaserName))?.PurchaserName?.Trim() ?? key,
            LogisticsAnalyticsMatrixSubjects.Brand => key,
            _ => key
        };
    }

    private static decimal? WeightedAvgAge(IReadOnlyList<StockAnalyticsRow> rows)
    {
        var qty = rows.Sum(r => r.Qty);
        if (qty <= 0) return null;
        return Math.Round((decimal)rows.Sum(r => r.AgeDays * r.Qty) / qty, 1);
    }

    private static string ClassifyAgeBucket(int ageDays)
    {
        if (ageDays <= 30) return "0_30";
        if (ageDays <= 90) return "31_90";
        if (ageDays <= 180) return "91_180";
        if (ageDays <= 365) return "181_365";
        return "365_plus";
    }

    private static void ApplyRatios(List<SalesAnalyticsBreakdownItemDto> items)
    {
        var total = items.Sum(x => x.Value);
        if (total <= 0)
        {
            foreach (var it in items) it.Ratio = 0;
            return;
        }

        foreach (var it in items)
            it.Ratio = Math.Round(it.Value / total * 100m, 2);
    }

    private static List<string> BuildPeriodKeys(DateTime from, DateTime to, string groupBy)
    {
        var keys = new List<string>();
        var cursor = SalesAnalyticsDateFilter.ToUtcDateStart(from);
        var end = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(to);
        while (cursor < end)
        {
            keys.Add(FormatPeriod(cursor, groupBy));
            cursor = groupBy switch
            {
                "day" => cursor.AddDays(1),
                "week" => cursor.AddDays(7),
                _ => cursor.AddMonths(1)
            };
        }

        return keys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string FormatPeriod(DateTime date, string groupBy) =>
        groupBy switch
        {
            "day" => date.ToString("yyyy-MM-dd"),
            "week" => $"{date:yyyy}-W{System.Globalization.ISOWeek.GetWeekOfYear(date):D2}",
            _ => date.ToString("yyyy-MM")
        };

    private static (DateTime Start, DateTime End) ParsePeriodRange(string period, string groupBy)
    {
        if (groupBy == "day" && DateTime.TryParse(period, out var day))
        {
            var start = SalesAnalyticsDateFilter.ToUtcDateStart(day);
            return (start, start.AddDays(1));
        }

        if (groupBy == "week" && period.Contains("-W", StringComparison.Ordinal))
        {
            var parts = period.Split("-W", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && int.TryParse(parts[0], out var y) && int.TryParse(parts[1], out var w))
            {
                var start = SalesAnalyticsDateFilter.ToUtcDateStart(System.Globalization.ISOWeek.ToDateTime(y, w, DayOfWeek.Monday));
                return (start, start.AddDays(7));
            }
        }

        if (DateTime.TryParse(period + "-01", out var month))
        {
            var start = SalesAnalyticsDateFilter.ToUtcDateStart(month);
            return (start, start.AddMonths(1));
        }

        var fallback = SalesAnalyticsDateFilter.ToUtcDateStart(DateTime.UtcNow);
        return (fallback, fallback.AddMonths(1));
    }
}
