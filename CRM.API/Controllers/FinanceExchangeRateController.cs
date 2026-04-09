using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/finance/exchange-rates")]
    public class FinanceExchangeRateController : ControllerBase
    {
        private readonly IFinanceExchangeRateService _service;
        private readonly ILogger<FinanceExchangeRateController> _logger;

        public FinanceExchangeRateController(
            IFinanceExchangeRateService service,
            ILogger<FinanceExchangeRateController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// 当前生效汇率（只读）。业务侧（如采购报价折算人民币）需调用；不限于 rbac.manage，
        /// 避免已配置汇率但报价页因 403 退回默认汇率。
        /// </summary>
        [HttpGet("current")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<FinanceExchangeRateDto>>> GetCurrent(CancellationToken ct)
        {
            try
            {
                var dto = await _service.GetCurrentAsync(ct);
                return Ok(ApiResponse<FinanceExchangeRateDto>.Ok(dto, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取汇率失败");
                return StatusCode(500, ApiResponse<FinanceExchangeRateDto>.Fail("读取失败", 500));
            }
        }

        [HttpPut("current")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<FinanceExchangeRateDto>>> PutCurrent(
            [FromBody] UpdateFinanceExchangeRateRequest body,
            CancellationToken ct)
        {
            if (body == null)
                return BadRequest(ApiResponse<FinanceExchangeRateDto>.Fail("请求体为空", 400));

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                var dto = await _service.UpdateAsync(
                    body.UsdToCny,
                    body.UsdToHkd,
                    body.UsdToEur,
                    userId,
                    userName,
                    ct);
                return Ok(ApiResponse<FinanceExchangeRateDto>.Ok(dto, "保存成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<FinanceExchangeRateDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存汇率失败");
                return StatusCode(500, ApiResponse<FinanceExchangeRateDto>.Fail("保存失败", 500));
            }
        }

        [HttpGet("change-log")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<FinanceExchangeRateChangeLogPageDto>>> GetChangeLog(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            try
            {
                var (items, total) = await _service.GetChangeLogPagedAsync(page, pageSize, ct);
                return Ok(ApiResponse<FinanceExchangeRateChangeLogPageDto>.Ok(
                    new FinanceExchangeRateChangeLogPageDto
                    {
                        Items = items.ToList(),
                        TotalCount = total,
                        PageIndex = page,
                        PageSize = pageSize
                    },
                    "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取汇率变更日志失败");
                return StatusCode(500, ApiResponse<FinanceExchangeRateChangeLogPageDto>.Fail("读取失败", 500));
            }
        }
    }

    public class UpdateFinanceExchangeRateRequest
    {
        public decimal UsdToCny { get; set; }
        public decimal UsdToHkd { get; set; }
        public decimal UsdToEur { get; set; }
    }

    public class FinanceExchangeRateChangeLogPageDto
    {
        public List<FinanceExchangeRateChangeLogDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
