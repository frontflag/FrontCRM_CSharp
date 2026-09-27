using System.Globalization;
using CRM.Core.Ai;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;

namespace CRM.Infrastructure.AiAssistant;

public sealed class AiDataQueryService : IAiDataQueryService
{
    private static readonly CultureInfo Zh = CultureInfo.GetCultureInfo("zh-CN");

    private readonly IRbacService _rbac;
    private readonly ISalesAnalyticsService _salesAnalytics;
    private readonly ISalesAnalyticsQuery _salesQuery;
    private readonly IStockOutItemListAnalyticsQuery _stockOutAnalytics;
    private readonly ISalesOrderItemLineListQuery _orderItemAnalytics;
    private readonly IInventoryOnHandListAnalyticsQuery _inventoryAnalytics;
    private readonly IFinanceReceivableListQuery _receivableAnalytics;

    public AiDataQueryService(
        IRbacService rbac,
        ISalesAnalyticsService salesAnalytics,
        ISalesAnalyticsQuery salesQuery,
        IStockOutItemListAnalyticsQuery stockOutAnalytics,
        ISalesOrderItemLineListQuery orderItemAnalytics,
        IInventoryOnHandListAnalyticsQuery inventoryAnalytics,
        IFinanceReceivableListQuery receivableAnalytics)
    {
        _rbac = rbac;
        _salesAnalytics = salesAnalytics;
        _salesQuery = salesQuery;
        _stockOutAnalytics = stockOutAnalytics;
        _orderItemAnalytics = orderItemAnalytics;
        _inventoryAnalytics = inventoryAnalytics;
        _receivableAnalytics = receivableAnalytics;
    }

    public async Task<AiDataQueryResponse> AskAsync(
        string userId,
        string question,
        AiDataQueryState? state,
        CancellationToken cancellationToken = default)
    {
        var plan = AiDataQueryPlanner.Plan(question, state);
        if (plan.Kind != AiDataQueryKinds.Result)
        {
            return new AiDataQueryResponse
            {
                Kind = plan.Kind,
                Summary = plan.Summary,
                State = plan.State,
                Options = plan.Options
            };
        }

        var summary = await _rbac.GetUserPermissionSummaryAsync(userId);
        return plan.State.Intent switch
        {
            AiDataQueryIntents.Performance => await PerformanceAsync(userId, summary, plan.State, cancellationToken),
            AiDataQueryIntents.SalesTrend => await SalesTrendAsync(userId, summary, plan.State, cancellationToken),
            AiDataQueryIntents.OrderTrend => await OrderTrendAsync(userId, summary, plan.State, cancellationToken),
            AiDataQueryIntents.TopBrands => await TopBrandsAsync(userId, summary, plan.State, cancellationToken),
            AiDataQueryIntents.InventoryAmount => await InventoryAmountAsync(userId, summary, plan.State, cancellationToken),
            AiDataQueryIntents.Backlog => await BacklogAsync(userId, summary, plan.State, cancellationToken),
            AiDataQueryIntents.ReceivableTotal => await ReceivableTotalAsync(userId, summary, plan.State, cancellationToken),
            AiDataQueryIntents.ReceivableCustomers => await ReceivableCustomersAsync(userId, summary, plan.State, cancellationToken),
            _ => Unsupported(plan.State)
        };
    }

    private async Task<AiDataQueryResponse> PerformanceAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        var (from, to, periodLabel) = PeriodRange(state.Period);
        if (state.Metric == "stockOutAmount")
        {
            var dash = await _stockOutAnalytics.GetDashboardAsync(
                StockOutQuery(userId, from, to),
                MaskSalesAmounts(summary),
                cancellationToken);
            var amount = dash.Snapshot.AmountUsd;
            return NumberResult(
                state,
                amount == null
                    ? $"{periodLabel}已完成销售出库金额当前账号看不到。"
                    : $"{periodLabel}已完成销售出库金额为 {Money(amount)} 美元。",
                "口径与出库明细看板一致：只计已完成的销售出库，折算美金优先出库过账快照。",
                amount,
                "美元",
                Link("查看出库明细", "StockOutItemList"));
        }

