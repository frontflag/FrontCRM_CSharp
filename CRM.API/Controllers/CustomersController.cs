using Microsoft.AspNetCore.Mvc;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetCustomers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] short? customerType = null,
            [FromQuery] short? customerLevel = null,
            [FromQuery] string? industry = null,
            [FromQuery] string? region = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool? sortDescending = null)
        {
            try
            {
                var request = new CustomerQueryRequest
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Keyword = searchTerm,
                    Type = customerType,
                    Level = customerLevel,
                    Status = isActive.HasValue ? (isActive.Value ? (short)1 : (short)0) : null
                };

                var result = await _customerService.GetCustomersPagedAsync(request);

                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageNumber = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages
                }, "获取客户列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取客户列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CustomerInfo>>> GetCustomerById(string id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return NotFound(ApiResponse<CustomerInfo>.Fail("客户不存在", 404));
                }
                return Ok(ApiResponse<CustomerInfo>.Ok(customer, "获取客户详情成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户详情失败");
                return StatusCode(500, ApiResponse<CustomerInfo>.Fail($"获取客户详情失败: {ex.Message}", 500));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CustomerInfo>>> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            try
            {
                var customer = await _customerService.CreateCustomerAsync(request);
                return Ok(ApiResponse<CustomerInfo>.Ok(customer, "创建客户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建客户失败");
                return StatusCode(500, ApiResponse<CustomerInfo>.Fail($"创建客户失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CustomerInfo>>> UpdateCustomer(string id, [FromBody] UpdateCustomerRequest request)
        {
            try
            {
                var customer = await _customerService.UpdateCustomerAsync(id, request);
                return Ok(ApiResponse<CustomerInfo>.Ok(customer, "更新客户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新客户失败");
                return StatusCode(500, ApiResponse<CustomerInfo>.Fail("更新客户失败", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCustomer(string id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                return Ok(ApiResponse<object>.Ok(null, "删除客户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除客户失败");
                return StatusCode(500, ApiResponse<object>.Fail("删除客户失败", 500));
            }
        }

        [HttpPost("{id}/activate")]
        public async Task<ActionResult<ApiResponse<object>>> ActivateCustomer(string id)
        {
            try
            {
                await _customerService.UpdateCustomerStatusAsync(id, 1);
                return Ok(ApiResponse<object>.Ok(null, "激活客户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "激活客户失败");
                return StatusCode(500, ApiResponse<object>.Fail("激活客户失败", 500));
            }
        }

        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult<ApiResponse<object>>> DeactivateCustomer(string id)
        {
            try
            {
                await _customerService.UpdateCustomerStatusAsync(id, 0);
                return Ok(ApiResponse<object>.Ok(null, "停用客户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停用客户失败");
                return StatusCode(500, ApiResponse<object>.Fail("停用客户失败", 500));
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetCustomerStatistics()
        {
            try
            {
                var customers = await _customerService.GetAllCustomersAsync();
                var totalCount = customers.Count();
                var activeCount = customers.Count(c => c.Status == 1);

                return Ok(ApiResponse<object>.Ok(new
                {
                    totalCount,
                    activeCount,
                    inactiveCount = totalCount - activeCount
                }, "获取客户统计成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户统计失败");
                return StatusCode(500, ApiResponse<object>.Fail("获取客户统计失败", 500));
            }
        }

        [HttpGet("{customerId}/contacts")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerContactInfo>>>> GetCustomerContacts(string customerId)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                {
                    return NotFound(ApiResponse<IEnumerable<CustomerContactInfo>>.Fail("客户不存在", 404));
                }
                return Ok(ApiResponse<IEnumerable<CustomerContactInfo>>.Ok(customer.Contacts ?? new List<CustomerContactInfo>(), "获取联系人列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取联系人列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<CustomerContactInfo>>.Fail("获取联系人列表失败", 500));
            }
        }

        [HttpPost("{customerId}/contacts")]
        public async Task<ActionResult<ApiResponse<CustomerContactInfo>>> AddContact(string customerId, [FromBody] AddContactRequest request)
        {
            try
            {
                _logger.LogInformation("开始添加联系人，客户ID: {CustomerId}", customerId);
                var contact = await _customerService.AddContactAsync(customerId, request);
                _logger.LogInformation("联系人添加成功: {ContactId}", contact.Id);
                return Ok(ApiResponse<CustomerContactInfo>.Ok(contact, "添加联系人成功"));
            }
            catch (Exception ex)
                {
                _logger.LogError(ex, "创建联系人时发生异常: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<CustomerContactInfo>.Fail($"创建联系人时发生错误: {ex.Message}", 500));
            }
        }

        [HttpGet("{customerId}/addresses")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerAddress>>>> GetCustomerAddresses(string customerId)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                {
                    return NotFound(ApiResponse<IEnumerable<CustomerAddress>>.Fail("客户不存在", 404));
                }
                return Ok(ApiResponse<IEnumerable<CustomerAddress>>.Ok(customer.Addresses ?? new List<CustomerAddress>(), "获取地址列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取地址列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<CustomerAddress>>.Fail("获取地址列表失败", 500));
            }
        }

        [HttpPost("{customerId}/addresses")]
        public async Task<ActionResult<ApiResponse<CustomerAddress>>> AddAddress(string customerId, [FromBody] AddAddressRequest request)
        {
            try
            {
                var address = await _customerService.AddAddressAsync(customerId, request);
                return Ok(ApiResponse<CustomerAddress>.Ok(address, "添加地址成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加地址失败");
                return StatusCode(500, ApiResponse<CustomerAddress>.Fail($"添加地址失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{customerId}/banks")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerBankInfo>>>> GetCustomerBanks(string customerId)
        {
            try
            {
                var banks = await _customerService.GetBanksByCustomerIdAsync(customerId);
                return Ok(ApiResponse<IEnumerable<CustomerBankInfo>>.Ok(banks, "获取银行信息列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取银行信息列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<CustomerBankInfo>>.Fail("获取银行信息列表失败", 500));
            }
        }

        [HttpPost("{customerId}/banks")]
        public async Task<ActionResult<ApiResponse<CustomerBankInfo>>> AddBank(string customerId, [FromBody] AddBankRequest request)
        {
            try
            {
                var bank = await _customerService.AddBankAsync(customerId, request);
                return Ok(ApiResponse<CustomerBankInfo>.Ok(bank, "添加银行信息成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加银行信息失败");
                return StatusCode(500, ApiResponse<CustomerBankInfo>.Fail($"添加银行信息失败: {ex.Message}", 500));
            }
        }
    }
}
