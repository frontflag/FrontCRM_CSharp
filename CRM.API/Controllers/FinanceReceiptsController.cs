using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Finance;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("finance-receipt.read")]
    [ApiController]
    [Route("api/v1/finance/receipts")]
    public class FinanceReceiptsController : ControllerBase
    {
        private readonly IFinanceReceiptService _service;
        private readonly IFinanceReceivableService _receivableService;
        private readonly IFinanceReceiptListAnalyticsQuery _listAnalytics;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IExportOperationLogService _exportLog;
        private readonly ILogger<FinanceReceiptsController> _logger;

        public FinanceReceiptsController(
            IFinanceReceiptService service,
            IFinanceReceivableService receivableService,
            IFinanceReceiptListAnalyticsQuery listAnalytics,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IExportOperationLogService exportLog,
            ILogger<FinanceReceiptsController> logger)
        {
            _service = service;
            _receivableService = receivableService;
            _listAnalytics = listAnalytics;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _exportLog = exportLog;
            _logger = logger;
        }

        /// <summary>获取收款单列表</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] short? receiptPurpose,
            [FromQuery] short? verificationStatus,
            [FromQuery] short? receiptCurrency,
            [FromQuery] string? receiptDateFrom,
            [FromQuery] string? receiptDateTo,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new FinanceReceiptQueryRequest
                {
                    Keyword = keyword,
                    Status = status,
                    ReceiptPurpose = receiptPurpose,
                    VerificationStatus = verificationStatus,
                    ReceiptCurrency = receiptCurrency,
                    ReceiptDateFrom = PostgreSqlDateTime.ParseDateOnly(receiptDateFrom),
                    ReceiptDateTo = PostgreSqlDateTime.ParseDateOnly(receiptDateTo),
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _service.GetPagedAsync(request);
                var items = result.Items.ToList();
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyFinanceReceipts(items, true);
                return Ok(new { success = true, data = new { items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收款单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>按当前筛选导出收款记录 CSV，并写入操作审计。</summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportList(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] short? receiptPurpose,
            [FromQuery] short? verificationStatus,
            [FromQuery] short? receiptCurrency,
            [FromQuery] string? receiptDateFrom,
            [FromQuery] string? receiptDateTo,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
                var request = new FinanceReceiptQueryRequest
                {
                    Keyword = keyword,
                    Status = status,
                    ReceiptPurpose = receiptPurpose,
                    VerificationStatus = verificationStatus,
                    ReceiptCurrency = receiptCurrency,
                    ReceiptDateFrom = PostgreSqlDateTime.ParseDateOnly(receiptDateFrom),
                    ReceiptDateTo = PostgreSqlDateTime.ParseDateOnly(receiptDateTo),
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    CurrentUserId = InventoryExportHttp.UserId(User)
                };
                var (items, truncated, _) = await InventoryExportHttp.CollectForExportAsync(
                    async (page, pageSize, _) =>
                    {
                        request.Page = page;
                        request.PageSize = pageSize;
                        return await _service.GetPagedAsync(request);
                    },
                    cancellationToken: cancellationToken);
                if (mask521)
                    SaleSensitiveFieldMask521.ApplyFinanceReceipts(items, true);

                var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
                {
                    ["keyword"] = keyword,
                    ["status"] = status,
                    ["receiptPurpose"] = receiptPurpose,
                    ["verificationStatus"] = verificationStatus,
                    ["receiptCurrency"] = receiptCurrency,
                    ["receiptDateFrom"] = receiptDateFrom,
                    ["receiptDateTo"] = receiptDateTo,
                    ["startDate"] = startDate,
                    ["endDate"] = endDate
                });
                await FinanceExportHttp.AppendListLogAsync(
                    _exportLog,
                    BusinessLogTypes.FinanceReceipt,
                    ExportOperationAudit.FinanceReceiptListRecordCode,
                    FinanceExportActionTypes.ReceiptListExport,
                    ExportAuditKinds.FinanceReceiptList,
                    "收款记录",
                    items.Count,
                    truncated,
                    filters,
                    mask521,
                    User,
                    cancellationToken);
                return FinanceExportHttp.CsvFile(FinanceExportHttp.BuildReceiptCsv(items, mask521), "收款记录.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出收款记录失败");
                return StatusCode(500, new { success = false, message = $"导出收款记录失败: {ex.Message}" });
            }
        }

        [HttpGet("analytics/dashboard")]
        public async Task<IActionResult> GetListAnalyticsDashboard(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] short? receiptPurpose,
            [FromQuery] short? verificationStatus,
            [FromQuery] short? receiptCurrency,
            [FromQuery] string? receiptDateFrom,
            [FromQuery] string? receiptDateTo,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryAsync(
                keyword, status, receiptPurpose, verificationStatus, receiptCurrency, receiptDateFrom, receiptDateTo, startDate, endDate, cancellationToken);
            var data = await _listAnalytics.GetDashboardAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<FinanceReceiptListAnalyticsDashboardDto>.Ok(data));
        }

        [HttpGet("analytics/trends")]
        public async Task<IActionResult> GetListAnalyticsTrends(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] short? receiptPurpose,
            [FromQuery] short? verificationStatus,
            [FromQuery] short? receiptCurrency,
            [FromQuery] string? receiptDateFrom,
            [FromQuery] string? receiptDateTo,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] string? groupBy,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryAsync(
                keyword, status, receiptPurpose, verificationStatus, receiptCurrency, receiptDateFrom, receiptDateTo, startDate, endDate, cancellationToken);
            var data = await _listAnalytics.GetTrendsAsync(
                request,
                string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim(),
                maskAmounts,
                cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<FinanceReceiptListAnalyticsTrendPointDto>>.Ok(data));
        }

        [HttpGet("analytics/breakdowns")]
        public async Task<IActionResult> GetListAnalyticsBreakdowns(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] short? receiptPurpose,
            [FromQuery] short? verificationStatus,
            [FromQuery] short? receiptCurrency,
            [FromQuery] string? receiptDateFrom,
            [FromQuery] string? receiptDateTo,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryAsync(
                keyword, status, receiptPurpose, verificationStatus, receiptCurrency, receiptDateFrom, receiptDateTo, startDate, endDate, cancellationToken);
            var data = await _listAnalytics.GetBreakdownsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<FinanceReceiptListAnalyticsBreakdownGroupDto>>.Ok(data));
        }

        [HttpGet("analytics/rankings")]
        public async Task<IActionResult> GetListAnalyticsRankings(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] short? receiptPurpose,
            [FromQuery] short? verificationStatus,
            [FromQuery] short? receiptCurrency,
            [FromQuery] string? receiptDateFrom,
            [FromQuery] string? receiptDateTo,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            var (request, maskAmounts) = await BuildListAnalyticsQueryAsync(
                keyword, status, receiptPurpose, verificationStatus, receiptCurrency, receiptDateFrom, receiptDateTo, startDate, endDate, cancellationToken);
            var data = await _listAnalytics.GetRankingsAsync(request, maskAmounts, cancellationToken);
            return Ok(ApiResponse<FinanceReceiptListAnalyticsRankingsDto>.Ok(data));
        }

        /// <summary>获取单个收款单</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var receipt = await _service.GetByIdAsync(id);
                if (receipt == null) return NotFound(new { success = false, message = "收款单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinanceReceiptAsync(userId, receipt))
                    return StatusCode(403, new { success = false, message = "无权限访问该收款单" });
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyFinanceReceipt(receipt, true);
                return Ok(new { success = true, data = receipt });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>获取收款单核销流水</summary>
        [HttpGet("{id}/write-offs")]
        public async Task<IActionResult> GetWriteOffs(string id)
        {
            try
            {
                var receipt = await _service.GetByIdAsync(id);
                if (receipt == null) return NotFound(new { success = false, message = "收款单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinanceReceiptAsync(userId, receipt))
                    return StatusCode(403, new { success = false, message = "无权限访问该收款单" });

                var items = await _receivableService.GetWriteOffsByReceiptIdAsync(id);
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                {
                    foreach (var item in items)
                    {
                        item.Amount = 0m;
                        item.PN = null;
                        item.Brand = null;
                        item.StockOutCode = null;
                        item.SellOrderCode = null;
                        item.ReceivableCode = null;
                    }
                }

                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收款单核销流水失败 ReceiptId={ReceiptId}", id);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建收款单</summary>
        [HttpPost]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Create([FromBody] CreateFinanceReceiptRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var receipt = await _service.CreateAsync(request, userId);
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyFinanceReceipt(receipt, true);
                return CreatedAtAction(nameof(GetById), new { id = receipt.Id },
                    new { success = true, data = receipt });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新建收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 更新收款单（仅新建）。改收款金额时同步唯一默认明细，见
        /// <see cref="IFinanceReceiptService.UpdateAsync"/>。
        /// </summary>
        [HttpPut("{id}")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinanceReceiptRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var receipt = await _service.UpdateAsync(id, request, userId);
                if (await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User))
                    SaleSensitiveFieldMask521.ApplyFinanceReceipt(receipt, true);
                return Ok(new { success = true, data = receipt });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "收款单不存在" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新收款单状态（仅允许确认 3 / 取消 4）</summary>
        [HttpPatch("{id}/status")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] FinanceReceiptStatusRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var (userId, userName) = GetActor();
                await _service.UpdateStatusAsync(id, request.Status, userId, userName);
                return Ok(new { success = true, message = "状态更新成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "收款单不存在" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新收款单状态失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>收款单已取消审核；请改用确认。</summary>
        [HttpPost("{id}/submit")]
        [RequirePermission("finance-receipt.write")]
        public Task<IActionResult> Submit(string id) =>
            Task.FromResult<IActionResult>(Conflict(new
            {
                success = false,
                message = "收款单已取消审核流程，请使用确认"
            }));

        /// <summary>收款单已取消审核；请改用确认。</summary>
        [HttpPost("{id}/approve")]
        [RequirePermission("finance-receipt.write")]
        public Task<IActionResult> Approve(string id) =>
            Task.FromResult<IActionResult>(Conflict(new
            {
                success = false,
                message = "收款单已取消审核流程，请使用确认"
            }));

        /// <summary>确认收款单（新建 → 确认）</summary>
        [HttpPost("{id}/confirm")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Confirm(string id)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var (userId, userName) = GetActor();
                await _service.ConfirmAsync(id, userId, userName);
                return Ok(new { success = true, message = "已确认" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>兼容旧客户端：与 confirm 相同。</summary>
        [HttpPost("{id}/confirm-received")]
        [RequirePermission("finance-receipt.write")]
        public Task<IActionResult> ConfirmReceived(string id) => Confirm(id);

        /// <summary>取消收款单（新建可直接取消；确认后须整单未核销）</summary>
        [HttpPost("{id}/cancel")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var (userId, userName) = GetActor();
                await _service.UpdateStatusAsync(id, FinanceReceiptStatusCode.Cancelled, userId, userName);
                return Ok(new { success = true, message = "已取消" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>删除收款单</summary>
        [HttpDelete("{id}")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.DeleteAsync(id, actorId);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "收款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>收款反核销（撤销核销流水，主单状态不变）</summary>
        [HttpPost("{id}/reverse-verification")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> ReverseVerification(string id, [FromBody] ForceDeleteFinanceReceiptRequest? body)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(new { success = false, message = "请填写 confirmBillCode" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, new { success = false, message = "未登录或身份无效" });

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                var receipt = await _service.ReverseVerificationAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinanceReceiptAsync(userId, receipt))
                    return StatusCode(403, new { success = false, message = "无权访问该收款单" });

                SaleSensitiveFieldMask521.ApplyFinanceReceipt(receipt, true);
                return Ok(new { success = true, message = "反核销成功", data = receipt });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "收款反核销失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>强制删除收款单（SYS_ADMIN / SYS_MANAGER）</summary>
        [HttpPost("{id}/force-delete")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> ForceDelete(string id, [FromBody] ForceDeleteFinanceReceiptRequest? body)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, new { success = false, message = "未登录或身份无效" });

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (!ManagementAccountPolicy.CanForceDelete(summary))
                    return StatusCode(403, new { success = false, message = "仅系统管理员或平台管理员可执行强制删除" });

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(new { success = false, message = "请填写 confirmBillCode" });

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await _service.ForceDeleteAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(new { success = true, message = "强制删除成功" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除收款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>核销收款明细</summary>
        [HttpPost("items/{receiptItemId}/verify")]
        [RequirePermission("finance-receipt.write")]
        public async Task<IActionResult> VerifyItem(string receiptItemId, [FromBody] VerifyReceiptItemRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.VerifyReceiptItemAsync(receiptItemId, request.SellInvoiceId, request.Amount, userId);
                return Ok(new { success = true, message = "核销成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "核销收款明细失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<(FinanceReceiptQueryRequest Request, bool MaskAmounts)> BuildListAnalyticsQueryAsync(
            string? keyword,
            short? status,
            short? receiptPurpose,
            short? verificationStatus,
            short? receiptCurrency,
            string? receiptDateFrom,
            string? receiptDateTo,
            string? startDate,
            string? endDate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new FinanceReceiptQueryRequest
            {
                Keyword = keyword,
                Status = status,
                ReceiptPurpose = receiptPurpose,
                VerificationStatus = verificationStatus,
                ReceiptCurrency = receiptCurrency,
                ReceiptDateFrom = PostgreSqlDateTime.ParseDateOnly(receiptDateFrom),
                ReceiptDateTo = PostgreSqlDateTime.ParseDateOnly(receiptDateTo),
                StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            };
            var mask521 = await SaleMaskHttp.ShouldMaskSale521Async(_rbacService, User);
            return (request, mask521);
        }

        private (string? userId, string? userName) GetActor()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            return (
                string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
                string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());
        }

        private async Task<IActionResult?> RejectIfFinanceDataReadOnlyAsync()
        {
            if (!await FinanceDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, new { success = false, message = "主部门财务数据为只读或禁止，无法修改" });
            return null;
        }
    }

    public class FinanceReceiptStatusRequest
    {
        public short Status { get; set; }
    }

    public class VerifyReceiptItemRequest
    {
        public string SellInvoiceId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class ForceDeleteFinanceReceiptRequest
    {
        public string ConfirmBillCode { get; set; } = string.Empty;
    }
}
