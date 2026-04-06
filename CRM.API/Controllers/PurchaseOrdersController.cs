using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.API.Authorization;
using CRM.API.Services;
using CRM.API.Services.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("purchase-order.read")]
    [ApiController]
    [Route("api/v1/purchase-orders")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _service;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IEntityLookupService _entityLookup;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(
            IPurchaseOrderService service,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IEntityLookupService entityLookup,
            IEmailSender emailSender,
            ApplicationDbContext db,
            ILogger<PurchaseOrdersController> logger)
        {
            _service = service;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _entityLookup = entityLookup;
            _emailSender = emailSender;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new PurchaseOrderQueryRequest
                {
                    Keyword = keyword,
                    Status = status,
                    StartDate = DateTime.TryParse(startDate, out var start) ? start : null,
                    EndDate = DateTime.TryParse(endDate, out var end) ? end : null,
                    Page = page,
                    PageSize = pageSize,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _service.GetPagedAsync(request);
                var summary = await GetPermissionSummaryAsync(request.CurrentUserId);
                var items = result.Items.Select(x => MaskPurchaseOrder(x, summary)).ToList();
                return Ok(new { success = true, data = new { items, total = result.TotalCount, page = result.PageIndex, pageSize = result.PageSize } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购订单列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>采购订单报表页：一次返回订单详情（含供应商扩展字段）与公司参数，避免单独请求 company-profile/report-bundle。</summary>
        [HttpGet("{id}/report-data")]
        public async Task<IActionResult> GetReportData(string id, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                VendorContactInfo? contact = null;
                VendorInfo? vendor = null;
                var canVendor = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("vendor.info.read") ?? false);
                if (canVendor)
                {
                    if (!string.IsNullOrWhiteSpace(order.VendorContactId))
                        contact = await _entityLookup.GetVendorContactByIdAsync(order.VendorContactId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(order.VendorId))
                        vendor = await _entityLookup.GetVendorByIdAsync(order.VendorId, cancellationToken);
                }
                var companyProfile = await CompanyProfileBundleLoader.LoadAsync(_db, _logger, cancellationToken);
                var reportItemExtends = await LoadPoItemExtendsAsync(order.Items, cancellationToken);
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        order = MaskPurchaseOrder(order, summary, contact, vendor, reportItemExtends),
                        companyProfile
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });
                var summary = await GetPermissionSummaryAsync(userId);
                VendorContactInfo? contact = null;
                VendorInfo? vendor = null;
                var canVendor = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("vendor.info.read") ?? false);
                if (canVendor)
                {
                    if (!string.IsNullOrWhiteSpace(order.VendorContactId))
                        contact = await _entityLookup.GetVendorContactByIdAsync(order.VendorContactId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(order.VendorId))
                        vendor = await _entityLookup.GetVendorByIdAsync(order.VendorId, cancellationToken);
                }
                var detailItemExtends = await LoadPoItemExtendsAsync(order.Items, cancellationToken);
                return Ok(new { success = true, data = MaskPurchaseOrder(order, summary, contact, vendor, detailItemExtends) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>将当前预览的采购订单 PDF（Base64）作为附件发送至指定邮箱。</summary>
        [HttpPost("{id}/report/send-email")]
        public async Task<IActionResult> SendReportEmail(string id, [FromBody] SendPurchaseOrderReportEmailRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.To))
                    return BadRequest(new { success = false, message = "收件人邮箱不能为空" });
                if (string.IsNullOrWhiteSpace(request.PdfBase64))
                    return BadRequest(new { success = false, message = "PDF 内容不能为空" });

                var order = await _service.GetByIdAsync(id);
                if (order == null) return NotFound(new { success = false, message = "采购订单不存在" });
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, order))
                    return StatusCode(403, new { success = false, message = "无权限访问该采购订单" });

                var raw = request.PdfBase64.Trim();
                var comma = raw.IndexOf(',');
                if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
                    raw = raw[(comma + 1)..];

                byte[] pdfBytes;
                try
                {
                    pdfBytes = Convert.FromBase64String(raw);
                }
                catch (FormatException)
                {
                    return BadRequest(new { success = false, message = "PDF 数据格式无效" });
                }

                const int maxBytes = 25 * 1024 * 1024;
                if (pdfBytes.Length > maxBytes)
                    return BadRequest(new { success = false, message = "附件过大" });

                var fileName = string.IsNullOrWhiteSpace(request.FileName) ? $"{order.PurchaseOrderCode}.pdf" : request.FileName!.Trim();
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    fileName += ".pdf";

                var subject = string.IsNullOrWhiteSpace(request.Subject)
                    ? $"采购订单 {order.PurchaseOrderCode}"
                    : request.Subject!.Trim();

                await _emailSender.SendWithAttachmentAsync(
                    request.To.Trim(),
                    subject,
                    request.Body,
                    pdfBytes,
                    fileName,
                    "application/pdf",
                    cancellationToken);

                return Ok(new { success = true, message = "邮件已发送" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送采购订单报表邮件失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("by-sell-order/{sellOrderCode}")]
        public async Task<IActionResult> GetBySellOrder(string sellOrderCode)
        {
            try
            {
                var orders = await _service.GetBySellOrderCodeAsync(sellOrderCode);
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var summary = await GetPermissionSummaryAsync(userId);
                return Ok(new { success = true, data = orders.Select(x => MaskPurchaseOrder(x, summary)).ToList() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var order = await _service.CreateAsync(request, actorId);
                return CreatedAtAction(nameof(GetById), new { id = order.Id },
                    new { success = true, data = order });
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
                _logger.LogError(ex, "创建采购订单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePurchaseOrderRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var order = await _service.UpdateAsync(id, request, actorId);
                return Ok(new { success = true, data = order });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] PurchaseOrderUpdateStatusRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.UpdateStatusAsync(id, request.Status, actorId);
                return Ok(new { success = true, message = "状态更新成功" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>以销定采：根据销售订单自动生成采购订单</summary>
        [HttpPost("auto-generate/{sellOrderId}")]
        [RequirePermission("purchase-order.write")]
        public async Task<IActionResult> AutoGenerate(string sellOrderId)
        {
            try
            {
                var orders = await _service.AutoGenerateFromSellOrderAsync(sellOrderId);
                return Ok(new { success = true, data = orders, message = $"成功生成 {orders.Count()} 张采购订单" });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动生成采购订单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<UserPermissionSummaryDto?> GetPermissionSummaryAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _rbacService.GetUserPermissionSummaryAsync(userId);
        }

        private async Task<IReadOnlyDictionary<string, PurchaseOrderItemExtend>?> LoadPoItemExtendsAsync(
            ICollection<PurchaseOrderItem>? items,
            CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0) return null;
            var ids = items.Select(i => i.Id).ToList();
            var rows = await _db.PurchaseOrderItemExtends.AsNoTracking()
                .Where(e => ids.Contains(e.Id))
                .ToListAsync(cancellationToken);
            if (rows.Count == 0) return null;
            return rows.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
        }

        private object MaskPurchaseOrder(
            CRM.Core.Models.Purchase.PurchaseOrder order,
            UserPermissionSummaryDto? summary,
            VendorContactInfo? vendorContact = null,
            VendorInfo? vendor = null,
            IReadOnlyDictionary<string, PurchaseOrderItemExtend>? itemExtends = null)
        {
            var canViewVendorInfo = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("vendor.info.read") ?? false);
            var canViewPurchaseAmount = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("purchase.amount.read") ?? false);
            var canWriteFinancePayment = summary?.IsSysAdmin == true || (summary?.PermissionCodes?.Contains("finance-payment.write") ?? false);

            const short poOrderCancelled = -2;
            const short poLineCancelled = -2;
            var itemList = (order.Items ?? Enumerable.Empty<CRM.Core.Models.Purchase.PurchaseOrderItem>()).ToList();
            var poOrderCanceled = order.Status == poOrderCancelled;
            var sellLinePurchaseSum = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (!poOrderCanceled && itemList.Count > 0)
            {
                foreach (var g in itemList.GroupBy(i => (i.SellOrderItemId ?? "").Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(g.Key)) continue;
                    sellLinePurchaseSum[g.Key] = g.Where(x => x.Status != poLineCancelled).Sum(x => x.Qty);
                }
            }

            return new
            {
                order.Id,
                order.PurchaseOrderCode,
                VendorId = canViewVendorInfo ? order.VendorId : null,
                VendorName = canViewVendorInfo ? order.VendorName : null,
                VendorCode = canViewVendorInfo ? order.VendorCode : null,
                VendorContactId = canViewVendorInfo ? order.VendorContactId : null,
                VendorContactEmail = canViewVendorInfo ? vendorContact?.Email : null,
                VendorContactName = canViewVendorInfo ? (vendorContact?.CName ?? vendorContact?.EName) : null,
                VendorContactPhone = canViewVendorInfo ? (vendorContact?.Mobile ?? vendorContact?.Tel) : null,
                VendorOfficeAddress = canViewVendorInfo ? vendor?.OfficeAddress : null,
                order.PurchaseUserId,
                order.PurchaseUserName,
                order.Status,
                order.Type,
                order.Currency,
                Total = canViewPurchaseAmount ? order.Total : 0m,
                ConvertTotal = canViewPurchaseAmount ? order.ConvertTotal : 0m,
                order.ItemRows,
                order.StockStatus,
                order.FinanceStatus,
                order.StockOutStatus,
                order.InvoiceStatus,
                order.DeliveryAddress,
                order.DeliveryDate,
                order.Comment,
                order.InnerComment,
                order.CreateTime,
                order.ModifyTime,
                order.CreateByUserId,
                order.ModifyByUserId,
                Items = itemList.Select(i =>
                {
                    PurchaseOrderItemExtend? ext = null;
                    itemExtends?.TryGetValue(i.Id, out ext);
                    var soKey = (i.SellOrderItemId ?? "").Trim();
                    var sellLineSum = poOrderCanceled || string.IsNullOrEmpty(soKey)
                        ? 0m
                        : sellLinePurchaseSum.GetValueOrDefault(soKey);
                    return new
                    {
                        i.Id,
                        i.PurchaseOrderId,
                        i.PurchaseOrderItemCode,
                        i.SellOrderItemId,
                        VendorId = canViewVendorInfo ? i.VendorId : null,
                        i.ProductId,
                        i.PN,
                        i.Brand,
                        i.Qty,
                        Cost = canViewPurchaseAmount ? i.Cost : 0m,
                        ConvertPrice = canViewPurchaseAmount ? i.ConvertPrice : 0m,
                        i.Currency,
                        i.Status,
                        i.StockInStatus,
                        i.FinancePaymentStatus,
                        i.StockOutStatus,
                        i.ErrStatus,
                        i.DeliveryDate,
                        i.Comment,
                        i.InnerComment,
                        i.CreateTime,
                        i.ModifyTime,
                        purchaseProgressStatus = ext?.PurchaseProgressStatus ?? (short)0,
                        stockInProgressStatus = ext?.StockInProgressStatus ?? (short)0,
                        paymentProgressStatus = ext?.PaymentProgressStatus ?? (short)0,
                        invoiceProgressStatus = ext?.InvoiceProgressStatus ?? (short)0,
                        purchaseProgressQty = ext?.PurchaseProgressQty ?? 0m,
                        sellLinePurchaseQtySum = sellLineSum,
                        stockInProgressQty = ext?.QtyReceiveTotal ?? 0m,
                        paymentProgressAmount = canViewPurchaseAmount ? (ext?.PaymentAmountFinish ?? 0m) : 0m,
                        invoiceProgressAmount = canViewPurchaseAmount ? (ext?.PurchaseInvoiceDone ?? 0m) : 0m,
                        // 业务口径：主单已确认即可申请付款；兼容历史数据中“主单已确认但明细状态未同步到30”的情况
                        CanApplyPayment = canWriteFinancePayment
                            && i.FinancePaymentStatus < 2
                            && (i.Status == 30 || order.Status == 30)
                    };
                }).ToList()
            };
        }
    }

    public class PurchaseOrderUpdateStatusRequest
    {
        public short Status { get; set; }
    }

    public class SendPurchaseOrderReportEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string PdfBase64 { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
    }
}
