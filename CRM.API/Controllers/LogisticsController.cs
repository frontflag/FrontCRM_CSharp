using System.Security.Claims;
using System.Threading;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/logistics")]
    public class LogisticsController : ControllerBase
    {
        private readonly ILogisticsService _service;
        private readonly IArrivalNoticeListQuery _arrivalNoticeListQuery;
        private readonly IRepository<StockInNotify> _notifyRepo;
        private readonly IRepository<QCInfo> _qcRepo;
        private readonly IRepository<QCItem> _qcItemRepo;
        private readonly IRepository<StockIn> _stockInRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRbacService _rbacService;
        private readonly ILogger<LogisticsController> _logger;

        public LogisticsController(
            ILogisticsService service,
            IArrivalNoticeListQuery arrivalNoticeListQuery,
            IRepository<StockInNotify> notifyRepo,
            IRepository<QCInfo> qcRepo,
            IRepository<QCItem> qcItemRepo,
            IRepository<StockIn> stockInRepo,
            IUnitOfWork unitOfWork,
            IRbacService rbacService,
            ILogger<LogisticsController> logger)
        {
            _service = service;
            _arrivalNoticeListQuery = arrivalNoticeListQuery;
            _notifyRepo = notifyRepo;
            _qcRepo = qcRepo;
            _qcItemRepo = qcItemRepo;
            _stockInRepo = stockInRepo;
            _unitOfWork = unitOfWork;
            _rbacService = rbacService;
            _logger = logger;
        }

        public class ForceDeleteQcRequest
        {
            public string ConfirmBillCode { get; set; } = string.Empty;
        }

        [HttpGet("arrival-notices")]
        public async Task<IActionResult> GetArrivalNotices(
            [FromQuery] short? status,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] DateTime? expectedArrivalDate,
            [FromQuery] string? id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var paged = await _arrivalNoticeListQuery.GetPagedAsync(
                    status,
                    purchaseOrderCode,
                    expectedArrivalDate,
                    string.IsNullOrWhiteSpace(id) ? null : id.Trim(),
                    page,
                    pageSize,
                    cancellationToken);
                var items = paged.Items.ToList();
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyStockInNotifies(items, true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = paged.TotalCount,
                        page = paged.PageIndex,
                        pageSize = paged.PageSize
                    },
                    message = "获取到货通知成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取到货通知失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("arrival-notices")]
        public async Task<ActionResult<ApiResponse<StockInNotify>>> CreateArrivalNotice([FromBody] CreateArrivalNoticeRequest request)
        {
            try
            {
                return Ok(ApiResponse<StockInNotify>.Ok(await _service.CreateArrivalNoticeAsync(request), "创建到货通知成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建到货通知失败");
                return BadRequest(ApiResponse<StockInNotify>.Fail(ex.Message, 400));
            }
        }

        [HttpPost("arrival-notices/auto-generate")]
        public async Task<ActionResult<ApiResponse<AutoGenerateArrivalNoticeResult>>> AutoGenerateArrivalNotices()
        {
            try
            {
                return Ok(ApiResponse<AutoGenerateArrivalNoticeResult>.Ok(await _service.AutoGenerateArrivalNoticesAsync(), "批量生成到货通知成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量生成到货通知失败");
                return BadRequest(ApiResponse<AutoGenerateArrivalNoticeResult>.Fail(ex.Message, 400));
            }
        }

        [HttpPatch("arrival-notices/{id}/status")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateArrivalStatus(string id, [FromQuery] short status)
        {
            try
            {
                await _service.UpdateArrivalNoticeStatusAsync(id, status);
                return Ok(ApiResponse<object>.Ok(null, "更新到货通知状态成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新到货通知状态失败");
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
        }

        [HttpDelete("arrival-notices/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteArrivalNotice(string id)
        {
            try
            {
                var notice = await _notifyRepo.GetByIdAsync(id);
                if (notice == null)
                    return NotFound(ApiResponse<object>.Fail("到货通知不存在", 404));

                var hasQc = (await _qcRepo.FindAsync(x => x.StockInNotifyId == notice.Id)).Any();
                if (hasQc || notice.Status >= 30 || notice.ReceiveQty > 0 || notice.PassedQty > 0)
                    return BadRequest(ApiResponse<object>.Fail("该到货通知已进入质检/入库流程，不能普通删除", 400));

                await _notifyRepo.DeleteAsync(notice.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除到货通知成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除到货通知失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除到货通知失败: {ex.Message}", 500));
            }
        }

        [HttpPost("arrival-notices/{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDeleteArrivalNotice(string id, [FromBody] ForceDeleteQcRequest? body)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (summary?.IsSysAdmin != true)
                    return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员可执行强制删除", 403));

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await _service.ForceDeleteArrivalNoticeAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(ApiResponse<object>.Ok(null, "强制删除到货通知成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除到货通知失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除到货通知失败: {ex.Message}", 500));
            }
        }

        [HttpGet("qcs")]
        public async Task<IActionResult> GetQcs(
            [FromQuery] QcQueryRequest? request,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var paged = await _service.GetQcsPagedAsync(page, pageSize, request, cancellationToken);
                var items = paged.Items.ToList();
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                    PurchaseSensitiveFieldMask511.ApplyQcInfos(items, true);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        items,
                        total = paged.TotalCount,
                        page = paged.PageIndex,
                        pageSize = paged.PageSize
                    },
                    message = "获取质检单成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取质检单失败");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("qcs")]
        public async Task<ActionResult<ApiResponse<QCInfo>>> CreateQc([FromBody] CreateQcRequest request)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return Ok(ApiResponse<QCInfo>.Ok(await _service.CreateQcAsync(request, actorId), "创建质检单成功"));
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "创建质检单保存失败");
                return BadRequest(ApiResponse<QCInfo>.Fail(msg, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建质检单失败");
                return BadRequest(ApiResponse<QCInfo>.Fail(ex.Message, 400));
            }
        }

        [HttpPatch("qcs/{id}/result")]
        public async Task<ActionResult<ApiResponse<QCInfo>>> UpdateQcResult(string id, [FromBody] UpdateQcResultRequest request)
        {
            try
            {
                return Ok(ApiResponse<QCInfo>.Ok(await _service.UpdateQcResultAsync(id, request), "更新质检结果成功"));
            }
            catch (DbUpdateException ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "更新质检结果保存失败");
                return BadRequest(ApiResponse<QCInfo>.Fail(msg, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新质检结果失败");
                return BadRequest(ApiResponse<QCInfo>.Fail(ex.Message, 400));
            }
        }

        [HttpPatch("qcs/{id}/bind-stock-in")]
        public async Task<ActionResult<ApiResponse<object>>> BindStockIn(string id, [FromQuery] string stockInId)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _service.BindQcStockInAsync(id, stockInId, actorId);
                return Ok(ApiResponse<object>.Ok(null, "绑定入库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "绑定入库单失败");
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
        }

        [HttpDelete("qcs/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteQc(string id)
        {
            try
            {
                var qc = await _qcRepo.GetByIdAsync(id);
                if (qc == null)
                    return NotFound(ApiResponse<object>.Fail("质检单不存在", 404));

                if (!string.IsNullOrWhiteSpace(qc.StockInId) || qc.StockInStatus >= 100)
                    return BadRequest(ApiResponse<object>.Fail("该质检单已关联入库，不能普通删除", 400));

                var items = (await _qcItemRepo.FindAsync(x => x.QcInfoId == qc.Id)).ToList();
                foreach (var item in items)
                    await _qcItemRepo.DeleteAsync(item.Id);

                await _qcRepo.DeleteAsync(qc.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "删除质检单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除质检单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除质检单失败: {ex.Message}", 500));
            }
        }

        [HttpPost("qcs/{id}/force-delete")]
        public async Task<ActionResult<ApiResponse<object>>> ForceDeleteQc(string id, [FromBody] ForceDeleteQcRequest? body)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
                if (summary?.IsSysAdmin != true)
                    return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员可执行强制删除", 403));

                if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                    return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                await _service.ForceDeleteQcAsync(
                    id,
                    body.ConfirmBillCode.Trim(),
                    userId.Trim(),
                    string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

                return Ok(ApiResponse<object>.Ok(null, "强制删除质检单成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除质检单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除质检单失败: {ex.Message}", 500));
            }
        }
    }
}
