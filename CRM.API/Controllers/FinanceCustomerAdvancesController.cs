using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Constants;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.API.Authorization;
using CRM.API.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/customer-advances")]
public class FinanceCustomerAdvancesController : ControllerBase
{
    private readonly IFinanceCustomerAdvanceService _service;
    private readonly IRbacService _rbacService;
    private readonly IExportOperationLogService _exportLog;
    private readonly ILogger<FinanceCustomerAdvancesController> _logger;

    public FinanceCustomerAdvancesController(
        IFinanceCustomerAdvanceService service,
        IRbacService rbacService,
        IExportOperationLogService exportLog,
        ILogger<FinanceCustomerAdvancesController> logger)
    {
        _service = service;
        _rbacService = rbacService;
        _exportLog = exportLog;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? currency,
        [FromQuery] bool? onlyPositiveBalance,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetPagedAsync(new FinanceCustomerAdvanceQueryRequest
            {
                Keyword = keyword,
                CustomerId = customerId,
                Currency = currency,
                OnlyPositiveBalance = onlyPositiveBalance ?? true,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
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
            _logger.LogError(ex, "获取预收余额列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportList(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] short? currency,
        [FromQuery] bool? onlyPositiveBalance,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            var request = new FinanceCustomerAdvanceQueryRequest
            {
                Keyword = keyword,
                CustomerId = mask521 ? null : customerId,
                Currency = currency,
                OnlyPositiveBalance = onlyPositiveBalance ?? true,
                CurrentUserId = InventoryExportHttp.UserId(User)
            };
            var (items, truncated, _) = await InventoryExportHttp.CollectForExportAsync(
                (page, pageSize, ct) =>
                {
                    request.Page = page;
                    request.PageSize = pageSize;
                    return _service.GetPagedAsync(request, ct);
                },
                cancellationToken: cancellationToken);
            if (mask521)
                SaleSensitiveFieldMask521.ApplyFinanceCustomerAdvances(items, true);

            var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
            {
                ["keyword"] = keyword,
                ["customerId"] = mask521 ? null : customerId,
                ["currency"] = currency,
                ["onlyPositiveBalance"] = onlyPositiveBalance ?? true
            });
            await FinanceExportHttp.AppendListLogAsync(
                _exportLog,
                BusinessLogTypes.FinanceCustomerAdvance,
                ExportOperationAudit.FinanceCustomerAdvanceListRecordCode,
                FinanceExportActionTypes.CustomerAdvanceListExport,
                ExportAuditKinds.FinanceCustomerAdvanceList,
                "预收款",
                items.Count,
                truncated,
                filters,
                mask521,
                User,
                cancellationToken);
            return FinanceExportHttp.CsvFile(FinanceExportHttp.BuildCustomerAdvanceCsv(items, mask521), "预收款.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出预收款失败");
            return StatusCode(500, new { success = false, message = $"导出预收款失败: {ex.Message}" });
        }
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] string? customerId,
        [FromQuery] short? currency,
        [FromQuery] short? ledgerType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetLedgerPagedAsync(new FinanceCustomerAdvanceLedgerQueryRequest
            {
                CustomerId = customerId,
                Currency = currency,
                LedgerType = ledgerType,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
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
            _logger.LogError(ex, "获取预收流水失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance([FromQuery] string customerId, [FromQuery] short currency = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { success = false, message = "请指定客户" });

            var balance = await _service.GetBalanceAsync(customerId.Trim(), currency);
            var all = await _service.GetBalancesForCustomerAsync(customerId.Trim());
            return Ok(new { success = true, data = new { balance, balances = all } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取预收余额失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
