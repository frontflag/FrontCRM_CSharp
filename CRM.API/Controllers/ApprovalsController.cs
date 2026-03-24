using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/approvals")]
    public class ApprovalsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly IQuoteService _quoteService;
        private readonly ISalesOrderService _salesOrderService;
        private readonly IFinanceReceiptService _financeReceiptService;
        private readonly IFinancePaymentService _financePaymentService;
        private readonly IRFQService _rfqService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;

        public ApprovalsController(
            IVendorService vendorService,
            IQuoteService quoteService,
            ISalesOrderService salesOrderService,
            IFinanceReceiptService financeReceiptService,
            IFinancePaymentService financePaymentService,
            IRFQService rfqService,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService)
        {
            _vendorService = vendorService;
            _quoteService = quoteService;
            _salesOrderService = salesOrderService;
            _financeReceiptService = financeReceiptService;
            _financePaymentService = financePaymentService;
            _rfqService = rfqService;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
        }

        private sealed class BizTypeConfig
        {
            public string BizType { get; set; } = string.Empty;
            public string BizTypeName { get; set; } = string.Empty;

            // PermissionCode used to decide whether user can approve records of this biz type
            public string PermissionCode { get; set; } = string.Empty;

            public short PendingStatus { get; set; }
            public short ApproveStatus { get; set; }
            public short RejectStatus { get; set; }
        }

        // Pending status mapping (based on entity model comments & existing UI mapping)
        private static readonly Dictionary<string, BizTypeConfig> BizTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["VENDOR"] = new BizTypeConfig
            {
                BizType = "VENDOR",
                BizTypeName = "供应商",
                PermissionCode = "vendor.write",
                PendingStatus = 1,
                ApproveStatus = 2,
                RejectStatus = 0
            },
            ["QUOTE"] = new BizTypeConfig
            {
                BizType = "QUOTE",
                BizTypeName = "报价单",
                PermissionCode = "rfq.write",
                PendingStatus = 1,
                ApproveStatus = 2,
                RejectStatus = 5
            },
            ["SALES_ORDER"] = new BizTypeConfig
            {
                BizType = "SALES_ORDER",
                BizTypeName = "销售订单",
                PermissionCode = "sales-order.write",
                PendingStatus = 2,  // 待审核 PendingAudit
                ApproveStatus = 10, // 审核通过 Approved
                RejectStatus = -1  // 审核失败 AuditFailed（取消=-2 由其他操作单独标记）
            },
            ["FINANCE_RECEIPT"] = new BizTypeConfig
            {
                BizType = "FINANCE_RECEIPT",
                BizTypeName = "收款单",
                PermissionCode = "finance-receipt.write",
                PendingStatus = 1,
                // 与前端 FinanceReceiptList 的“审核通过”一致：直接标记为已收款(3)
                ApproveStatus = 3,
                RejectStatus = 4
            },
            ["FINANCE_PAYMENT"] = new BizTypeConfig
            {
                BizType = "FINANCE_PAYMENT",
                BizTypeName = "付款单",
                PermissionCode = "finance-payment.write",
                PendingStatus = 2,  // 待审核
                ApproveStatus = 10, // 审核通过（应付款）
                RejectStatus = -1   // 审核失败
            }
        };

        private static bool HasApprovePermission(CRM.Core.Interfaces.UserPermissionSummaryDto summary, BizTypeConfig cfg)
        {
            return summary.IsSysAdmin || summary.PermissionCodes.Contains(cfg.PermissionCode);
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending([FromQuery] PendingApprovalsQueryRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(401, new { success = false, message = "未登录或登录态失效", errorCode = 401 });

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);

                var bizTypeFilter = request.BizType?.Trim();
                var configs = BizTypes.Values
                    .Where(c => string.IsNullOrWhiteSpace(bizTypeFilter) ||
                                c.BizType.Equals(bizTypeFilter, StringComparison.OrdinalIgnoreCase))
                    .Where(c => HasApprovePermission(summary, c))
                    .ToList();

                // Fetch pending records for each biz type, then merge & paginate
                var allItems = new List<PendingApprovalItemDto>();

                foreach (var cfg in configs)
                {
                    if (cfg.BizType.Equals("VENDOR", StringComparison.OrdinalIgnoreCase))
                    {
                        var pr = await _vendorService.GetPagedAsync(new CRM.Core.Interfaces.VendorQueryRequest
                        {
                            PageIndex = 1,
                            PageSize = 5000,
                            Status = cfg.PendingStatus,
                            CurrentUserId = userId
                        });
                        foreach (var v in pr.Items)
                        {
                            allItems.Add(new PendingApprovalItemDto
                            {
                                BizType = cfg.BizType,
                                BizTypeName = cfg.BizTypeName,
                                BusinessId = v.Id,
                                DocumentCode = v.Code,
                                Title = v.OfficialName ?? v.NickName ?? v.Code,
                                CounterpartyName = v.OfficialName ?? v.NickName ?? v.Code,
                                Amount = null,
                                Currency = null,
                                Status = v.Status,
                                CreatedAt = v.CreateTime
                            });
                        }
                    }
                    else if (cfg.BizType.Equals("QUOTE", StringComparison.OrdinalIgnoreCase))
                    {
                        var allQuotes = await _quoteService.GetAllAsync();
                        var pendingQuotes = allQuotes.Where(q => q.Status == cfg.PendingStatus).ToList();

                        // Quotes are permissioned by their RFQ; we must filter by RFQ access.
                        foreach (var q in pendingQuotes)
                        {
                            if (string.IsNullOrWhiteSpace(q.RFQId))
                                continue;

                            var rfq = await _rfqService.GetByIdAsync(q.RFQId);
                            if (rfq == null)
                                continue;

                            if (!await _dataPermissionService.CanAccessRFQAsync(userId, rfq))
                                continue;

                            allItems.Add(new PendingApprovalItemDto
                            {
                                BizType = cfg.BizType,
                                BizTypeName = cfg.BizTypeName,
                                BusinessId = q.Id,
                                DocumentCode = q.QuoteCode,
                                Title = q.Mpn ?? q.QuoteCode,
                                CounterpartyName = q.CustomerId,
                                Amount = null,
                                Currency = null,
                                Status = q.Status,
                                CreatedAt = q.CreateTime
                            });
                        }
                    }
                    else if (cfg.BizType.Equals("SALES_ORDER", StringComparison.OrdinalIgnoreCase))
                    {
                        var canViewCustomerInfo = summary.IsSysAdmin || summary.PermissionCodes.Contains("customer.info.read");
                        var canViewSalesAmount = summary.IsSysAdmin || summary.PermissionCodes.Contains("sales.amount.read");

                        var pr = await _salesOrderService.GetPagedAsync(new CRM.Core.Interfaces.SalesOrderQueryRequest
                        {
                            Page = 1,
                            PageSize = 5000,
                            Status = cfg.PendingStatus,
                            CurrentUserId = userId
                        });

                        foreach (var o in pr.Items)
                        {
                            // 非系统管理员不展示本人提交的销售订单（由上级在数据权限范围内审批）
                            if (!summary.IsSysAdmin &&
                                string.Equals(o.SalesUserId, userId, StringComparison.OrdinalIgnoreCase))
                                continue;

                            allItems.Add(new PendingApprovalItemDto
                            {
                                BizType = cfg.BizType,
                                BizTypeName = cfg.BizTypeName,
                                BusinessId = o.Id,
                                DocumentCode = o.SellOrderCode,
                                Title = o.SellOrderCode,
                                CounterpartyName = canViewCustomerInfo ? o.CustomerName : null,
                                Amount = canViewSalesAmount ? o.Total : null,
                                Currency = o.Currency,
                                Status = (short)o.Status,
                                CreatedAt = o.CreateTime
                            });
                        }
                    }
                    else if (cfg.BizType.Equals("FINANCE_RECEIPT", StringComparison.OrdinalIgnoreCase))
                    {
                        var pr = await _financeReceiptService.GetPagedAsync(new CRM.Core.Interfaces.FinanceReceiptQueryRequest
                        {
                            Page = 1,
                            PageSize = 5000,
                            Status = cfg.PendingStatus,
                            CurrentUserId = userId
                        });

                        foreach (var r in pr.Items)
                        {
                            allItems.Add(new PendingApprovalItemDto
                            {
                                BizType = cfg.BizType,
                                BizTypeName = cfg.BizTypeName,
                                BusinessId = r.Id,
                                DocumentCode = r.FinanceReceiptCode,
                                Title = r.FinanceReceiptCode,
                                CounterpartyName = r.CustomerName,
                                Amount = r.ReceiptAmount,
                                Currency = r.ReceiptCurrency,
                                Status = r.Status,
                                CreatedAt = r.CreateTime
                            });
                        }
                    }
                    else if (cfg.BizType.Equals("FINANCE_PAYMENT", StringComparison.OrdinalIgnoreCase))
                    {
                        var pr = await _financePaymentService.GetPagedAsync(new CRM.Core.Interfaces.FinancePaymentQueryRequest
                        {
                            Page = 1,
                            PageSize = 5000,
                            Status = cfg.PendingStatus,
                            CurrentUserId = userId
                        });

                        foreach (var p in pr.Items)
                        {
                            allItems.Add(new PendingApprovalItemDto
                            {
                                BizType = cfg.BizType,
                                BizTypeName = cfg.BizTypeName,
                                BusinessId = p.Id,
                                DocumentCode = p.FinancePaymentCode,
                                Title = p.FinancePaymentCode,
                                CounterpartyName = p.VendorName,
                                Amount = p.PaymentAmount,
                                Currency = p.PaymentCurrency,
                                Status = p.Status,
                                CreatedAt = p.CreateTime
                            });
                        }
                    }
                }

                // Sort desc by created time, then paginate
                allItems = allItems.OrderByDescending(i => i.CreatedAt).ToList();

                var page = request.Page < 1 ? 1 : request.Page;
                var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
                var total = allItems.Count;
                var pagedItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return Ok(new
                {
                    success = true,
                    data = new PendingApprovalsPageDto
                    {
                        Items = pagedItems,
                        Total = total,
                        Page = page,
                        PageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        [HttpPost("pending/decide")]
        public async Task<IActionResult> Decide([FromBody] DecidePendingApprovalRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(401, new { success = false, message = "未登录或登录态失效", errorCode = 401 });

                if (string.IsNullOrWhiteSpace(request.BizType) || string.IsNullOrWhiteSpace(request.BusinessId))
                    return BadRequest(new { success = false, message = "BizType 和 BusinessId 为必填项", errorCode = 400 });

                if (!BizTypes.TryGetValue(request.BizType.Trim(), out var cfg))
                    return BadRequest(new { success = false, message = "未知的业务类型", errorCode = 400 });

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
                if (!HasApprovePermission(summary, cfg))
                    return StatusCode(403, new { success = false, message = "无审批权限", errorCode = 403 });

                var decision = request.Decision?.Trim().ToLowerInvariant();
                short nextStatus;
                if (decision is "approve" or "pass")
                    nextStatus = cfg.ApproveStatus;
                else if (decision is "reject" or "deny" or "refuse")
                    nextStatus = cfg.RejectStatus;
                else
                    return BadRequest(new { success = false, message = "Decision 只能为 approve 或 reject", errorCode = 400 });

                // Ensure record is pending & data permissioned, then update status.
                if (cfg.BizType.Equals("VENDOR", StringComparison.OrdinalIgnoreCase))
                {
                    var v = await _vendorService.GetByIdAsync(request.BusinessId);
                    if (v == null) return NotFound(new { success = false, message = "供应商不存在", errorCode = 404 });
                    if (v.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (!await _dataPermissionService.CanAccessVendorAsync(userId, v))
                        return StatusCode(403, new { success = false, message = "无权限访问该供应商", errorCode = 403 });
                    await _vendorService.UpdateStatusAsync(v.Id, nextStatus);
                }
                else if (cfg.BizType.Equals("QUOTE", StringComparison.OrdinalIgnoreCase))
                {
                    var q = await _quoteService.GetByIdAsync(request.BusinessId);
                    if (q == null) return NotFound(new { success = false, message = "报价单不存在", errorCode = 404 });
                    if (q.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (string.IsNullOrWhiteSpace(q.RFQId)) return Conflict(new { success = false, message = "报价单缺失 RFQ 关联信息", errorCode = 409 });

                    var rfq = await _rfqService.GetByIdAsync(q.RFQId);
                    if (rfq == null) return NotFound(new { success = false, message = "关联 RFQ 不存在", errorCode = 404 });
                    if (!await _dataPermissionService.CanAccessRFQAsync(userId, rfq))
                        return StatusCode(403, new { success = false, message = "无权限访问该报价单", errorCode = 403 });

                    await _quoteService.UpdateStatusAsync(q.Id, nextStatus);
                }
                else if (cfg.BizType.Equals("SALES_ORDER", StringComparison.OrdinalIgnoreCase))
                {
                    var o = await _salesOrderService.GetByIdAsync(request.BusinessId);
                    if (o == null) return NotFound(new { success = false, message = "销售订单不存在", errorCode = 404 });
                    if ((short)o.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (!await _dataPermissionService.CanAccessSalesOrderAsync(userId, o))
                        return StatusCode(403, new { success = false, message = "无权限访问该销售订单", errorCode = 403 });
                    if (!summary.IsSysAdmin &&
                        string.Equals(o.SalesUserId, userId, StringComparison.OrdinalIgnoreCase))
                        return Conflict(new { success = false, message = "不能审批本人提交的销售订单", errorCode = 409 });

                    var isReject = decision is "reject" or "deny" or "refuse";
                    if (isReject && string.IsNullOrWhiteSpace(request.Remark))
                        return BadRequest(new { success = false, message = "驳回时请填写原因", errorCode = 400 });

                    await _salesOrderService.UpdateStatusAsync(
                        o.Id,
                        (SellOrderMainStatus)nextStatus,
                        isReject ? request.Remark : null);
                }
                else if (cfg.BizType.Equals("FINANCE_RECEIPT", StringComparison.OrdinalIgnoreCase))
                {
                    var r = await _financeReceiptService.GetByIdAsync(request.BusinessId);
                    if (r == null) return NotFound(new { success = false, message = "收款单不存在", errorCode = 404 });
                    if (r.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (!await _dataPermissionService.CanAccessFinanceReceiptAsync(userId, r))
                        return StatusCode(403, new { success = false, message = "无权限访问该收款单", errorCode = 403 });

                    await _financeReceiptService.UpdateStatusAsync(r.Id, nextStatus);
                }
                else if (cfg.BizType.Equals("FINANCE_PAYMENT", StringComparison.OrdinalIgnoreCase))
                {
                    var p = await _financePaymentService.GetByIdAsync(request.BusinessId);
                    if (p == null) return NotFound(new { success = false, message = "付款单不存在", errorCode = 404 });
                    if (p.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (!await _dataPermissionService.CanAccessFinancePaymentAsync(userId, p))
                        return StatusCode(403, new { success = false, message = "无权限访问该付款单", errorCode = 403 });

                    var isReject = decision is "reject" or "deny" or "refuse";
                    if (isReject && string.IsNullOrWhiteSpace(request.Remark))
                        return BadRequest(new { success = false, message = "驳回时请填写原因", errorCode = 400 });

                    await _financePaymentService.UpdateStatusAsync(
                        p.Id,
                        nextStatus,
                        isReject ? request.Remark : null);
                }

                return Ok(new { success = true, data = new { }, message = "审批成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }
    }
}