        var scope = await ResolveSalesAsync(userId, from, to, "month", cancellationToken);
        if (scope.Error != null)
            return Denied(state, scope.Error);
        var dashboard = await _salesQuery.GetDashboardAsync(scope.Scope!, cancellationToken);
        var orderMasked = dashboard.ScopeContext.MaskAmounts;
        decimal? orderAmount = orderMasked ? null : (dashboard.Snapshot.SalesAmountApproved ?? 0m);
        if (state.Metric is "orderCount" or "orderCountAndAmount")
            return NewOrderResult(state, periodLabel, dashboard.Snapshot.SalesOrderCount, orderAmount, state.Metric == "orderCountAndAmount");
        return NumberResult(
            state,
            orderAmount == null
                ? $"{periodLabel}已审核订单金额当前账号看不到。"
                : $"{periodLabel}已审核订单金额为 {Money(orderAmount)} 美元。",
            "口径与销售分析的已审核订单金额一致，按订单创建时间。",
            orderAmount,
            "美元",
            Link("查看销售分析", "SalesAnalytics"));
    }

    private static AiDataQueryResponse NewOrderResult(
        AiDataQueryState state,
        string periodLabel,
        int count,
        decimal? amount,
        bool includeAmount)
    {
        var countText = count.ToString("N0", Zh);
        if (!includeAmount)
        {
            return NumberResult(
                state,
                $"{periodLabel}新建销售订单 {countText} 张。",
                "口径与销售分析一致：按订单创建时间，不含取消和审核失败。",
                count,
                "张",
                Link("查看销售分析", "SalesAnalytics"));
        }

        var summary = amount == null
            ? $"{periodLabel}新建销售订单 {countText} 张。已审核金额当前账号看不到。"
            : $"{periodLabel}新建销售订单 {countText} 张，已审核金额 {Money(amount)} 美元。";
        return new AiDataQueryResponse
        {
            Kind = AiDataQueryKinds.Result,
            Summary = summary,
            BasisNote = "口径与销售分析一致：按订单创建时间，不含取消和审核失败。金额是已审核订单的折算美金。",
            State = state,
            Rows = new[]
            {
                Row("订单张数", countText),
                Row("已审核金额", amount == null ? null : $"{Money(amount)} 美元")
            },
            Link = Link("查看销售分析", "SalesAnalytics")
        };
    }

    private async Task<AiDataQueryResponse> SalesTrendAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        var (from, to, periodLabel) = PeriodRange(state.Period);
        var groupBy = GroupBy(from, to);
        if (state.Metric == "stockOutAmount")
        {
            var points = await _stockOutAnalytics.GetTrendsAsync(
                StockOutQuery(userId, from, to),
                groupBy,
                MaskSalesAmounts(summary),
                cancellationToken);
            var chartPoints = points.Select(p => new AiDataQueryPointDto
            {
                Label = p.Period,
                Value = p.AmountUsd
            }).ToList();
            var masked = chartPoints.All(p => p.Value == null) && MaskSalesAmounts(summary);
            return ChartResult(
                state,
                masked
                    ? $"{periodLabel}销售出库金额趋势当前账号看不到。"
                    : $"{periodLabel}已完成销售出库金额趋势如下。",
                "口径与出库明细看板一致：只计已完成的销售出库，折算美金优先出库过账快照。",
                "line",
                "美元",
                chartPoints,
                Link("查看出库明细", "StockOutItemList"));
        }

        var scope = await ResolveSalesAsync(userId, from, to, groupBy, cancellationToken);
        if (scope.Error != null)
            return Denied(state, scope.Error);
        var trends = await _salesQuery.GetTrendsAsync(scope.Scope!, cancellationToken);
        var maskedSales = scope.Scope!.MaskAmounts;
        var useCount = state.Metric == "orderCount";
        var series = trends.Select(p => new AiDataQueryPointDto
        {
            Label = p.Period,
            Value = useCount
                ? p.SalesOrderItemCount
                : (maskedSales ? null : p.SalesAmountApproved)
        }).ToList();
        return ChartResult(
            state,
            useCount
                ? $"{periodLabel}销售订单明细行数趋势如下。"
                : maskedSales
                    ? $"{periodLabel}订单金额趋势当前账号看不到。"
                    : $"{periodLabel}已审核订单金额趋势如下。",
            useCount
                ? "口径与销售分析趋势的销售订单明细行数一致，按订单创建时间。"
                : "口径与销售分析趋势的已审核订单金额一致，按订单创建时间。",
            "line",
            useCount ? "行" : "美元",
            series,
            Link("查看销售分析", "SalesAnalytics"));
    }

    private async Task<AiDataQueryResponse> OrderTrendAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        state.Metric = state.Metric == "orderCount" ? "orderCount" : "orderAmount";
        return await SalesTrendAsync(userId, summary, state, cancellationToken);
    }

    private async Task<AiDataQueryResponse> TopBrandsAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        var (from, to, periodLabel) = PeriodRange(state.Period);
        var top = Math.Clamp(state.TopN ?? 10, 1, 10);
        var byQty = state.Measure == "qty";
        var mask = MaskSalesAmounts(summary);
        if (state.Basis == "stockOut")
        {
            var rankings = await _stockOutAnalytics.GetRankingsAsync(
                StockOutQuery(userId, from, to),
                mask,
                cancellationToken);
            var rows = (byQty ? rankings.BrandByQty : rankings.BrandByAmount).Take(top).ToList();
            return RankResult(
                state,
                mask && !byQty
                    ? $"{periodLabel}出库品牌金额排行当前账号看不到。"
                    : $"{periodLabel}已完成销售出库里，{(byQty ? "数量" : "金额")}最高的 {rows.Count} 个品牌如下。",
                "口径与出库明细看板一致：只计已完成的销售出库。金额是折算美金，排行最多 10 名。",
                rows.Select(r => Point(r.Name, byQty ? r.OrderCount : r.Amount)).ToList(),
                byQty ? "件" : "美元",
                rows.Select(r => Row(r.Name, byQty ? Qty(r.OrderCount) : MoneyOrBlank(r.Amount, mask))).ToList(),
                Link("查看出库明细", "StockOutItemList"));
        }

        var request = new SellOrderItemLineQueryRequest
        {
            OrderCreateStart = from,
            OrderCreateEnd = to,
            CurrentUserId = userId
        };
        var orderRank = await _orderItemAnalytics.GetListAnalyticsRankingsAsync(request, mask, cancellationToken);
        var orderRows = (byQty ? orderRank.BrandByQty : orderRank.BrandByAmount).Take(top).ToList();
        return RankResult(
            state,
            mask && !byQty
                ? $"{periodLabel}下单品牌金额排行当前账号看不到。"
                : $"{periodLabel}已审核销售订单里，{(byQty ? "数量" : "金额")}最高的 {orderRows.Count} 个品牌如下。",
            "口径与销售订单明细看板的品牌排行一致，按订单创建时间。排行最多 10 名。",
            orderRows.Select(r => Point(r.Name, byQty ? r.OrderCount : r.Amount)).ToList(),
            byQty ? "件" : "美元",
            orderRows.Select(r => Row(r.Name, byQty ? Qty(r.OrderCount) : MoneyOrBlank(r.Amount, mask))).ToList(),
            Link("查看销售订单明细", "SalesOrderItemList"));
    }

    private async Task<AiDataQueryResponse> InventoryAmountAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        var mask = MaskPurchaseAmounts(summary);
        var total = await _inventoryAnalytics.GetConvertedUsdTotalAsync(
            new InventoryOnHandSummaryQueryRequest { CurrentUserId = userId },
            mask,
            cancellationToken);
        return NumberResult(
            state,
            total == null
                ? "当前在库金额当前账号看不到。"
                : $"当前在库金额为 {Money(total)} 美元。",
            "口径与库存中心在库折算美金一致：优先采购行折算单价，否则用入库时写入的美金采购价。",
            total,
            "美元",
            Link("查看在库", "InventoryStockItemList"));
    }

    private async Task<AiDataQueryResponse> BacklogAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        var days = state.AgeDays ?? 0;
        var byQty = state.Measure == "qty";
        var mask = MaskPurchaseAmounts(summary);
        var rankings = await _inventoryAnalytics.GetAgedRankingsAsync(
            new InventoryOnHandSummaryQueryRequest { CurrentUserId = userId },
            days,
            mask,
            cancellationToken);
        var parts = new List<(string Title, IReadOnlyList<SalesAnalyticsRankingRowDto> Rows)>();
        if (state.Dimension is "brand" or "both")
        {
            parts.Add(("品牌", (byQty ? rankings.BrandByQty : UsdRows(rankings.BrandByAmount)).Take(10).ToList()));
        }
        if (state.Dimension is "customer" or "both")
        {
            parts.Add(("客户", (byQty ? rankings.CustomerByQty : UsdRows(rankings.CustomerByAmount)).Take(10).ToList()));
        }

        var points = new List<AiDataQueryPointDto>();
        var table = new List<AiDataQueryRowDto>();
        foreach (var part in parts)
        {
            foreach (var row in part.Rows)
            {
                if (table.Count >= 20) break;
                var prefix = parts.Count > 1 ? $"{part.Title} " : string.Empty;
                points.Add(Point(prefix + row.Name, byQty ? row.OrderCount : row.Amount));
                table.Add(Row(prefix + row.Name, byQty ? Qty(row.OrderCount) : MoneyOrBlank(row.Amount, mask)));
            }
        }

        var what = byQty ? "数量" : "金额";
        var who = state.Dimension switch
        {
            "customer" => "客户",
            "both" => "品牌和客户",
            _ => "品牌"
        };
        return RankResult(
            state,
            mask && !byQty
                ? $"库龄超过 {days} 天的在库{what}当前账号看不到。"
                : $"库龄超过 {days} 天的在库里，{what}最高的{who}如下。没有入库日的不计入。",
            "口径与库存中心在库一致。金额是折算美金。排行最多 10 名。",
            points,
            byQty ? "件" : "美元",
            table,
            Link("查看在库", "InventoryStockItemList"));
    }

    private async Task<AiDataQueryResponse> ReceivableTotalAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        var today = CompanyCalendarDate.ToCompanyDate(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
        var scope = await ResolveSalesAsync(userId, today.AddMonths(-5), today, "month", cancellationToken);
        if (scope.Error != null)
            return Denied(state, scope.Error);
        var dashboard = await _salesQuery.GetDashboardAsync(scope.Scope!, cancellationToken);
        var money = dashboard.Todo.ReceivableAmount;
        var masked = scope.Scope!.MaskAmounts || money.TotalUsd == null;
        var bands = masked
            ? string.Empty
            : string.Join("，", money.ByCurrency.Select(c => $"{c.CurrencyLabel} {c.Amount.ToString("N2", Zh)}"));
        var sentence = masked
            ? "当前待核销应收当前账号看不到。"
            : string.IsNullOrEmpty(bands)
                ? $"当前待核销应收为 {Money(money.TotalUsd)} 美元。"
                : $"当前待核销应收为 {Money(money.TotalUsd)} 美元（{bands}）。";
        return NumberResult(
            state,
            sentence,
            "口径与销售分析待办的待核销应收一致。美元优先用出库时写入的销售折算单价。",
            masked ? null : money.TotalUsd,
            "美元",
            Link("查看应收款", "FinanceReceivableList", "onlyOpen", "1"));
    }

    private async Task<AiDataQueryResponse> ReceivableCustomersAsync(
        string userId,
        UserPermissionSummaryDto summary,
        AiDataQueryState state,
        CancellationToken cancellationToken)
    {
        var mask = MaskSalesAmounts(summary);
        var rankings = await _receivableAnalytics.GetListAnalyticsRankingsAsync(
            new FinanceReceivableQueryRequest
            {
                CurrentUserId = userId,
                OnlyOpen = true
            },
            cancellationToken);
        var rows = rankings.CustomerByAmount
            .Where(r => (r.PendingAmountUsd ?? 0m) > 0m || mask)
            .Take(10)
            .ToList();
        if (mask)
        {
            return new AiDataQueryResponse
            {
                Kind = AiDataQueryKinds.Result,
                Summary = "有待核销应收的客户金额当前账号看不到。",
                BasisNote = "口径与应收款列表的待核销余额一致。",
                State = state,
                Link = Link("查看应收款", "FinanceReceivableList", "onlyOpen", "1")
            };
        }

        return RankResult(
            state,
            rows.Count == 0
                ? "当前没有待核销应收的客户。"
                : $"待核销金额最高的 {rows.Count} 个客户如下。",
            "口径与应收款列表的待核销余额一致，与看板排行同为前 10。",
            rows.Select(r => Point(r.Name, r.PendingAmountUsd)).ToList(),
            "美元",
            rows.Select(r => new AiDataQueryRowDto
            {
                Label = r.Name,
                ValueText = Money(r.PendingAmountUsd),
                RouteName = "FinanceReceivableList",
                RouteQueryKey = "keyword",
                RouteQueryValue = r.Name
            }).ToList(),
            Link("查看应收款", "FinanceReceivableList", "onlyOpen", "1"));
    }

    private async Task<(string? Error, SalesAnalyticsResolvedScope? Scope)> ResolveSalesAsync(
        string userId,
        DateTime from,
        DateTime to,
        string groupBy,
        CancellationToken cancellationToken)
    {
        var (ok, error, scope) = await _salesAnalytics.ResolveScopeAsync(
            userId,
            new SalesAnalyticsQueryParams
            {
                ViewLevel = SalesAnalyticsViewLevels.Company,
                DateFrom = from,
                DateTo = to,
                GroupBy = groupBy
            },
            cancellationToken);
        if (!ok || scope == null)
            return (string.IsNullOrWhiteSpace(error) ? "当前账号无销售数据范围。" : error, null);
        return (null, scope);
    }

    private static StockOutItemListQuery StockOutQuery(string userId, DateTime from, DateTime to) =>
        new()
        {
            CurrentUserId = userId,
            Status = StockOutStatusCode.Completed,
            StockOutType = StockOutTypeCode.Sales,
            StockOutDateFrom = from,
            StockOutDateTo = to
        };

    private static (DateTime From, DateTime To, string Label) PeriodRange(string? period)
    {
        var today = CompanyCalendarDate.ToCompanyDate(DateTime.UtcNow);
        var todayDt = today.ToDateTime(TimeOnly.MinValue);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        return period switch
        {
            "today" => (todayDt, todayDt, "今天"),
            "lastMonth" => (
                monthStart.AddMonths(-1).ToDateTime(TimeOnly.MinValue),
                monthStart.AddDays(-1).ToDateTime(TimeOnly.MinValue),
                "上月"),
            "last3Months" => (monthStart.AddMonths(-2).ToDateTime(TimeOnly.MinValue), todayDt, "近三个月"),
            "last6Months" => (monthStart.AddMonths(-5).ToDateTime(TimeOnly.MinValue), todayDt, "近半年"),
            _ => (monthStart.ToDateTime(TimeOnly.MinValue), todayDt, "本月")
        };
    }

    private static string GroupBy(DateTime from, DateTime to) =>
        (to.Date - from.Date).TotalDays <= 31 ? "day" : "month";

    private static bool MaskSalesAmounts(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin) return false;
        if (SaleSensitiveFieldMask521.ShouldMask(summary)) return true;
        return !summary.PermissionCodes.Any(c =>
            string.Equals(c, "sales.amount.read", StringComparison.OrdinalIgnoreCase));
    }

    private static bool MaskPurchaseAmounts(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin) return false;
        if (PurchaseSensitiveFieldMask511.ShouldMask(summary)) return true;
        return !summary.PermissionCodes.Any(c =>
            string.Equals(c, "purchase.amount.read", StringComparison.OrdinalIgnoreCase));
    }

    private static List<SalesAnalyticsRankingRowDto> UsdRows(
        IReadOnlyList<InventoryOnHandListAnalyticsRankingFacetDto> facets)
    {
        var facet = facets.FirstOrDefault(f =>
            string.Equals(f.CurrencyKey, InventoryOnHandCurrency.ConvertedUsdKey, StringComparison.OrdinalIgnoreCase));
        return facet?.Rows?.ToList() ?? new List<SalesAnalyticsRankingRowDto>();
    }

    private static AiDataQueryResponse NumberResult(
        AiDataQueryState state,
        string summary,
        string basis,
        decimal? value,
        string unit,
        AiDataQueryLinkDto link) =>
        new()
        {
            Kind = AiDataQueryKinds.Result,
            Summary = summary,
            BasisNote = basis,
            State = state,
            Chart = value == null
                ? null
                : new AiDataQueryChartDto
                {
                    Type = "number",
                    Unit = unit,
                    Points = new[] { new AiDataQueryPointDto { Label = summary, Value = value } }
                },
            Link = link
        };

    private static AiDataQueryResponse ChartResult(
        AiDataQueryState state,
        string summary,
        string basis,
        string type,
        string unit,
        IReadOnlyList<AiDataQueryPointDto> points,
        AiDataQueryLinkDto link) =>
        new()
        {
            Kind = AiDataQueryKinds.Result,
            Summary = summary,
            BasisNote = basis,
            State = state,
            Chart = points.Count == 0 || points.All(p => p.Value == null)
                ? null
                : new AiDataQueryChartDto { Type = type, Unit = unit, Points = points },
            Link = link
        };

    private static AiDataQueryResponse RankResult(
        AiDataQueryState state,
        string summary,
        string basis,
        IReadOnlyList<AiDataQueryPointDto> points,
        string unit,
        IReadOnlyList<AiDataQueryRowDto> rows,
        AiDataQueryLinkDto link)
    {
        var visible = points.Where(p => p.Value != null).ToList();
        return new AiDataQueryResponse
        {
            Kind = AiDataQueryKinds.Result,
            Summary = summary,
            BasisNote = basis,
            State = state,
            Chart = visible.Count == 0
                ? null
                : new AiDataQueryChartDto { Type = "bar", Unit = unit, Points = visible },
            Rows = rows.Take(20).ToList(),
            Link = link
        };
    }

    private static AiDataQueryResponse Denied(AiDataQueryState state, string message) =>
        new()
        {
            Kind = AiDataQueryKinds.Result,
            Summary = message,
            State = state
        };

    private static AiDataQueryResponse Unsupported(AiDataQueryState state) =>
        new()
        {
            Kind = AiDataQueryKinds.Unsupported,
            Summary = "这个数还不能查。",
            State = state
        };

    private static AiDataQueryLinkDto Link(string label, string route, string? key = null, string? value = null) =>
        new()
        {
            Label = label,
            RouteName = route,
            QueryKey = key,
            QueryValue = value
        };

    private static AiDataQueryPointDto Point(string label, decimal? value) =>
        new() { Label = label, Value = value };

    private static AiDataQueryRowDto Row(string label, string? value) =>
        new() { Label = label, ValueText = value };

    private static string Money(decimal? value) =>
        (value ?? 0m).ToString("N2", Zh);

    private static string? MoneyOrBlank(decimal? value, bool mask) =>
        mask || value == null ? null : Money(value);

    private static string Qty(int value) => value.ToString("N0", Zh);
}
