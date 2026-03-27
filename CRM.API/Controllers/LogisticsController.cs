using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/logistics")]
    public class LogisticsController : ControllerBase
    {
        private readonly ILogisticsService _service;
        private readonly ILogger<LogisticsController> _logger;

        public LogisticsController(ILogisticsService service, ILogger<LogisticsController> logger)
        {
            _service = service;
            _logger = logger;
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
                return Ok(ApiResponse<IReadOnlyList<QCInfo>>.Ok(await _service.GetQcsAsync(request), "获取质检单成功"));
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
                return Ok(ApiResponse<QCInfo>.Ok(await _service.CreateQcAsync(request), "创建质检单成功"));
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
                await _service.BindQcStockInAsync(id, stockInId);
                return Ok(ApiResponse<object>.Ok(null, "绑定入库单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "绑定入库单失败");
                return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
            }
        }
    }
}
