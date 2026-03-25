using CRM.API.Authorization;
using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Vendor;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/purchase-requisitions")]
    public class PurchaseRequisitionsController : ControllerBase
    {
        private readonly IPurchaseRequisitionService _service;
        private readonly IRepository<PurchaseRequisition> _prRepo;
        private readonly IRepository<SellOrder> _soRepo;
        private readonly IRepository<SellOrderItem> _soItemRepo;
        private readonly IVendorService _vendorService;
        private readonly IRepository<QuoteItem> _quoteItemRepo;
        private readonly ILogger<PurchaseRequisitionsController> _logger;
        private readonly IEntityLookupService _entityLookupService;

        public PurchaseRequisitionsController(
            IPurchaseRequisitionService service,
            IRepository<PurchaseRequisition> prRepo,
            IRepository<SellOrder> soRepo,
            IRepository<SellOrderItem> soItemRepo,
            IVendorService vendorService,
            IRepository<QuoteItem> quoteItemRepo,
            IEntityLookupService entityLookupService,
            ILogger<PurchaseRequisitionsController> logger)
        {
            _service = service;
            _prRepo = prRepo;
            _soRepo = soRepo;
            _soItemRepo = soItemRepo;
            _vendorService = vendorService;
            _quoteItemRepo = quoteItemRepo;
            _logger = logger;
            _entityLookupService = entityLookupService;
        }

        /// <summary>采购申请列表</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? keyword,
            [FromQuery] string? sellOrderId,
            [FromQuery] short? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var all = (await _prRepo.GetAllAsync()).ToList();

                if (!string.IsNullOrWhiteSpace(sellOrderId))
                    all = all.Where(p => p.SellOrderId == sellOrderId.Trim()).ToList();

                if (status.HasValue)
                    all = all.Where(p => p.Status == status.Value).ToList();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim();
                    all = all.Where(p =>
                        (p.BillCode != null && p.BillCode.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(p.PN) && p.PN.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(p.Brand) && p.Brand.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(p.Remark) && p.Remark.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var total = all.Count;
                var safePage = page < 1 ? 1 : page;
                var safePageSize = pageSize < 1 ? 20 : pageSize;
                var slice = all.OrderByDescending(p => p.CreateTime)
                    .Skip((safePage - 1) * safePageSize)
                    .Take(safePageSize)
                    .ToList();

                var soCodeById = (await _soRepo.GetAllAsync()).ToDictionary(s => s.Id, s => s.SellOrderCode);

                var items = slice.Select(p => new
                {
                    id = p.Id,
                    billCode = p.BillCode,
                    sellOrderId = p.SellOrderId,
                    sellOrderItemId = p.SellOrderItemId,
                    sellOrderCode = soCodeById.TryGetValue(p.SellOrderId, out var code) ? code : null,
                    pn = p.PN,
                    brand = p.Brand,
                    qty = p.Qty,
                    expectedPurchaseTime = p.ExpectedPurchaseTime,
                    status = p.Status,
                    type = p.Type,
                    purchaseUserId = p.PurchaseUserId,
                    quoteVendorId = p.QuoteVendorId,
                    quoteCost = p.QuoteCost,
                    remark = p.Remark,
                    createTime = p.CreateTime
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total,
                        page = safePage,
                        pageSize = safePageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购申请列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>采购申请详情</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var pr = await _prRepo.GetByIdAsync(id);
                if (pr == null) return NotFound(new { success = false, message = "采购申请不存在" });

                var so = await _soRepo.GetByIdAsync(pr.SellOrderId);
                var line = await _soItemRepo.GetByIdAsync(pr.SellOrderItemId);

                var purchaseUserName = await _entityLookupService.GetUserDisplayNameAsync(pr.PurchaseUserId);

                // 优先从报价明细（QuoteItem）匹配出供应商/联系人（即使历史 PR 的 QuoteVendorId 为 null，也能回填）
                QuoteItem? matchedQuoteItem = null;
                if (line != null && !string.IsNullOrWhiteSpace(line.QuoteId))
                {
                    var quoteItems = (await _quoteItemRepo.FindAsync(qi => qi.QuoteId == line.QuoteId)).ToList();
                    matchedQuoteItem =
                        quoteItems.FirstOrDefault(qi =>
                            qi.Mpn == line.PN &&
                            qi.Brand == line.Brand &&
                            qi.Currency == line.Currency &&
                            Math.Abs(qi.UnitPrice - line.Price) < 0.000001m &&
                            (qi.DateCode == line.DateCode || string.IsNullOrWhiteSpace(qi.DateCode) || string.IsNullOrWhiteSpace(line.DateCode))
                        ) ?? quoteItems.FirstOrDefault();
                }

                var vendorIdFromQuote = matchedQuoteItem?.VendorId ?? pr.QuoteVendorId;

                VendorInfo? vendor = null;
                if (!string.IsNullOrWhiteSpace(vendorIdFromQuote))
                    vendor = await _entityLookupService.GetVendorByIdAsync(vendorIdFromQuote);

                var intendedVendorName = matchedQuoteItem?.VendorName ?? vendor?.OfficialName ?? vendor?.NickName ?? vendor?.Code;

                // 供应商联系人：默认取主联系人，否则取第一个联系人
                string? intendedVendorContactId = null;
                string? intendedVendorContactName = null;
                if (!string.IsNullOrWhiteSpace(vendorIdFromQuote))
                {
                    // 优先用 QuoteItem 里的联系人
                    if (!string.IsNullOrWhiteSpace(matchedQuoteItem?.ContactId) || !string.IsNullOrWhiteSpace(matchedQuoteItem?.ContactName))
                    {
                        intendedVendorContactId = matchedQuoteItem?.ContactId;
                        intendedVendorContactName = matchedQuoteItem?.ContactName;
                    }

                    // 再兜底：如果 quoteItem 没填联系人，取供应商主联系人
                    if (string.IsNullOrWhiteSpace(intendedVendorContactName))
                    {
                        var contacts = (await _vendorService.GetContactsByVendorIdAsync(vendorIdFromQuote)).ToList();
                        var main = contacts.FirstOrDefault(c => c.IsMain) ?? contacts.FirstOrDefault();
                        intendedVendorContactId = intendedVendorContactId ?? main?.Id;
                        intendedVendorContactName = main?.CName ?? main?.EName ?? main?.Mobile ?? main?.Tel;
                    }
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = pr.Id,
                        billCode = pr.BillCode,
                        sellOrderId = pr.SellOrderId,
                        sellOrderItemId = pr.SellOrderItemId,
                        sellOrderCode = so?.SellOrderCode,
                        pn = pr.PN,
                        brand = pr.Brand,
                        // 图2红框内“订单明细”所需的销售订单明细字段
                        customerMaterialModel = line?.CustomerPnNo,
                        targetPrice = line?.Price,
                        currency = line?.Currency,
                        dateCode = line?.DateCode,
                        deliveryDate = pr.ExpectedPurchaseTime,
                        itemRemark = line?.Comment ?? pr.Remark,
                        qty = pr.Qty,
                        expectedPurchaseTime = pr.ExpectedPurchaseTime,
                        status = pr.Status,
                        type = pr.Type,
                        purchaseUserId = pr.PurchaseUserId,
                        purchaseUserName = purchaseUserName,
                        quoteVendorId = vendorIdFromQuote,
                        quoteCost = pr.QuoteCost != 0m ? pr.QuoteCost : (matchedQuoteItem?.UnitPrice ?? 0m),
                        intendedVendorName = intendedVendorName,
                        intendedVendorContactId = intendedVendorContactId,
                        intendedVendorContactName = intendedVendorContactName,
                        remark = pr.Remark,
                        createTime = pr.CreateTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购申请详情失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>获取“可申请采购”的销售订单明细行（用于弹窗预填）</summary>
        [HttpGet("sell-order/{sellOrderId}/line-options")]
        public async Task<IActionResult> GetSellOrderLineOptions(string sellOrderId)
        {
            try
            {
                var items = await _service.GetSellOrderLineOptionsAsync(sellOrderId);
                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购申请明细行选项失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>创建采购申请（单行）</summary>
        [HttpPost]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseRequisitionRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { success = false, message = "请求体不能为空" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(request.PurchaseUserId))
                    request.PurchaseUserId = userId;

                var created = await _service.CreateAsync(request);
                return Ok(new { success = true, data = created });
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
                _logger.LogError(ex, "创建采购申请失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>以销定采：自动生成采购申请（简化实现）</summary>
        [HttpPost("auto-generate/{sellOrderId}")]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> AutoGenerate(string sellOrderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(401, new { success = false, message = "未登录用户" });

                var created = await _service.AutoGenerateAsync(sellOrderId, userId);
                return Ok(new { success = true, data = created.ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动生成采购申请失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>重新计算（占位）</summary>
        [HttpPost("{id}/recalculate")]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> Recalculate(string id)
        {
            try
            {
                await _service.RecalculateAsync(id);
                return Ok(new { success = true, message = "已触发重算" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重算失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
#if false
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [RequirePermission("purchase-requisition.write")]
    [ApiController]
    [Route("api/v1/purchase-requisitions")]
    public class PurchaseRequisitionsController : ControllerBase
    {
        private readonly IPurchaseRequisitionService _service;
        private readonly ILogger<PurchaseRequisitionsController> _logger;

        public PurchaseRequisitionsController(IPurchaseRequisitionService service, ILogger<PurchaseRequisitionsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>获取“可申请采购”的销售订单明细行（用于弹窗预填）</summary>
        [HttpGet("sell-order/{sellOrderId}/line-options")]
        public async Task<IActionResult> GetSellOrderLineOptions(string sellOrderId)
        {
            try
            {
                var items = await _service.GetSellOrderLineOptionsAsync(sellOrderId);
                return Ok(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购申请明细行选项失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>创建采购申请（单行）</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseRequisitionRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { success = false, message = "请求体不能为空" });

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(request.PurchaseUserId))
                    request.PurchaseUserId = userId;

                var created = await _service.CreateAsync(request);
                return Ok(new { success = true, data = created });
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
                _logger.LogError(ex, "创建采购申请失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>以销定采：自动生成采购申请（简化实现）</summary>
        [HttpPost("auto-generate/{sellOrderId}")]
        public async Task<IActionResult> AutoGenerate(string sellOrderId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(401, new { success = false, message = "未登录用户" });

                var created = await _service.AutoGenerateAsync(sellOrderId, userId);
                return Ok(new { success = true, data = created.ToList() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动生成采购申请失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>重新计算（占位）</summary>
        [HttpPost("{id}/recalculate")]
        public async Task<IActionResult> Recalculate(string id)
        {
            try
            {
                await _service.RecalculateAsync(id);
                return Ok(new { success = true, message = "已触发重算" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重算失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}

using System.Security.Claims;
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [RequirePermission("purchase-requisition.read")]
    [ApiController]
    [Route("api/v1/purchase-requisitions")]
    public class PurchaseRequisitionsController : ControllerBase
    {
        private readonly IPurchaseRequisitionService _service;
        private readonly ILogger<PurchaseRequisitionsController> _logger;

        public PurchaseRequisitionsController(
            IPurchaseRequisitionService service,
            ILogger<PurchaseRequisitionsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? keyword,
            [FromQuery] string? sellOrderId,
            [FromQuery] short? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _service.GetPagedAsync(new PurchaseRequisitionQueryRequest
                {
                    Keyword = keyword,
                    SellOrderId = sellOrderId,
                    Status = status,
                    Page = page,
                    PageSize = pageSize
                });
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items = result.Items.ToList(),
                        total = result.TotalCount,
                        page = result.PageIndex,
                        pageSize = result.PageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取采购申请列表失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("sell-order/{sellOrderId}/line-options")]
        public async Task<IActionResult> GetLineOptions(string sellOrderId)
        {
            try
            {
                var list = await _service.GetLineOptionsForSellOrderAsync(sellOrderId);
                return Ok(new { success = true, data = list });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可建采购申请的销售明细失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var row = await _service.GetDetailAsync(id);
                if (row == null) return NotFound(new { success = false, message = "采购申请不存在" });
                return Ok(new { success = true, data = row });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseRequisitionRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                var dto = await _service.CreateAsync(request, userId, userName);
                return Ok(new { success = true, data = dto });
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
                _logger.LogError(ex, "创建采购申请失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePurchaseRequisitionRequest request)
        {
            try
            {
                await _service.UpdateAsync(id, request);
                var row = await _service.GetDetailAsync(id);
                return Ok(new { success = true, data = row });
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
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("auto-generate/{sellOrderId}")]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> AutoGenerate(string sellOrderId)
        {
            try
            {
                var list = await _service.AutoGenerateForSellOrderAsync(sellOrderId);
                return Ok(new { success = true, data = list });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/recalculate")]
        [RequirePermission("purchase-requisition.write")]
        public async Task<IActionResult> Recalculate(string id)
        {
            try
            {
                var row = await _service.GetDetailAsync(id);
                if (row == null) return NotFound(new { success = false, message = "采购申请不存在" });
                return Ok(new { success = true, data = row });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
#endif
