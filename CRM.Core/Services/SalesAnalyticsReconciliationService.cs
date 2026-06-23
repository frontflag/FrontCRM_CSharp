using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class SalesAnalyticsReconciliationService : ISalesAnalyticsReconciliationService
{
    private readonly ISalesAnalyticsService _analyticsService;
    private readonly ISalesAnalyticsReconciliationBaseline _baseline;
    private readonly ISalesOrderListQuery _salesOrderListQuery;
    private readonly IRbacService _rbacService;
    private readonly IRepository<CRM.Core.Models.User> _userRepo;

    public SalesAnalyticsReconciliationService(
        ISalesAnalyticsService analyticsService,
        ISalesAnalyticsReconciliationBaseline baseline,
        ISalesOrderListQuery salesOrderListQuery,
        IRbacService rbacService,
        IRepository<CRM.Core.Models.User> userRepo)
    {
        _analyticsService = analyticsService;
        _baseline = baseline;
        _salesOrderListQuery = salesOrderListQuery;
        _rbacService = rbacService;
        _userRepo = userRepo;
    }

    public async Task<SalesAnalyticsReconciliationReportDto> ReconcileAsync(
        string actingUserId,
        SalesAnalyticsQueryParams query,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await _analyticsService.ResolveScopeAsync(actingUserId, query, cancellationToken);
        if (!ok || scope == null)
            throw new InvalidOperationException(error ?? "无法解析看板范围");

        var dashboard = await _analyticsService.GetDashboardAsync(scope, cancellationToken);
        var baselineSnapshot = await _baseline.GetSnapshotAsync(scope, cancellationToken);
        var baselineTodo = await _baseline.GetTodoAsync(scope, cancellationToken);

        var user = (await _userRepo.GetAllAsync()).FirstOrDefault(u => u.Id == actingUserId);

        var metrics = new List<SalesAnalyticsReconciliationMetricDto>
        {
            CompareDecimal("salesAmountApproved", "成单金额(已审核)", dashboard.Snapshot.SalesAmountApproved, baselineSnapshot.SalesAmountApproved),
            CompareInt("salesOrderItemCount", "销售订单条目数", dashboard.Snapshot.SalesOrderItemCount, baselineSnapshot.SalesOrderItemCount),
            CompareInt("salesOrderCustomerCount", "销售客户数", dashboard.Snapshot.SalesOrderCustomerCount, baselineSnapshot.SalesOrderCustomerCount),
            CompareInt("rfqItemCount", "需求条目数", dashboard.Snapshot.RfqItemCount, baselineSnapshot.RfqItemCount),
            CompareInt("rfqCustomerCount", "需求客户数", dashboard.Snapshot.RfqCustomerCount, baselineSnapshot.RfqCustomerCount),
            CompareRate("rfqToSalesConversionRate", "需求→销售转化率", dashboard.Snapshot.RfqToSalesConversionRate, baselineSnapshot.RfqToSalesConversionRate),
            CompareDecimal("receivableAmount", "应收款金额", dashboard.Todo.ReceivableAmount, baselineTodo.ReceivableAmount),
            CompareInt("pendingStockOutItemCount", "待出库明细数", dashboard.Todo.PendingStockOutItemCount, baselineTodo.PendingStockOutItemCount),
            CompareDecimal("pendingInvoiceAmount", "待开票金额", dashboard.Todo.PendingInvoiceAmount, baselineTodo.PendingInvoiceAmount),
            CompareDecimal("salesAmountStockOut", "已出库金额", dashboard.Snapshot.SalesAmountStockOut, baselineSnapshot.SalesAmountStockOut),
            CompareDecimal("salesAmountReceived", "已收款金额", dashboard.Snapshot.SalesAmountReceived, baselineSnapshot.SalesAmountReceived)
        };

        bool? listPathMatched = null;
        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && (scope.Summary.SaleDataScope == 1 || string.IsNullOrWhiteSpace(scope.SalesUserId)
                || string.Equals(scope.SalesUserId, actingUserId, StringComparison.OrdinalIgnoreCase)))
        {
            var listComparable = await _salesOrderListQuery.GetAnalyticsComparableAsync(
                new SalesOrderQueryRequest
                {
                    CurrentUserId = actingUserId,
                    StartDate = scope.DateFrom,
                    EndDate = scope.DateTo
                },
                cancellationToken);

            metrics.Add(CompareDecimal("listApprovedConvertTotal", "列表路径-成单金额", dashboard.Snapshot.SalesAmountApproved, listComparable.ApprovedConvertTotal));
            metrics.Add(CompareInt("listOrderItemCount", "列表路径-明细数", dashboard.Snapshot.SalesOrderItemCount, listComparable.ItemCount));
            metrics.Add(CompareInt("listCustomerCount", "列表路径-客户数", dashboard.Snapshot.SalesOrderCustomerCount, listComparable.CustomerCount));
            listPathMatched = metrics
                .Where(m => m.Key.StartsWith("list", StringComparison.Ordinal))
                .All(m => m.Matched);
        }

        return new SalesAnalyticsReconciliationReportDto
        {
            UserId = actingUserId,
            UserName = user?.UserName,
            SaleDataScope = scope.Summary.SaleDataScope,
            ViewLevel = scope.ViewLevel,
            DateFrom = scope.DateFrom,
            DateTo = scope.DateTo,
            AllMatched = metrics.All(m => m.Matched),
            ListPathMatched = listPathMatched,
            Metrics = metrics
        };
    }

    public async Task<IReadOnlyList<SalesAnalyticsReconciliationReportDto>> ReconcileSampleUsersAsync(
        string actingUserId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepo.GetAllAsync();
        var sampleNames = new[] { "sales_mgr", "sales_staff", "admin" };
        var reports = new List<SalesAnalyticsReconciliationReportDto>();

        foreach (var name in sampleNames)
        {
            var u = users.FirstOrDefault(x => string.Equals(x.UserName, name, StringComparison.OrdinalIgnoreCase));
            if (u == null) continue;

            var summary = await _rbacService.GetUserPermissionSummaryAsync(u.Id);
            if (!SalesAnalyticsScopeValidator.CanAccessPage(summary))
                continue;

            var levels = SalesAnalyticsScopeValidator.GetAllowedViewLevels(summary);
            foreach (var level in levels)
            {
                var report = await ReconcileAsync(
                    u.Id,
                    new SalesAnalyticsQueryParams
                    {
                        ViewLevel = level,
                        DateFrom = dateFrom,
                        DateTo = dateTo
                    },
                    cancellationToken);
                reports.Add(report);
            }
        }

        return reports;
    }

    private static SalesAnalyticsReconciliationMetricDto CompareDecimal(
        string key,
        string label,
        decimal? dashboard,
        decimal? baseline)
    {
        if (dashboard == null && baseline == null)
            return new SalesAnalyticsReconciliationMetricDto { Key = key, Label = label, Matched = true };

        var d = dashboard ?? 0m;
        var b = baseline ?? 0m;
        return new SalesAnalyticsReconciliationMetricDto
        {
            Key = key,
            Label = label,
            DashboardValue = dashboard,
            BaselineValue = baseline,
            Delta = d - b,
            Matched = d == b
        };
    }

    private static SalesAnalyticsReconciliationMetricDto CompareInt(
        string key,
        string label,
        int dashboard,
        int baseline) =>
        CompareDecimal(key, label, dashboard, baseline);

    private static SalesAnalyticsReconciliationMetricDto CompareRate(
        string key,
        string label,
        decimal? dashboard,
        decimal? baseline) =>
        CompareDecimal(key, label, dashboard, baseline);
}
