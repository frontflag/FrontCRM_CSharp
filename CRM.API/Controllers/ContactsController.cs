using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ContactsController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(ICustomerService customerService, ILogger<ContactsController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpPut("{contactId}")]
        public async Task<ActionResult<ApiResponse<CustomerContactInfo>>> UpdateContact(string contactId, [FromBody] UpdateContactRequest request)
        {
            try
            {
                var contact = await _customerService.UpdateContactAsync(contactId, request);
                return Ok(ApiResponse<CustomerContactInfo>.Ok(contact, "更新联系人成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新联系人失败");
                return StatusCode(500, ApiResponse<CustomerContactInfo>.Fail("更新联系人失败", 500));
            }
        }

        [HttpDelete("{contactId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteContact(string contactId)
        {
            try
            {
                await _customerService.DeleteContactAsync(contactId);
                return Ok(ApiResponse<object>.Ok(null, "删除联系人成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除联系人失败");
                return StatusCode(500, ApiResponse<object>.Fail("删除联系人失败", 500));
            }
        }

        [HttpPost("{contactId}/set-default")]
        public async Task<ActionResult<ApiResponse<object>>> SetDefaultContact(string contactId)
        {
            try
            {
                await _customerService.SetDefaultContactAsync(contactId);
                return Ok(ApiResponse<object>.Ok(null, "设置默认联系人成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置默认联系人失败");
                return StatusCode(500, ApiResponse<object>.Fail("设置默认联系人失败", 500));
            }
        }
    }
}
