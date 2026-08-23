using CRM.API.Authorization;
using CRM.API.Utilities;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Services;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/finance/payments")]
    public class FinancePaymentsController : ControllerBase
    {
        private readonly IFinancePaymentService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IApprovalPartyIntelWarmupService _approvalPartyIntelWarmup;
        private readonly IExportOperationLogService _exportLog;
        private readonly ILogger<FinancePaymentsController> _logger;

        public FinancePaymentsController(
            IFinancePaymentService service,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IApprovalPartyIntelWarmupService approvalPartyIntelWarmup,
            IExportOperationLogService exportLog,
            ILogger<FinancePaymentsController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _approvalPartyIntelWarmup = approvalPartyIntelWarmup;
            _exportLog = exportLog;
            _logger = logger;
        }

        /// <summary>获取付款单列表</summary>
        [HttpGet]
        [RequireAnyPermission("finance-payment.read", "purchase-order.read")]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] string? financePaymentCode,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? bankSlipNo,
            [FromQuery] short? paymentMode,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? purchaseUserName,
            [FromQuery] short? purchaseCurrency,
            [FromQuery] string? remark,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new FinancePaymentQueryRequest
                {
                    Keyword = keyword,
                    FinancePaymentCode = financePaymentCode,
                    FreightForwarderOrderNo = freightForwarderOrderNo,
                    BankSlipNo = bankSlipNo,
                    PaymentMode = paymentMode,
                    VendorName = vendorName,
                    PurchaseOrderCode = purchaseOrderCode,
                    PurchaseUserName = purchaseUserName,
                    PurchaseCurrency = purchaseCurrency,
                    Remark = remark,
                    Status = status,
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _service.GetPagedAsync(request);
                var items = result.Items.ToList();
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyFinancePayments(items, true);
                return Ok(new { success = true, data = new { items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取付款单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>按当前筛选导出付款记录 CSV，并写入操作审计。</summary>
        [HttpGet("export")]
        [RequireAnyPermission("finance-payment.read", "purchase-order.read")]
        public async Task<IActionResult> ExportList(
            [FromQuery] string? keyword,
            [FromQuery] string? financePaymentCode,
            [FromQuery] string? freightForwarderOrderNo,
            [FromQuery] string? bankSlipNo,
            [FromQuery] short? paymentMode,
            [FromQuery] string? vendorName,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] string? purchaseUserName,
            [FromQuery] short? purchaseCurrency,
            [FromQuery] string? remark,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mask511 = await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User);
                var request = new FinancePaymentQueryRequest
                {
                    Keyword = keyword,
                    FinancePaymentCode = financePaymentCode,
                    FreightForwarderOrderNo = freightForwarderOrderNo,
                    BankSlipNo = bankSlipNo,
                    PaymentMode = paymentMode,
                    VendorName = mask511 ? null : vendorName,
                    PurchaseOrderCode = purchaseOrderCode,
                    PurchaseUserName = purchaseUserName,
                    PurchaseCurrency = purchaseCurrency,
                    Remark = remark,
                    Status = status,
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
                if (mask511)
                    PurchaseSensitiveFieldMask511.ApplyFinancePayments(items, true);

                var filters = ExportOperationAudit.NormalizeFilters(new Dictionary<string, object?>
                {
                    ["keyword"] = keyword,
                    ["financePaymentCode"] = financePaymentCode,
                    ["freightForwarderOrderNo"] = freightForwarderOrderNo,
                    ["bankSlipNo"] = bankSlipNo,
                    ["paymentMode"] = paymentMode,
                    ["vendorName"] = mask511 ? null : vendorName,
                    ["purchaseOrderCode"] = purchaseOrderCode,
                    ["purchaseUserName"] = purchaseUserName,
                    ["purchaseCurrency"] = purchaseCurrency,
                    ["remark"] = remark,
                    ["status"] = status,
                    ["startDate"] = startDate,
                    ["endDate"] = endDate
                });
                await FinanceExportHttp.AppendListLogAsync(
                    _exportLog,
                    BusinessLogTypes.FinancePayment,
                    ExportOperationAudit.FinancePaymentListRecordCode,
                    FinanceExportActionTypes.PaymentListExport,
                    ExportAuditKinds.FinancePaymentList,
                    "付款记录",
                    items.Count,
                    truncated,
                    filters,
                    mask511,
                    User,
                    cancellationToken);
                return FinanceExportHttp.CsvFile(FinanceExportHttp.BuildPaymentCsv(items, mask511), "付款记录.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出付款记录失败");
                return StatusCode(500, new { success = false, message = $"导出付款记录失败: {ex.Message}" });
            }
        }

        /// <summary>获取单个付款单</summary>
        [HttpGet("{id}")]
        [RequireAnyPermission("finance-payment.read", "purchase-order.read")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var payment = await _service.GetByIdAsync(id);
                if (payment == null) return NotFound(new { success = false, message = "付款单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessFinancePaymentAsync(userId, payment))
                    return StatusCode(403, new { success = false, message = "无权限访问该付款单" });
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyFinancePayment(payment, true);
                return Ok(new { success = true, data = payment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>新建付款单（采购端可凭采购订单写权限从明细发起）</summary>
        [HttpPost]
        [RequireAnyPermission("finance-payment.write", "purchase-order.write")]
        public async Task<IActionResult> Create([FromBody] CreateFinancePaymentRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync(allowPurchaseOrderWriteBypass: true);
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var payment = await _service.CreateAsync(request, userId);
                return CreatedAtAction(nameof(GetById), new { id = payment.Id },
                    new { success = true, data = payment });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "新建付款单保存失败");
                return BadRequest(new { success = false, message = innerMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新建付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新付款单</summary>
        [HttpPut("{id}")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateFinancePaymentRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var payment = await _service.UpdateAsync(id, request, userId);
                return Ok(new { success = true, data = payment });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "付款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>编辑请款（status 1 / -1）</summary>
        [HttpPut("{id}/request")]
        [RequireAnyPermission("finance-payment.write", "purchase-order.write")]
        public async Task<IActionResult> UpdateRequest(string id, [FromBody] UpdateFinancePaymentRequestBody request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync(allowPurchaseOrderWriteBypass: true);
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var payment = await _service.UpdateRequestAsync(id, request, userId);
                return Ok(new { success = true, data = payment });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "编辑请款失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>保存付款执行信息（status 10）</summary>
        [HttpPut("{id}/execution")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> UpdateExecution(string id, [FromBody] UpdateFinancePaymentExecutionRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var payment = await _service.UpdateExecutionAsync(id, request, userId);
                return Ok(new { success = true, data = payment });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存付款执行信息失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>撤回审核通过的请款（10→1）</summary>
        [HttpPost("{id}/withdraw")]
        [RequireAnyPermission("finance-payment.write", "finance-payment.read", "purchase-order.write", "purchase-order.read")]
        public async Task<IActionResult> Withdraw(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, new { success = false, message = "未登录或身份无效" });

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                var hasFinWrite = summary?.PermissionCodes?.Any(c =>
                    string.Equals(c, "finance-payment.write", StringComparison.OrdinalIgnoreCase)) == true;

                if (hasFinWrite)
                {
                    var denied = await RejectIfFinanceDataReadOnlyAsync();
                    if (denied != null) return denied;
                }

                var payment = await _service.WithdrawAsync(id, userId.Trim(), hasFinWrite);
                return Ok(new { success = true, data = payment, message = "已撤回，请修改后重新提交审批" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "撤回付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>更新付款单状态（提交审核/审核通过/驳回/作废等）；采购从明细提交草稿→待审用 Patch</summary>
        [HttpPatch("{id}/status")]
        [RequireAnyPermission("finance-payment.write", "purchase-order.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] FinancePaymentStatusRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync(allowPurchaseOrderWriteBypass: true);
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, request.Status, remark: null, actingUserId: userId);
                return Ok(new { success = true, message = "状态更新成功" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { success = false, message = "付款单不存在" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新付款单状态失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>提交审核（1 -> 2）</summary>
        [HttpPost("{id}/submit")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Submit(string id)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, 2, remark: null, actingUserId: userId);
                _approvalPartyIntelWarmup.ScheduleAfterSubmit("FINANCE_PAYMENT", id, userId);
                return Ok(new { success = true, message = "提交审核成功" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交付款单审核失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>审核通过（2 -> 10）</summary>
        [HttpPost("{id}/approve")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Approve(string id, [FromBody] FinancePaymentDecisionRequest? request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, 10, request?.Remark, userId);
                return Ok(new { success = true, message = "审核通过" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审核付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>审核驳回（2 -> -1）</summary>
        [HttpPost("{id}/reject")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Reject(string id, [FromBody] FinancePaymentDecisionRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                if (string.IsNullOrWhiteSpace(request.Remark))
                    return BadRequest(new { success = false, message = "驳回原因不能为空" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, -1, request.Remark, userId);
                return Ok(new { success = true, message = "已驳回" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驳回付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>确认付款完成（10 -> 100）</summary>
        [HttpPost("{id}/complete")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Complete(string id)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, 100, remark: null, actingUserId: userId);
                return Ok(new { success = true, message = "付款完成" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认付款失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>取消付款单（1/2 -> -2）</summary>
        [HttpPost("{id}/cancel")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> Cancel(string id, [FromBody] FinancePaymentDecisionRequest? request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, -2, request?.Remark, userId);
                return Ok(new { success = true, message = "已取消" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>删除付款单</summary>
        [HttpDelete("{id}")]
        [RequirePermission("finance-payment.write")]
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
                return NotFound(new { success = false, message = "付款单不存在" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>付款反核销（100→10，回滚核销并同步采购付款状态）</summary>
        [HttpPost("{id}/reverse-verification")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> ReverseVerification(string id, [FromBody] ForceDeleteFinancePaymentRequest? body)
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
                var payment = await _service.ReverseVerificationAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(new { success = true, message = "反核销成功", data = payment });
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
                _logger.LogError(ex, "付款反核销失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>强制删除付款单（SYS_ADMIN / SYS_MANAGER）</summary>
        [HttpPost("{id}/force-delete")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> ForceDelete(string id, [FromBody] ForceDeleteFinancePaymentRequest? body)
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
                _logger.LogError(ex, "强制删除付款单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>核销付款明细</summary>
        [HttpPost("items/{paymentItemId}/verify")]
        [RequirePermission("finance-payment.write")]
        public async Task<IActionResult> VerifyItem(string paymentItemId, [FromBody] VerifyPaymentItemRequest request)
        {
            try
            {
                var denied = await RejectIfFinanceDataReadOnlyAsync();
                if (denied != null) return denied;

                await _service.VerifyPaymentItemAsync(paymentItemId, request.Amount);
                return Ok(new { success = true, message = "核销成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "核销付款明细失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<IActionResult?> RejectIfFinanceDataReadOnlyAsync(bool allowPurchaseOrderWriteBypass = false)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return StatusCode(403, new { success = false, message = "未登录或身份无效" });

            if (allowPurchaseOrderWriteBypass)
            {
                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                var hasFin = summary.PermissionCodes.Any(c =>
                    string.Equals(c, "finance-payment.write", StringComparison.OrdinalIgnoreCase));
                var hasPo = summary.PermissionCodes.Any(c =>
                    string.Equals(c, "purchase-order.write", StringComparison.OrdinalIgnoreCase));
                if (hasPo && !hasFin) return null;
            }

            if (!await FinanceDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, new { success = false, message = "主部门财务数据为只读或禁止，无法修改" });
            return null;
        }
    }

    public class FinancePaymentStatusRequest
    {
        public short Status { get; set; }
    }

    public class VerifyPaymentItemRequest
    {
        public decimal Amount { get; set; }
    }

    public class FinancePaymentDecisionRequest
    {
        public string? Remark { get; set; }
    }

    public class ForceDeleteFinancePaymentRequest
    {
        public string ConfirmBillCode { get; set; } = string.Empty;
    }
}
