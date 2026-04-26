using System.Security.Claims;
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
        private readonly IRepository<QCInfo> _qcRepo;
        private readonly IRepository<QCItem> _qcItemRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRbacService _rbacService;
        private readonly ILogger<LogisticsController> _logger;

        public LogisticsController(
            ILogisticsService service,
            IRepository<QCInfo> qcRepo,
            IRepository<QCItem> qcItemRepo,
            IUnitOfWork unitOfWork,
            IRbacService rbacService,
            ILogger<LogisticsController> logger)
        {
            _service = service;
            _qcRepo = qcRepo;
            _qcItemRepo = qcItemRepo;
            _unitOfWork = unitOfWork;
            _rbacService = rbacService;
            _logger = logger;
        }

        public class ForceDeleteQcRequest
        {
            public string ConfirmBillCode { get; set; } = string.Empty;
        }

        [HttpGet("arrival-notices")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<StockInNotify>>>> GetArrivalNotices(
            [FromQuery] short? status,
            [FromQuery] string? purchaseOrderCode,
            [FromQuery] DateTime? expectedArrivalDate)
        {
            try
            {
                var list = await _service.GetArrivalNoticesAsync();

                if (status.HasValue)
                {
                    list = list.Where(x => x.Status == status.Value).ToList();
                }

                if (!string.IsNullOrWhiteSpace(purchaseOrderCode))
                {
                    var keyword = purchaseOrderCode.Trim();
                    list = list.Where(x =>
                        !string.IsNullOrWhiteSpace(x.PurchaseOrderCode) &&
                        x.PurchaseOrderCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (expectedArrivalDate.HasValue)
                {
                    var targetDate = expectedArrivalDate.Value.Date;
                    list = list.Where(x =>
                        x.ExpectedArrivalDate.HasValue &&
                        x.ExpectedArrivalDate.Value.Date == targetDate).ToList();
                }

                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    PurchaseSensitiveFieldMask511.ApplyStockInNotifies(masked, true);
                    list = masked;
                }

                return Ok(ApiResponse<IReadOnlyList<StockInNotify>>.Ok(list, "获取到货通知成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取到货通知失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<StockInNotify>>.Fail(ex.Message, 500));
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

        [HttpGet("qcs")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<QCInfo>>>> GetQcs([FromQuery] QcQueryRequest? request)
        {
            try
            {
                var list = await _service.GetQcsAsync(request);
                if (await PurchaseMaskHttp.ShouldMaskPurchase511Async(_rbacService, User))
                {
                    var masked = list.ToList();
                    PurchaseSensitiveFieldMask511.ApplyQcInfos(masked, true);
                    list = masked;
                }

                return Ok(ApiResponse<IReadOnlyList<QCInfo>>.Ok(list, "获取质检单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取质检单失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<QCInfo>>.Fail(ex.Message, 500));
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

                var qc = await _qcRepo.GetByIdAsync(id);
                if (qc == null)
                    return NotFound(ApiResponse<object>.Fail("质检单不存在", 404));

                if (!string.Equals(body.ConfirmBillCode.Trim(), qc.QcCode?.Trim(), StringComparison.Ordinal))
                    return BadRequest(ApiResponse<object>.Fail("确认单号不匹配，已拒绝删除", 400));

                var items = (await _qcItemRepo.FindAsync(x => x.QcInfoId == qc.Id)).ToList();
                foreach (var item in items)
                    await _qcItemRepo.DeleteAsync(item.Id);
                await _qcRepo.DeleteAsync(qc.Id);
                await _unitOfWork.SaveChangesAsync();
                return Ok(ApiResponse<object>.Ok(null, "强制删除质检单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制删除质检单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"强制删除质检单失败: {ex.Message}", 500));
            }
        }
    }
}
