using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/vendor-contacts")]
    public class VendorContactsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly ILogger<VendorContactsController> _logger;

        public VendorContactsController(IVendorService vendorService, ILogger<VendorContactsController> logger)
        {
            _vendorService = vendorService;
            _logger = logger;
        }

        [HttpPut("{contactId}")]
        public async Task<ActionResult<ApiResponse<VendorContactInfo>>> UpdateContact(string contactId, [FromBody] UpdateVendorContactRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorContactInfo>.Fail("请求体不能为空", 400));
                var contact = await _vendorService.UpdateContactAsync(contactId, request);
                return Ok(ApiResponse<VendorContactInfo>.Ok(contact, "更新联系人成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<VendorContactInfo>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新供应商联系人失败");
                return StatusCode(500, ApiResponse<VendorContactInfo>.Fail($"更新供应商联系人失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{contactId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteContact(string contactId)
        {
            try
            {
                await _vendorService.DeleteContactAsync(contactId);
                return Ok(ApiResponse<object>.Ok(null, "删除联系人成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除供应商联系人失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除供应商联系人失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{contactId}/set-main")]
        public async Task<ActionResult<ApiResponse<object>>> SetMainContact(string contactId)
        {
            try
            {
                await _vendorService.SetMainContactAsync(contactId);
                return Ok(ApiResponse<object>.Ok(null, "设置主联系人成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置供应商主联系人失败");
                return StatusCode(500, ApiResponse<object>.Fail($"设置供应商主联系人失败: {ex.Message}", 500));
            }
        }
    }
}
