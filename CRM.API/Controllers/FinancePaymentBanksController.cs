using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/finance/payment-banks")]
    public class FinancePaymentBanksController : ControllerBase
    {
        private readonly IFinancePaymentBankService _service;
        private readonly ILogger<FinancePaymentBanksController> _logger;

        public FinancePaymentBanksController(
            IFinancePaymentBankService service,
            ILogger<FinancePaymentBanksController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// 已启用的付款银行（请款等下拉用）。任意已登录用户可调用；不含已禁用项。
        /// </summary>
        [HttpGet("options")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<FinancePaymentBankDto>>>> ListEnabledOptions(CancellationToken ct)
        {
            try
            {
                var list = await _service.ListEnabledAsync(ct);
                return Ok(ApiResponse<List<FinancePaymentBankDto>>.Ok(list.ToList(), "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取付款银行选项失败");
                return StatusCode(500, ApiResponse<List<FinancePaymentBankDto>>.Fail("读取失败", 500));
            }
        }

        [HttpGet]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<List<FinancePaymentBankDto>>>> List(CancellationToken ct)
        {
            try
            {
                var list = await _service.ListAsync(ct);
                return Ok(ApiResponse<List<FinancePaymentBankDto>>.Ok(list.ToList(), "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取付款银行列表失败");
                return StatusCode(500, ApiResponse<List<FinancePaymentBankDto>>.Fail("读取失败", 500));
            }
        }

        [HttpPost]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<FinancePaymentBankDto>>> Create(
            [FromBody] CreateFinancePaymentBankRequest? body,
            CancellationToken ct)
        {
            if (body == null)
                return BadRequest(ApiResponse<FinancePaymentBankDto>.Fail("请求体为空", 400));
            try
            {
                var dto = await _service.CreateAsync(body.BankName, body.SortOrder, ct);
                return Ok(ApiResponse<FinancePaymentBankDto>.Ok(dto, "已新增"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<FinancePaymentBankDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增付款银行失败");
                return StatusCode(500, ApiResponse<FinancePaymentBankDto>.Fail("保存失败", 500));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("rbac.manage")]
        public async Task<ActionResult<ApiResponse<FinancePaymentBankDto>>> Update(
            [FromRoute] string id,
            [FromBody] UpdateFinancePaymentBankRequest? body,
            CancellationToken ct)
        {
            if (body == null)
                return BadRequest(ApiResponse<FinancePaymentBankDto>.Fail("请求体为空", 400));
            try
            {
                var dto = await _service.UpdateAsync(id, body.BankName, body.SortOrder, body.IsDisabled, ct);
                if (dto == null)
                    return NotFound(ApiResponse<FinancePaymentBankDto>.Fail("记录不存在", 404));
                return Ok(ApiResponse<FinancePaymentBankDto>.Ok(dto, "已保存"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<FinancePaymentBankDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新付款银行失败");
                return StatusCode(500, ApiResponse<FinancePaymentBankDto>.Fail("保存失败", 500));
            }
        }
    }

    public class CreateFinancePaymentBankRequest
    {
        public string BankName { get; set; } = string.Empty;
        public int? SortOrder { get; set; }
    }

    public class UpdateFinancePaymentBankRequest
    {
        public string BankName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsDisabled { get; set; }
    }
}
