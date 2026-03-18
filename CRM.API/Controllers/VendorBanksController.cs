using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/vendor-banks")]
    public class VendorBanksController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly ILogger<VendorBanksController> _logger;

        public VendorBanksController(IVendorService vendorService, ILogger<VendorBanksController> logger)
        {
            _vendorService = vendorService;
            _logger = logger;
        }

        [HttpPut("{bankId}")]
        public async Task<ActionResult<ApiResponse<VendorBankInfo>>> UpdateBank(string bankId, [FromBody] UpdateVendorBankRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorBankInfo>.Fail("请求体不能为空", 400));
                var bank = await _vendorService.UpdateBankAsync(bankId, request);
                return Ok(ApiResponse<VendorBankInfo>.Ok(bank, "更新银行信息成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<VendorBankInfo>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新供应商银行信息失败");
                return StatusCode(500, ApiResponse<VendorBankInfo>.Fail($"更新供应商银行信息失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{bankId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBank(string bankId)
        {
            try
            {
                await _vendorService.DeleteBankAsync(bankId);
                return Ok(ApiResponse<object>.Ok(null, "删除银行信息成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除供应商银行信息失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除供应商银行信息失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{bankId}/set-default")]
        public async Task<ActionResult<ApiResponse<object>>> SetDefaultBank(string bankId)
        {
            try
            {
                await _vendorService.SetDefaultBankAsync(bankId);
                return Ok(ApiResponse<object>.Ok(null, "设置默认银行成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置供应商默认银行失败");
                return StatusCode(500, ApiResponse<object>.Fail($"设置供应商默认银行失败: {ex.Message}", 500));
            }
        }
    }
}

