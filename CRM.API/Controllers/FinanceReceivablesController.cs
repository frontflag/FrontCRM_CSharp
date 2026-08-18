using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Finance;
using CRM.Core.Constants;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/receivables")]
public class FinanceReceivablesController : ControllerBase
{
    private readonly IFinanceReceivableService _service;
    private readonly IFinanceReceivableListQuery _listQuery;
    private readonly IRbacService _rbacService;
    private readonly IExportOperationLogService _exportLog;
    private readonly ILogger<FinanceReceivablesController> _logger;

    public FinanceReceivablesController(
        IFinanceReceivableService service,
        IFinanceReceivableListQuery listQuery,
        IRbacService rbacService,
        IExportOperationLogService exportLog,
        ILogger<FinanceReceivablesController> logger)
    {
        _service = service;
        _listQuery = listQuery;
        _rbacService = rbacService;
        _exportLog = exportLog;
        _logger = logger;
    }

    [HttpGet("analytics/dashboard")]
    public async Task<IActionResult> GetListAnalyticsDashboard(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] short? invoiceMatchStatus,
        [FromQuery] bool? invoiceMatchOnlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        CancellationToken cancellationToken = default)
    {
        var request = BuildListQueryRequest(keyword, customerId, verificationStatus, onlyOpen, invoiceMatchStatus, invoiceMatchOnlyOpen, stockOutDateFrom, stockOutDateTo);
        var data = await _listQuery.GetListAnalyticsDashboardAsync(request, cancellationToken);
        return Ok(ApiResponse<FinanceReceivableListAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("analytics/trends")]
    public async Task<IActionResult> GetListAnalyticsTrends(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] short? invoiceMatchStatus,
        [FromQuery] bool? invoiceMatchOnlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        [FromQuery] string? groupBy = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildListQueryRequest(keyword, customerId, verificationStatus, onlyOpen, invoiceMatchStatus, invoiceMatchOnlyOpen, stockOutDateFrom, stockOutDateTo);
        var data = await _listQuery.GetListAnalyticsTrendsAsync(
            request,
            string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
            cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FinanceReceivableListAnalyticsTrendPointDto>>.Ok(data));
    }

    [HttpGet("analytics/breakdowns")]
    public async Task<IActionResult> GetListAnalyticsBreakdowns(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] short? invoiceMatchStatus,
        [FromQuery] bool? invoiceMatchOnlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        CancellationToken cancellationToken = default)
    {
        var request = BuildListQueryRequest(keyword, customerId, verificationStatus, onlyOpen, invoiceMatchStatus, invoiceMatchOnlyOpen, stockOutDateFrom, stockOutDateTo);
        var data = await _listQuery.GetListAnalyticsBreakdownsAsync(request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FinanceReceivableListAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    [HttpGet("analytics/rankings")]
    public async Task<IActionResult> GetListAnalyticsRankings(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] short? invoiceMatchStatus,
        [FromQuery] bool? invoiceMatchOnlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        CancellationToken cancellationToken = default)
    {
        var request = BuildListQueryRequest(keyword, customerId, verificationStatus, onlyOpen, invoiceMatchStatus, invoiceMatchOnlyOpen, stockOutDateFrom, stockOutDateTo);
        var data = await _listQuery.GetListAnalyticsRankingsAsync(request, cancellationToken);
        return Ok(ApiResponse<FinanceReceivableListAnalyticsRankingsDto>.Ok(data));
    }

    private FinanceReceivableQueryRequest BuildListQueryRequest(
        string? keyword,
        string? customerId,
        short? verificationStatus,
        bool? onlyOpen,
        short? invoiceMatchStatus,
        bool? invoiceMatchOnlyOpen,
        string? stockOutDateFrom,
        string? stockOutDateTo,
        int page = 1,
        int pageSize = 20) =>
        new()
        {
            Keyword = keyword,
            CustomerId = customerId,
            VerificationStatus = verificationStatus,
            OnlyOpen = onlyOpen,
            InvoiceMatchStatus = invoiceMatchStatus,
            InvoiceMatchOnlyOpen = invoiceMatchOnlyOpen,
            StockOutDateFrom = DateTime.TryParse(stockOutDateFrom, out var from) ? from : null,
            StockOutDateTo = DateTime.TryParse(stockOutDateTo, out var to) ? to : null,
            Page = page,
            PageSize = pageSize,
            CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        };

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] short? invoiceMatchStatus,
        [FromQuery] bool? invoiceMatchOnlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var request = BuildListQueryRequest(
                keyword, customerId, verificationStatus, onlyOpen, invoiceMatchStatus, invoiceMatchOnlyOpen,
                stockOutDateFrom, stockOutDateTo, page, pageSize);
            var result = await _service.GetPagedListAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    items = result.Items,
                    total = result.TotalCount,
                    page = result.PageIndex,
                    pageSize = result.PageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取应收款列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportList(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] short? invoiceMatchStatus,
        [FromQuery] bool? invoiceMatchOnlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            var request = BuildListQueryRequest(
                keyword, mask521 ? null : customerId, verificationStatus, onlyOpen, invoiceMatchStatus, invoiceMatchOnlyOpen,
                stockOutDateFrom, stockOutDateTo);
            request.CurrentUserId = InventoryExportHttp.UserId(User);
            var (items, truncated, _) = await InventoryExportHttp.CollectForExportAsync(
                (page, pageSize, ct) =>
                {
                    request.Page = page;
                    request.PageSize = pageSize;
                    return _service.GetPagedListAsync(request, ct);
                },
                cancellationToken: cancellationToken);
            if (mask521)
                SaleSensitiveFieldMask521.ApplyFinanceReceivableListItems(items, true);

            var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
            {
                ["keyword"] = keyword,
                ["customerId"] = mask521 ? null : customerId,
                ["verificationStatus"] = verificationStatus,
                ["onlyOpen"] = onlyOpen,
                ["invoiceMatchStatus"] = invoiceMatchStatus,
                ["invoiceMatchOnlyOpen"] = invoiceMatchOnlyOpen,
                ["stockOutDateFrom"] = stockOutDateFrom,
                ["stockOutDateTo"] = stockOutDateTo
            });
            await FinanceExportHttp.AppendListLogAsync(
                _exportLog,
                BusinessLogTypes.FinanceReceivable,
                ExportOperationAudit.FinanceReceivableListRecordCode,
                FinanceExportActionTypes.ReceivableListExport,
                ExportAuditKinds.FinanceReceivableList,
                "应收款",
                items.Count,
                truncated,
                filters,
                mask521,
                User,
                cancellationToken);
            return FinanceExportHttp.CsvFile(FinanceExportHttp.BuildReceivableCsv(items, mask521), "应收款.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出应收款失败");
            return StatusCode(500, new { success = false, message = $"导出应收款失败: {ex.Message}" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { success = false, message = "请指定应收款" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetByIdAsync(id, userId);
            if (data == null)
                return NotFound(new { success = false, message = "应收款不存在或无权查看" });

            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取应收款详情失败 Id={Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>作废未核销孤儿应收（出库已删；系统管理员 / 平台管理员）。出库仍有效时拒绝。</summary>
    [HttpPost("{id}/void")]
    [RequirePermission("finance-receipt.write")]
    public async Task<IActionResult> VoidUnverified(string id, [FromBody] VoidFinanceReceivableRequest? body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return StatusCode(403, new { success = false, message = "未登录或身份无效" });

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
            if (!ManagementAccountPolicy.CanForceDelete(summary))
                return StatusCode(403, new { success = false, message = "仅系统管理员或平台管理员可作废应收" });

            if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                return BadRequest(new { success = false, message = "请填写 confirmBillCode" });

            await _service.VoidUnverifiedAsync(id, body.ConfirmBillCode.Trim(), userId.Trim());
            return Ok(new { success = true, message = "应收已作废" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "作废应收失败 Id={Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class VoidFinanceReceivableRequest
    {
        public string? ConfirmBillCode { get; set; }
    }

    [HttpGet("{id}/write-offs")]
    public async Task<IActionResult> GetWriteOffs(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { success = false, message = "请指定应收款" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var receivable = await _service.GetByIdAsync(id, userId);
            if (receivable == null)
                return NotFound(new { success = false, message = "应收款不存在或无权查看" });

            var data = await _service.GetWriteOffsByReceivableIdAsync(id, userId);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取应收款核销记录失败 Id={Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/receivable-write-offs")]
public class FinanceReceivableWriteOffsController : ControllerBase
{
    private readonly IFinanceReceivableService _service;
    private readonly IFinanceCustomerAdvanceService _advanceService;
    private readonly ILogger<FinanceReceivableWriteOffsController> _logger;

    public FinanceReceivableWriteOffsController(
        IFinanceReceivableService service,
        IFinanceCustomerAdvanceService advanceService,
        ILogger<FinanceReceivableWriteOffsController> logger)
    {
        _service = service;
        _advanceService = advanceService;
        _logger = logger;
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.GetWriteOffLedgerPagedAsync(new FinanceReceivableWriteOffLedgerQueryRequest
            {
                Keyword = keyword,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = userId
            });
            return Ok(new
            {
                success = true,
                data = new
                {
                    items = result.Items,
                    total = result.TotalCount,
                    page = result.PageIndex,
                    pageSize = result.PageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收款核销流水失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("customer-summaries")]
    public async Task<IActionResult> GetCustomerSummaries([FromQuery] string? keyword)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetWriteOffCustomerSummariesAsync(keyword, userId);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收款核销客户汇总失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates([FromQuery] string customerId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { success = false, message = "请指定客户" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var data = await _service.GetWriteOffCandidatesAsync(customerId, userId);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取核销候选数据失败 CustomerId={CustomerId}", customerId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] FinanceReceivableWriteOffRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.ApplyWriteOffAsync(request, userId);
            if (result.RequiresSoMismatchConfirm)
            {
                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "预收关联销售订单与应收不一致，请确认后继续"
                });
            }

            return Ok(new { success = true, data = result, message = "核销成功" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收款核销失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    public class CreditReceiptItemRemainderToPoolRequest
    {
        public decimal? Amount { get; set; }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPost("receipt-items/{receiptItemId}/credit-to-advance-pool")]
    public async Task<IActionResult> CreditReceiptItemRemainderToAdvancePool(
        string receiptItemId,
        [FromBody] CreditReceiptItemRemainderToPoolRequest? body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _advanceService.CreditReceiptItemRemainderToAdvancePoolAsync(
                receiptItemId,
                body?.Amount,
                userId);
            return Ok(new
            {
                success = true,
                data = result,
                message = $"已转入预收池 {result.CreditedAmount:0.##}，收款明细剩余可核销 {result.RemainingAfter:0.##}"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收款余额转预收失败 ReceiptItemId={ReceiptItemId}", receiptItemId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
