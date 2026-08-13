using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Services;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[RequirePermission("finance-receipt.read")]
[ApiController]
[Route("api/v1/finance/freight-forwarder-payables")]
public class FinanceFreightForwarderPayablesController : ControllerBase
{
        private readonly IFinanceFreightForwarderPayableService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IExportOperationLogService _exportLog;
        private readonly ILogger<FinanceFreightForwarderPayablesController> _logger;

        public FinanceFreightForwarderPayablesController(
            IFinanceFreightForwarderPayableService service,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IExportOperationLogService exportLog,
            ILogger<FinanceFreightForwarderPayablesController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _exportLog = exportLog;
            _logger = logger;
        }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] string? freightForwarderCompanyId,
        [FromQuery] short? payableStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var request = new FinanceFreightForwarderPayableQueryRequest
            {
                Keyword = keyword,
                CustomerId = customerId,
                FreightForwarderCompanyId = freightForwarderCompanyId,
                PayableStatus = payableStatus,
                Page = page,
                PageSize = pageSize,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };
            var result = await _service.GetPagedAsync(request);
            var items = result.Items.ToList();
            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
            {
                foreach (var item in items)
                {
                    item.CustomerName = null;
                }
            }
            return Ok(new { success = true, data = new { items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货代付款台账列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportList(
        [FromQuery] string? keyword,
        [FromQuery] string? customerId,
        [FromQuery] string? freightForwarderCompanyId,
        [FromQuery] short? payableStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            var request = new FinanceFreightForwarderPayableQueryRequest
            {
                Keyword = keyword,
                CustomerId = mask521 ? null : customerId,
                FreightForwarderCompanyId = freightForwarderCompanyId,
                PayableStatus = payableStatus,
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
                SaleSensitiveFieldMask521.ApplyFinanceFreightForwarderPayableListItems(items, true);

            var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
            {
                ["keyword"] = keyword,
                ["customerId"] = mask521 ? null : customerId,
                ["freightForwarderCompanyId"] = freightForwarderCompanyId,
                ["payableStatus"] = payableStatus
            });
            await FinanceExportHttp.AppendListLogAsync(
                _exportLog,
                BusinessLogTypes.FinanceFreightForwarderPayable,
                ExportOperationAudit.FinanceFfPayableListRecordCode,
                FinanceExportActionTypes.FfPayableListExport,
                ExportAuditKinds.FinanceFfPayableList,
                "货代付款",
                items.Count,
                truncated,
                filters,
                mask521,
                User,
                cancellationToken);
            return FinanceExportHttp.CsvFile(FinanceExportHttp.BuildFfPayableCsv(items, mask521), "货代付款.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出货代付款失败");
            return StatusCode(500, new { success = false, message = $"导出货代付款失败: {ex.Message}" });
        }
    }

    [HttpGet("{receiptId}")]
    public async Task<IActionResult> GetDetail(string receiptId)
    {
        try
        {
            var detail = await _service.GetDetailAsync(receiptId);
            if (detail == null)
                return NotFound(new { success = false, message = "货代付款台账不存在或收款单未审核通过" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!await _dataPermissionService.CanAccessFinanceReceiptAsync(userId, detail.Receipt))
                return StatusCode(403, new { success = false, message = "无权访问该收款单" });

            if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                SaleSensitiveFieldMask521.ApplyFinanceReceipt(detail.Receipt, true);

            return Ok(new { success = true, data = detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货代付款台账详情失败: {ReceiptId}", receiptId);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPost("{receiptId}/payments")]
    public async Task<IActionResult> CreatePayment(string receiptId, [FromBody] CreateFinanceFreightForwarderPaymentRequest body)
    {
        try
        {
            if (!await FinanceDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, new { success = false, message = "当前账号无收款维护权限" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var payment = await _service.CreatePaymentAsync(receiptId, body, userId);
            return Ok(new { success = true, data = payment, message = "付款记录已保存" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登记货代付款失败: {ReceiptId}", receiptId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPut("{receiptId}/freight-forwarder-company")]
    public async Task<IActionResult> UpdateFfCompany(string receiptId, [FromBody] UpdateReceiptFfCompanyRequest body)
    {
        try
        {
            if (!await FinanceDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, new { success = false, message = "当前账号无收款维护权限" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var receipt = await _service.UpdateReceiptFfCompanyAsync(receiptId, body.FreightForwarderCompanyId, userId);
            return Ok(new { success = true, data = receipt, message = "货代公司已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新收款单货代公司失败: {ReceiptId}", receiptId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
