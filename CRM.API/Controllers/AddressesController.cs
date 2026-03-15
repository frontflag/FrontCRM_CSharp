using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AddressesController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<AddressesController> _logger;

        public AddressesController(ICustomerService customerService, ILogger<AddressesController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpPut("{addressId}")]
        public async Task<ActionResult<ApiResponse<CustomerAddress>>> UpdateAddress(string addressId, [FromBody] UpdateAddressRequest request)
        {
            try
            {
                var address = await _customerService.UpdateAddressAsync(addressId, request);
                return Ok(ApiResponse<CustomerAddress>.Ok(address, "更新地址成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新地址失败");
                return StatusCode(500, ApiResponse<CustomerAddress>.Fail("更新地址失败", 500));
            }
        }

        [HttpDelete("{addressId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAddress(string addressId)
        {
            try
            {
                await _customerService.DeleteAddressAsync(addressId);
                return Ok(ApiResponse<object>.Ok(null, "删除地址成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除地址失败");
                return StatusCode(500, ApiResponse<object>.Fail("删除地址失败", 500));
            }
        }

        [HttpPost("{addressId}/set-default")]
        public async Task<ActionResult<ApiResponse<object>>> SetDefaultAddress(string addressId)
        {
            try
            {
                await _customerService.SetDefaultAddressAsync(addressId);
                return Ok(ApiResponse<object>.Ok(null, "设置默认地址成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置默认地址失败");
                return StatusCode(500, ApiResponse<object>.Fail("设置默认地址失败", 500));
            }
        }
    }
}
