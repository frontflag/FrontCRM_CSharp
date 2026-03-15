using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BanksController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<BanksController> _logger;

        public BanksController(ICustomerService customerService, ILogger<BanksController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpPut("{bankId}")]
        public async Task<ActionResult<ApiResponse<CustomerBankInfo>>> UpdateBank(string bankId, [FromBody] UpdateBankRequest request)
        {
            try
            {
                var bank = await _customerService.UpdateBankAsync(bankId, request);
                return Ok(ApiResponse<CustomerBankInfo>.Ok(bank, "更新银行信息成功"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "银行信息不存在");
                return NotFound(ApiResponse<CustomerBankInfo>.Fail("银行信息不存在", 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新银行信息失败");
                return StatusCode(500, ApiResponse<CustomerBankInfo>.Fail("更新银行信息失败", 500));
            }
        }

        [HttpDelete("{bankId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBank(string bankId)
        {
            try
            {
                await _customerService.DeleteBankAsync(bankId);
                return Ok(ApiResponse<object>.Ok(null, "删除银行信息成功"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "银行信息不存在");
                return NotFound(ApiResponse<object>.Fail("银行信息不存在", 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除银行信息失败");
                return StatusCode(500, ApiResponse<object>.Fail("删除银行信息失败", 500));
            }
        }

        [HttpPost("{bankId}/set-default")]
        public async Task<ActionResult<ApiResponse<object>>> SetDefaultBank(string bankId)
        {
            try
            {
                await _customerService.SetDefaultBankAsync(bankId);
                return Ok(ApiResponse<object>.Ok(null, "设置默认银行成功"));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "银行信息不存在");
                return NotFound(ApiResponse<object>.Fail("银行信息不存在", 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置默认银行失败");
                return StatusCode(500, ApiResponse<object>.Fail("设置默认银行失败", 500));
            }
        }
    }
}
