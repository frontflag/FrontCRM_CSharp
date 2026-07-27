using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Finance;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
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
    private readonly ILogger<FinanceReceivablesController> _logger;

    public FinanceReceivablesController(
        IFinanceReceivableService service,
        IFinanceReceivableListQuery listQuery,
        ILogger<FinanceReceivablesController> logger)
    {
        _service = service;
        _listQuery = listQuery;
        _logger = logger;
    }

    [HttpGet("analytics/dashboard")]
    public async Task<IActionResult> GetListAnalyticsDashboard(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        CancellationToken cancellationToken = default)
    {
        var request = BuildAnalyticsQueryRequest(keyword, customerId, verificationStatus, onlyOpen, stockOutDateFrom, stockOutDateTo);
        var data = await _listQuery.GetListAnalyticsDashboardAsync(request, cancellationToken);
        return Ok(ApiResponse<FinanceReceivableListAnalyticsDashboardDto>.Ok(data));
    }

    [HttpGet("analytics/trends")]
    public async Task<IActionResult> GetListAnalyticsTrends(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        [FromQuery] string? groupBy = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildAnalyticsQueryRequest(keyword, customerId, verificationStatus, onlyOpen, stockOutDateFrom, stockOutDateTo);
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
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        CancellationToken cancellationToken = default)
    {
        var request = BuildAnalyticsQueryRequest(keyword, customerId, verificationStatus, onlyOpen, stockOutDateFrom, stockOutDateTo);
        var data = await _listQuery.GetListAnalyticsBreakdownsAsync(request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FinanceReceivableListAnalyticsBreakdownGroupDto>>.Ok(data));
    }

    [HttpGet("analytics/rankings")]
    public async Task<IActionResult> GetListAnalyticsRankings(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        CancellationToken cancellationToken = default)
    {
        var request = BuildAnalyticsQueryRequest(keyword, customerId, verificationStatus, onlyOpen, stockOutDateFrom, stockOutDateTo);
        var data = await _listQuery.GetListAnalyticsRankingsAsync(request, cancellationToken);
        return Ok(ApiResponse<FinanceReceivableListAnalyticsRankingsDto>.Ok(data));
    }

    private FinanceReceivableQueryRequest BuildAnalyticsQueryRequest(
        string? keyword,
        string? customerId,
        short? verificationStatus,
        bool? onlyOpen,
        string? stockOutDateFrom,
        string? stockOutDateTo) =>
        new()
        {
            Keyword = keyword,
            CustomerId = customerId,
            VerificationStatus = verificationStatus,
            OnlyOpen = onlyOpen ?? true,
            StockOutDateFrom = DateTime.TryParse(stockOutDateFrom, out var from) ? from : null,
            StockOutDateTo = DateTime.TryParse(stockOutDateTo, out var to) ? to : null,
            CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        };

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? verificationStatus,
        [FromQuery] bool? onlyOpen,
        [FromQuery] string? stockOutDateFrom,
        [FromQuery] string? stockOutDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var request = new FinanceReceivableQueryRequest
            {
                Keyword = keyword,
                CustomerId = customerId,
                VerificationStatus = verificationStatus,
                OnlyOpen = onlyOpen ?? true,
                StockOutDateFrom = DateTime.TryParse(stockOutDateFrom, out var from) ? from : null,
                StockOutDateTo = DateTime.TryParse(stockOutDateTo, out var to) ? to : null,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };
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
