using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequireAnyPermission("analytics-purchase.read", "purchase-order.read")]
[ApiController]
[Route("api/v1/analytics/purchase")]
public class PurchaseAnalyticsController : ControllerBase
{
    private readonly IPurchaseAnalyticsService _service;
    private readonly IPurchaseOrderItemListQuery _purchaseOrderItemListQuery;
    private readonly IQuoteListQuery _quoteListQuery;

    public PurchaseAnalyticsController(
        IPurchaseAnalyticsService service,
        IPurchaseOrderItemListQuery purchaseOrderItemListQuery,
        IQuoteListQuery quoteListQuery)
    {
        _service = service;
        _purchaseOrderItemListQuery = purchaseOrderItemListQuery;
        _quoteListQuery = quoteListQuery;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetDashboardAsync(scope!, cancellationToken);
        return Ok(ApiResponse<PurchaseAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, groupBy, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetTrendsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PurchaseAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("breakdowns")]
    public async Task<IActionResult> GetBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetBreakdownsAsync(scope!, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    /// <summary>供应商维看板（成单/复采供应商、身份等级行业、供应商 Top10）。</summary>
    [HttpGet("vendor")]
    public async Task<IActionResult> GetVendor(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var data = await _service.GetVendorAsync(scope!, cancellationToken);
        return Ok(ApiResponse<PurchaseAnalyticsVendorDto>.Ok(data));
    }

    /// <summary>采购订单明细维看板（与明细列表看板同实现；dataset=reportApproved 成单口径）。</summary>
    [HttpGet("order-items/dashboard")]
    public async Task<IActionResult> GetOrderItemsDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        var data = await _purchaseOrderItemListQuery.GetListAnalyticsDashboardAsync(request, scope!.MaskAmounts, cancellationToken);
        return Ok(ApiResponse<PurchaseOrderItemListAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("order-items/trends")]
    public async Task<IActionResult> GetOrderItemsTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, groupBy, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        var data = await _purchaseOrderItemListQuery.GetListAnalyticsTrendsAsync(
            request,
            scope!.GroupBy,
            scope.MaskAmounts,
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PurchaseOrderItemListAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("order-items/breakdowns")]
    public async Task<IActionResult> GetOrderItemsBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        var data = await _purchaseOrderItemListQuery.GetListAnalyticsBreakdownsAsync(request, scope!.MaskAmounts, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    [HttpGet("order-items/rankings")]
    public async Task<IActionResult> GetOrderItemsRankings(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? rankingSort = null,
        [FromQuery] string? rankingLineMetric = null,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var request = BuildOrderItemsRequest(scope!);
        request.AnalyticsRankingSort = rankingSort;
        request.AnalyticsRankingLineMetric = rankingLineMetric;
        var data = await _purchaseOrderItemListQuery.GetListAnalyticsRankingsAsync(request, scope!.MaskAmounts, cancellationToken);
        return Ok(ApiResponse<PurchaseOrderItemListAnalyticsRankingsDto>.Ok(data));
    }

    /// <summary>报价维看板（与报价列表看板同实现；dataset=reportScope，按 quote.create_time + 采购员透镜）。</summary>
    [HttpGet("quotes/dashboard")]
    public async Task<IActionResult> GetQuotesDashboard(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, maskCustomerNames, maskVendorNames) = BuildQuotesRequest(scope!);
        var data = await _quoteListQuery.GetListAnalyticsDashboardAsync(
            request, maskCustomerNames, maskVendorNames, cancellationToken);
        return Ok(ApiResponse<QuoteListAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("quotes/trends")]
    public async Task<IActionResult> GetQuotesTrends(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? groupBy,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, groupBy, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, _, _) = BuildQuotesRequest(scope!);
        var data = await _quoteListQuery.GetListAnalyticsTrendsAsync(request, scope!.GroupBy, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<QuoteListAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("quotes/breakdowns")]
    public async Task<IActionResult> GetQuotesBreakdowns(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, _, _) = BuildQuotesRequest(scope!);
        var data = await _quoteListQuery.GetListAnalyticsBreakdownsAsync(request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SalesAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    [HttpGet("quotes/rankings")]
    public async Task<IActionResult> GetQuotesRankings(
        [FromQuery] string? viewLevel,
        [FromQuery] string? departmentId,
        [FromQuery] string? purchaseUserId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken cancellationToken = default)
    {
        var (ok, error, scope) = await ResolveAsync(viewLevel, departmentId, purchaseUserId, dateFrom, dateTo, null, cancellationToken);
        if (!ok)
            return Forbidden(error);

        var (request, maskCustomerNames, maskVendorNames) = BuildQuotesRequest(scope!);
        var data = await _quoteListQuery.GetListAnalyticsRankingsAsync(
            request, maskCustomerNames, maskVendorNames, cancellationToken);
        return Ok(ApiResponse<QuoteListAnalyticsRankingsDto>.Ok(data));
    }

    private async Task<(bool Ok, string? Error, PurchaseAnalyticsResolvedScope? Scope)> ResolveAsync(
        string? viewLevel,
        string? departmentId,
        string? purchaseUserId,
        string? dateFrom,
        string? dateTo,
        string? groupBy,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return (false, "未登录或登录态失效", null);

        var query = new PurchaseAnalyticsQueryParams
        {
            ViewLevel = string.IsNullOrWhiteSpace(viewLevel) ? SalesAnalyticsViewLevels.Company : viewLevel.Trim(),
            DepartmentId = departmentId,
            PurchaseUserId = purchaseUserId,
            DateFrom = ParseQueryDate(dateFrom),
            DateTo = ParseQueryDate(dateTo),
            GroupBy = string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim()
        };

        return await _service.ResolveScopeAsync(userId, query, cancellationToken);
    }

    private static PurchaseOrderItemListQueryRequest BuildOrderItemsRequest(PurchaseAnalyticsResolvedScope scope)
    {
        var viewLevel = scope.ViewLevel?.Trim().ToLowerInvariant() ?? SalesAnalyticsViewLevels.Company;
        return new PurchaseOrderItemListQueryRequest
        {
            StartDate = scope.DateFrom,
            EndDate = scope.DateTo,
            AnalyticsDataset = PurchaseOrderItemAnalyticsDatasets.ReportApproved,
            AnalyticsViewLevel = viewLevel,
            AnalyticsDepartmentId = viewLevel == SalesAnalyticsViewLevels.Department ? scope.DepartmentId : null,
            PurchaseUserId = viewLevel == SalesAnalyticsViewLevels.Personal ? scope.PurchaseUserId : null,
            CurrentUserId = scope.Summary.UserId
        };
    }

    private static (QuoteQueryRequest Request, bool MaskCustomerNames, bool MaskVendorNames) BuildQuotesRequest(
        PurchaseAnalyticsResolvedScope scope)
    {
        var viewLevel = scope.ViewLevel?.Trim().ToLowerInvariant() ?? SalesAnalyticsViewLevels.Company;
        var mask521 = SaleSensitiveFieldMask521.ShouldMask(scope.Summary);
        var mask511 = PurchaseSensitiveFieldMask511.ShouldMask(scope.Summary);
        var canViewCustomer = !mask521 && (scope.Summary.IsSysAdmin
            || (scope.Summary.PermissionCodes?.Contains("customer.info.read") ?? false));

        var request = new QuoteQueryRequest
        {
            StartDate = scope.DateFrom,
            EndDate = scope.DateTo,
            AnalyticsDataset = QuoteAnalyticsDatasets.ReportScope,
            AnalyticsViewLevel = viewLevel,
            AnalyticsDepartmentId = viewLevel == SalesAnalyticsViewLevels.Department ? scope.DepartmentId : null,
            PurchaseUserId = viewLevel == SalesAnalyticsViewLevels.Personal ? scope.PurchaseUserId : null,
            CurrentUserId = scope.Summary.UserId
        };

        return (request, !canViewCustomer, mask511);
    }

    private static DateTime? ParseQueryDate(string? value) =>
        DateTime.TryParse(value, out var d) ? PurchaseAnalyticsDateFilter.ToUtcDateStart(d) : null;

    private IActionResult Forbidden(string? message) =>
        new ObjectResult(ApiResponse<object>.Fail(message ?? "无权访问", 403)) { StatusCode = 403 };
}
