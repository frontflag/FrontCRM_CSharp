using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Vendor;
using CRM.Core.Models.Purchase;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/approvals")]
    public class ApprovalsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly ISalesOrderService _salesOrderService;
        private readonly ICustomerService _customerService;
        private readonly IFinanceReceiptService _financeReceiptService;
        private readonly IFinancePaymentService _financePaymentService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRbacService _rbacService;
        private readonly IApprovalRecordService _approvalRecordService;
        private readonly IEntityLookupService _entityLookupService;

        public ApprovalsController(
            IVendorService vendorService,
            ISalesOrderService salesOrderService,
            ICustomerService customerService,
            IFinanceReceiptService financeReceiptService,
            IFinancePaymentService financePaymentService,
            IPurchaseOrderService purchaseOrderService,
            IDataPermissionService dataPermissionService,
            IRbacService rbacService,
            IApprovalRecordService approvalRecordService,
            IEntityLookupService entityLookupService)
        {
            _vendorService = vendorService;
            _salesOrderService = salesOrderService;
            _customerService = customerService;
            _financeReceiptService = financeReceiptService;
            _financePaymentService = financePaymentService;
            _purchaseOrderService = purchaseOrderService;
            _dataPermissionService = dataPermissionService;
            _rbacService = rbacService;
            _approvalRecordService = approvalRecordService;
            _entityLookupService = entityLookupService;
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
                PendingStatus = 2,
                ApproveStatus = 10,
                RejectStatus = -1
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
            ["PURCHASE_ORDER"] = new BizTypeConfig
            {
                BizType = "PURCHASE_ORDER",
                BizTypeName = "采购订单",
                PermissionCode = "purchase-order.write",
                PendingStatus = 2,  // 待审核
                ApproveStatus = 10, // 审核通过
                RejectStatus = -1   // 审核失败
            },
            ["CUSTOMER"] = new BizTypeConfig
            {
                BizType = "CUSTOMER",
                BizTypeName = "客户",
                PermissionCode = "customer.write",
                PendingStatus = 2,  // 待审核
                ApproveStatus = 10, // 已审核
                RejectStatus = -1   // 审核失败
            },
            ["FINANCE_RECEIPT"] = new BizTypeConfig
            {
                BizType = "FINANCE_RECEIPT",
                BizTypeName = "收款",
                PermissionCode = "finance-receipt.write",
                PendingStatus = 1,
                // 与前端 FinanceReceiptList 的“审核通过”一致：直接标记为已收款(3)
                ApproveStatus = 3,
                RejectStatus = 4
            },
            ["FINANCE_PAYMENT"] = new BizTypeConfig
            {
                BizType = "FINANCE_PAYMENT",
                BizTypeName = "付款",
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

        private static short ResolveStatusByState(BizTypeConfig cfg, string? state)
        {
            var s = (state ?? "pending").Trim().ToLowerInvariant();
            return s switch
            {
                "approved" => cfg.ApproveStatus,
                "rejected" => cfg.RejectStatus,
                _ => cfg.PendingStatus
            };
        }

        private static string NormalizeState(string? state)
        {
            var s = (state ?? "pending").Trim().ToLowerInvariant();
            return s is "approved" or "rejected" ? s : "pending";
        }

        private async Task FillSubmitterFromHistoryAsync(List<PendingApprovalItemDto> items)
        {
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.Submitter))
                {
                    item.Submitter = await _entityLookupService.GetUserDisplayNameAsync(item.Submitter) ?? item.Submitter;
                    continue;
                }

                var history = await _approvalRecordService.GetHistoryAsync(item.BizType, item.BusinessId);
                var submit = history.FirstOrDefault(h => string.Equals(h.ActionType, "submit", StringComparison.OrdinalIgnoreCase));
                var fromHistory = submit?.SubmitterUserName ?? submit?.SubmitterUserId;
                if (!string.IsNullOrWhiteSpace(submit?.SubmitterUserId))
                {
                    item.Submitter = await _entityLookupService.GetUserDisplayNameAsync(submit!.SubmitterUserId) ?? fromHistory;
                }
                else
                {
                    item.Submitter = fromHistory;
                }
            }
        }

        private static string BuildVendorSummary(VendorInfo v)
            => $"供应商：{(v.OfficialName ?? v.NickName ?? v.Code)}；采购员：{(v.PurchaseUserId ?? "—")}；付款方式：{(v.PaymentMethod ?? "—")}";

        private static string BuildCustomerSummary(CustomerInfo c)
            => $"客户：{(c.OfficialName ?? c.NickName ?? c.CustomerCode)}；业务员：{(c.SalesUserId ?? "—")}；信用额度：{c.CreditLine}";

        private static string BuildSalesOrderSummary(SellOrder o)
            => $"销售订单：{o.SellOrderCode}；客户：{(o.CustomerName ?? o.CustomerId ?? "—")}；金额：{o.Total}";

        private static string BuildFinanceReceiptSummary(FinanceReceipt r)
            => $"收款单：{r.FinanceReceiptCode}；客户：{(r.CustomerName ?? r.CustomerId ?? "—")}；金额：{r.ReceiptAmount}";

        private static string BuildFinancePaymentSummary(FinancePayment p)
            => $"付款单：{p.FinancePaymentCode}；供应商：{(p.VendorName ?? p.VendorId ?? "—")}；金额：{p.PaymentAmount}";

        private static string BuildPurchaseOrderSummary(PurchaseOrder o)
            => $"采购订单：{o.PurchaseOrderCode}；供应商：{(o.VendorName ?? o.VendorId ?? "—")}；金额：{o.Total}";

        private async Task<List<PendingApprovalItemDto>> QueryApprovalItemsByStateAsync(
            string userId,
            CRM.Core.Interfaces.UserPermissionSummaryDto summary,
            List<BizTypeConfig> configs,
            string state)
        {
            var allItems = new List<PendingApprovalItemDto>();

            foreach (var cfg in configs)
            {
                var targetStatus = ResolveStatusByState(cfg, state);
                var isPendingState = targetStatus == cfg.PendingStatus;
                if (cfg.BizType.Equals("VENDOR", StringComparison.OrdinalIgnoreCase))
                {
                    var pr = await _vendorService.GetPagedAsync(new CRM.Core.Interfaces.VendorQueryRequest
                    {
                        PageIndex = 1,
                        PageSize = 5000,
                        Status = targetStatus,
                        CurrentUserId = userId
                    });
                    foreach (var v in pr.Items)
                    {
                        if (isPendingState && !summary.IsSysAdmin &&
                            string.Equals(v.PurchaseUserId, userId, StringComparison.OrdinalIgnoreCase))
                            continue;
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
                            Submitter = v.PurchaseUserId ?? v.CreateUserId?.ToString(),
                            Status = v.Status,
                            CreatedAt = v.CreateTime
                        });
                    }
                }
                else if (cfg.BizType.Equals("CUSTOMER", StringComparison.OrdinalIgnoreCase))
                {
                    var pr = await _customerService.GetCustomersPagedAsync(new CRM.Core.Interfaces.CustomerQueryRequest
                    {
                        PageIndex = 1,
                        PageSize = 5000,
                        Status = targetStatus,
                        CurrentUserId = userId
                    });

                    foreach (var c in pr.Items)
                    {
                        if (isPendingState && !summary.IsSysAdmin &&
                            string.Equals(c.SalesUserId, userId, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (!await _dataPermissionService.CanAccessCustomerAsync(userId, c))
                            continue;

                        allItems.Add(new PendingApprovalItemDto
                        {
                            BizType = cfg.BizType,
                            BizTypeName = cfg.BizTypeName,
                            BusinessId = c.Id,
                            DocumentCode = c.CustomerCode,
                            Title = c.OfficialName ?? c.NickName ?? c.CustomerCode,
                            CounterpartyName = c.OfficialName ?? c.NickName ?? c.CustomerCode,
                            Amount = null,
                            Currency = null,
                            Submitter = c.SalesUserId ?? c.CreateUserId?.ToString(),
                            Status = c.Status,
                            CreatedAt = c.CreateTime
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
                        Status = targetStatus,
                        CurrentUserId = userId
                    });

                    foreach (var o in pr.Items)
                    {
                        if (isPendingState && !summary.IsSysAdmin &&
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
                            Submitter = o.SalesUserId ?? o.CreateUserId?.ToString(),
                            Status = (short)o.Status,
                            CreatedAt = o.CreateTime
                        });
                    }
                }
                else if (cfg.BizType.Equals("PURCHASE_ORDER", StringComparison.OrdinalIgnoreCase))
                {
                    var canViewVendorInfo = summary.IsSysAdmin || summary.PermissionCodes.Contains("vendor.info.read");
                    var canViewPurchaseAmount = summary.IsSysAdmin || summary.PermissionCodes.Contains("purchase.amount.read");

                    var pr = await _purchaseOrderService.GetPagedAsync(new CRM.Core.Interfaces.PurchaseOrderQueryRequest
                    {
                        Page = 1,
                        PageSize = 5000,
                        Status = targetStatus,
                        CurrentUserId = userId
                    });

                    foreach (var o in pr.Items)
                    {
                        if (isPendingState && !summary.IsSysAdmin &&
                            string.Equals(o.PurchaseUserId, userId, StringComparison.OrdinalIgnoreCase))
                            continue;

                        allItems.Add(new PendingApprovalItemDto
                        {
                            BizType = cfg.BizType,
                            BizTypeName = cfg.BizTypeName,
                            BusinessId = o.Id,
                            DocumentCode = o.PurchaseOrderCode,
                            Title = o.PurchaseOrderCode,
                            CounterpartyName = canViewVendorInfo ? o.VendorName : null,
                            Amount = canViewPurchaseAmount ? o.Total : null,
                            Currency = o.Currency,
                            Submitter = o.PurchaseUserId ?? o.CreateUserId?.ToString(),
                            Status = o.Status,
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
                        Status = targetStatus,
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
                            Submitter = r.ReceiptUserId ?? r.SalesUserId ?? r.CreateUserId?.ToString(),
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
                        Status = targetStatus,
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
                            Submitter = p.PaymentUserId ?? p.CreateUserId?.ToString(),
                            Status = p.Status,
                            CreatedAt = p.CreateTime
                        });
                    }
                }
            }

            return allItems.OrderByDescending(i => i.CreatedAt).ToList();
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending([FromQuery] PendingApprovalsQueryRequest request)
        {
            request.State = "pending";
            return await GetByState(request);
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetByState([FromQuery] PendingApprovalsQueryRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(401, new { success = false, message = "未登录或登录态失效", errorCode = 401 });

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
                var state = NormalizeState(request.State);

                var bizTypeFilter = request.BizType?.Trim();
                var configs = BizTypes.Values
                    .Where(c => string.IsNullOrWhiteSpace(bizTypeFilter) ||
                                c.BizType.Equals(bizTypeFilter, StringComparison.OrdinalIgnoreCase))
                    .Where(c => HasApprovePermission(summary, c))
                    .ToList();

                var allItems = await QueryApprovalItemsByStateAsync(userId, summary, configs, state);

                var page = request.Page < 1 ? 1 : request.Page;
                var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
                var total = allItems.Count;
                var pagedItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                await FillSubmitterFromHistoryAsync(pagedItems);

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

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] PendingApprovalsQueryRequest request)
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

                var pendingCount = (await QueryApprovalItemsByStateAsync(userId, summary, configs, "pending")).Count;
                var approvedCount = (await QueryApprovalItemsByStateAsync(userId, summary, configs, "approved")).Count;
                var rejectedCount = (await QueryApprovalItemsByStateAsync(userId, summary, configs, "rejected")).Count;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        pendingCount,
                        approvedCount,
                        rejectedCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message, errorCode = 500 });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string bizType, [FromQuery] string businessId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bizType) || string.IsNullOrWhiteSpace(businessId))
                    return BadRequest(new { success = false, message = "bizType/businessId 不能为空", errorCode = 400 });
                var items = await _approvalRecordService.GetHistoryAsync(bizType.Trim(), businessId.Trim());
                return Ok(new
                {
                    success = true,
                    data = items.Select(x => new
                    {
                        x.Id,
                        x.BizType,
                        x.BusinessId,
                        x.DocumentCode,
                        x.ItemDescription,
                        x.ActionType,
                        x.FromStatus,
                        x.ToStatus,
                        x.SubmitRemark,
                        x.AuditRemark,
                        x.SubmitterUserId,
                        x.SubmitterUserName,
                        x.ApproverUserId,
                        x.ApproverUserName,
                        x.ActionTime
                    }).ToList()
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
                    if (!summary.IsSysAdmin &&
                        string.Equals(v.PurchaseUserId, userId, StringComparison.OrdinalIgnoreCase))
                        return Conflict(new { success = false, message = "不能审批本人提交的供应商", errorCode = 409 });
                    var isReject = decision is "reject" or "deny" or "refuse";
                    if (isReject && string.IsNullOrWhiteSpace(request.Remark))
                        return BadRequest(new { success = false, message = "驳回时请填写原因", errorCode = 400 });
                    await _vendorService.UpdateStatusAsync(v.Id, nextStatus, isReject ? request.Remark : null);
                    await _approvalRecordService.RecordDecisionAsync(
                        cfg.BizType, v.Id, v.Code, BuildVendorSummary(v), v.Status, nextStatus, isReject ? "reject" : "approve",
                        isReject ? request.Remark : null, userId, User.Identity?.Name);
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
                    await _approvalRecordService.RecordDecisionAsync(
                        cfg.BizType, o.Id, o.SellOrderCode, BuildSalesOrderSummary(o), (short)o.Status, nextStatus, isReject ? "reject" : "approve",
                        isReject ? request.Remark : null, userId, User.Identity?.Name);
                }
                else if (cfg.BizType.Equals("PURCHASE_ORDER", StringComparison.OrdinalIgnoreCase))
                {
                    var o = await _purchaseOrderService.GetByIdAsync(request.BusinessId);
                    if (o == null) return NotFound(new { success = false, message = "采购订单不存在", errorCode = 404 });
                    if (o.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (!await _dataPermissionService.CanAccessPurchaseOrderAsync(userId, o))
                        return StatusCode(403, new { success = false, message = "无权限访问该采购订单", errorCode = 403 });
                    if (!summary.IsSysAdmin &&
                        string.Equals(o.PurchaseUserId, userId, StringComparison.OrdinalIgnoreCase))
                        return Conflict(new { success = false, message = "不能审批本人提交的采购订单", errorCode = 409 });

                    var isReject = decision is "reject" or "deny" or "refuse";
                    if (isReject && string.IsNullOrWhiteSpace(request.Remark))
                        return BadRequest(new { success = false, message = "驳回时请填写原因", errorCode = 400 });

                    await _purchaseOrderService.UpdateStatusAsync(o.Id, nextStatus);
                    await _approvalRecordService.RecordDecisionAsync(
                        cfg.BizType, o.Id, o.PurchaseOrderCode, BuildPurchaseOrderSummary(o), o.Status, nextStatus, isReject ? "reject" : "approve",
                        isReject ? request.Remark : null, userId, User.Identity?.Name);
                }
                else if (cfg.BizType.Equals("CUSTOMER", StringComparison.OrdinalIgnoreCase))
                {
                    var c = await _customerService.GetCustomerByIdAsync(request.BusinessId);
                    if (c == null) return NotFound(new { success = false, message = "客户不存在", errorCode = 404 });
                    if (c.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (!await _dataPermissionService.CanAccessCustomerAsync(userId, c))
                        return StatusCode(403, new { success = false, message = "无权限访问该客户", errorCode = 403 });
                    if (!summary.IsSysAdmin &&
                        string.Equals(c.SalesUserId, userId, StringComparison.OrdinalIgnoreCase))
                        return Conflict(new { success = false, message = "不能审批本人提交的客户", errorCode = 409 });

                    var isReject = decision is "reject" or "deny" or "refuse";
                    if (isReject && string.IsNullOrWhiteSpace(request.Remark))
                        return BadRequest(new { success = false, message = "驳回时请填写原因", errorCode = 400 });

                    await _customerService.UpdateCustomerStatusAsync(c.Id, nextStatus, isReject ? request.Remark : null);
                    await _approvalRecordService.RecordDecisionAsync(
                        cfg.BizType, c.Id, c.CustomerCode, BuildCustomerSummary(c), c.Status, nextStatus, isReject ? "reject" : "approve",
                        isReject ? request.Remark : null, userId, User.Identity?.Name);
                }
                else if (cfg.BizType.Equals("FINANCE_RECEIPT", StringComparison.OrdinalIgnoreCase))
                {
                    var r = await _financeReceiptService.GetByIdAsync(request.BusinessId);
                    if (r == null) return NotFound(new { success = false, message = "收款单不存在", errorCode = 404 });
                    if (r.Status != cfg.PendingStatus) return Conflict(new { success = false, message = "该记录已不处于待审批状态", errorCode = 409 });
                    if (!await _dataPermissionService.CanAccessFinanceReceiptAsync(userId, r))
                        return StatusCode(403, new { success = false, message = "无权限访问该收款单", errorCode = 403 });

                    await _financeReceiptService.UpdateStatusAsync(r.Id, nextStatus);
                    await _approvalRecordService.RecordDecisionAsync(
                        cfg.BizType, r.Id, r.FinanceReceiptCode, BuildFinanceReceiptSummary(r), r.Status, nextStatus, decision == "approve" || decision == "pass" ? "approve" : "reject",
                        decision is "reject" or "deny" or "refuse" ? request.Remark : null, userId, User.Identity?.Name);
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
                    await _approvalRecordService.RecordDecisionAsync(
                        cfg.BizType, p.Id, p.FinancePaymentCode, BuildFinancePaymentSummary(p), p.Status, nextStatus, isReject ? "reject" : "approve",
                        isReject ? request.Remark : null, userId, User.Identity?.Name);
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

