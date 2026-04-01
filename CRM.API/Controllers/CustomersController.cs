using Microsoft.AspNetCore.Mvc;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Sales;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    // 显式小写路径，与前端 /api/v1/customers 一致，避免反向代理等环境下大小写敏感导致 404
    [Route("api/v1/customers")]
    [RequirePermission("customer.read")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IApprovalRecordService _approvalRecordService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IRepository<SellOrder> _sellOrderRepository;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(
            ICustomerService customerService,
            IApprovalRecordService approvalRecordService,
            IDataPermissionService dataPermissionService,
            IRepository<SellOrder> sellOrderRepository,
            ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _approvalRecordService = approvalRecordService;
            _dataPermissionService = dataPermissionService;
            _sellOrderRepository = sellOrderRepository;
            _logger = logger;
        }

        private static bool IsCommercialSellOrder(SellOrder o) =>
            o.Status >= SellOrderMainStatus.Approved
            && o.Status != SellOrderMainStatus.Cancelled
            && o.Status != SellOrderMainStatus.AuditFailed;

        // ─── Customer CRUD ────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetCustomers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] short? customerType = null,
            [FromQuery] short? customerLevel = null,
            [FromQuery] string? industry = null,
            [FromQuery] string? region = null,
            [FromQuery] string? salesUserId = null,
            [FromQuery] DateTime? createdFrom = null,
            [FromQuery] DateTime? createdTo = null,
            [FromQuery] bool? isActive = null,
            /// <summary>工作流状态（与 customerinfo.Status 一致）；传入时优先于 isActive 的兼容映射</summary>
            [FromQuery] short? status = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool? sortDescending = null)
        {
            try
            {
                short? statusFilter = null;
                if (status.HasValue)
                    statusFilter = status.Value;
                else if (isActive.HasValue)
                    statusFilter = isActive.Value ? (short)1 : (short)0;

                var request = new CustomerQueryRequest
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Keyword = searchTerm,
                    Type = customerType,
                    Level = customerLevel,
                    Industry = industry,
                    Region = region,
                    SalesUserId = salesUserId,
                    CreatedFrom = createdFrom,
                    CreatedTo = createdTo,
                    Status = statusFilter,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
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

        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetCustomerStatistics()
        {
            try
            {
                var customers = (await _customerService.GetAllCustomersAsync()).ToList();
                var now = DateTime.UtcNow;
                var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var totalCustomers = customers.Count;
                // 与前端 IsActive（Status >= 10）一致：已审核及以上视为活跃
                var activeCustomers = customers.Count(c => c.Status >= 10);
                var newThisMonth = customers.Count(c => c.CreateTime >= firstDayOfMonth);
                var newLast30Days = customers.Count(c => c.CreateTime >= now.AddDays(-30));
                var totalBalance = customers.Sum(c => c.CreditLineRemain);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sellOrders = (await _sellOrderRepository.GetAllAsync()).ToList();
                if (!string.IsNullOrWhiteSpace(userId))
                    sellOrders = (await _dataPermissionService.FilterSalesOrdersAsync(userId, sellOrders)).ToList();

                var commercial = sellOrders.Where(IsCommercialSellOrder).ToList();
                var customersWithDeals = commercial
                    .Select(o => o.CustomerId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .Count();

                var receivableOrders = commercial.Where(o => o.FinanceReceiptStatus < 2).ToList();
                var receivableGoodsAmount = receivableOrders.Sum(o => o.ConvertTotal);
                var receivableCustomerCount = receivableOrders
                    .Select(o => o.CustomerId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .Count();

                var pendingOutboundOrders = commercial.Where(o => o.StockOutStatus < 2).ToList();
                var pendingOutboundAmount = pendingOutboundOrders.Sum(o => o.ConvertTotal);
                var pendingOutboundCustomerCount = pendingOutboundOrders
                    .Select(o => o.CustomerId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .Count();

                var byLevel = customers
                    .GroupBy(c => c.Level)
                    .ToDictionary(g => g.Key switch { 1 => "D", 2 => "C", 3 => "B", 4 => "BPO", 5 => "VIP", 6 => "VPO", _ => "Other" }, g => g.Count());

                var byIndustry = customers
                    .Where(c => !string.IsNullOrEmpty(c.Industry))
                    .GroupBy(c => c.Industry!)
                    .ToDictionary(g => g.Key, g => g.Count());

                return Ok(ApiResponse<object>.Ok(new
                {
                    totalCustomers,
                    activeCustomers,
                    newThisMonth,
                    newLast30Days,
                    customersWithDeals,
                    totalBalance,
                    receivableGoodsAmount,
                    receivableCustomerCount,
                    pendingOutboundAmount,
                    pendingOutboundCustomerCount,
                    byLevel,
                    byIndustry
                }, "获取客户统计成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户统计失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取客户统计失败: {ex.Message}", 500));
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessCustomerAsync(userId, customer))
                    return StatusCode(403, ApiResponse<CustomerInfo>.Fail("无权限访问该客户", 403));
                return Ok(ApiResponse<CustomerInfo>.Ok(customer, "获取客户详情成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户详情失败");
                return StatusCode(500, ApiResponse<CustomerInfo>.Fail($"获取客户详情失败: {ex.Message}", 500));
            }
        }

        [HttpPost]
        [RequirePermission("customer.write")]
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

        /// <summary>Excel 批量导入客户（前端解析 Excel 后提交）</summary>
        [HttpPost("import/batch")]
        [RequirePermission("customer.write")]
        public async Task<ActionResult<ApiResponse<CustomerImportBatchResult>>> ImportCustomersBatch([FromBody] CustomerImportBatchRequest request)
        {
            try
            {
                if (request?.Items == null || request.Items.Count == 0)
                    return BadRequest(ApiResponse<CustomerImportBatchResult>.Fail("导入数据为空", 400));

                var result = await _customerService.ImportCustomersBatchAsync(request);
                var msg = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailCount} 条";
                return Ok(ApiResponse<CustomerImportBatchResult>.Ok(result, msg));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入客户失败");
                return StatusCode(500, ApiResponse<CustomerImportBatchResult>.Fail($"批量导入失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("customer.write")]
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
                return StatusCode(500, ApiResponse<CustomerInfo>.Fail($"更新客户失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{id}")]
        [RequirePermission("customer.write")]
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
                return StatusCode(500, ApiResponse<object>.Fail($"删除客户失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/activate")]
        public async Task<ActionResult<ApiResponse<object>>> ActivateCustomer(string id)
        {
            try
            {
                // 新状态体系：激活等价于将客户置为“已审核”(10)
                await _customerService.UpdateCustomerStatusAsync(id, 10);
                return Ok(ApiResponse<object>.Ok(null, "激活客户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "激活客户失败");
                return StatusCode(500, ApiResponse<object>.Fail($"激活客户失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult<ApiResponse<object>>> DeactivateCustomer(string id)
        {
            try
            {
                // 新状态体系不再使用 0：停用回到“新建”(1)
                await _customerService.UpdateCustomerStatusAsync(id, 1);
                return Ok(ApiResponse<object>.Ok(null, "停用客户成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停用客户失败");
                return StatusCode(500, ApiResponse<object>.Fail($"停用客户失败: {ex.Message}", 500));
            }
        }

        /// <summary>提交审核：新建(1) -> 待审核(2)</summary>
        [HttpPost("{id}/submit-audit")]
        [RequirePermission("customer.write")]
        public async Task<ActionResult<ApiResponse<object>>> SubmitAudit(string id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                    return NotFound(ApiResponse<object>.Fail("客户不存在", 404));
                var before = customer.Status;
                await _customerService.UpdateCustomerStatusAsync(id, 2);
                await _approvalRecordService.RecordSubmitAsync(
                    "CUSTOMER",
                    customer.Id,
                    customer.CustomerCode,
                    $"客户：{(customer.OfficialName ?? customer.NickName ?? customer.CustomerCode)}；业务员：{(customer.SalesUserId ?? "—")}；信用额度：{customer.CreditLine}",
                    before,
                    2,
                    null,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    User.Identity?.Name);
                return Ok(ApiResponse<object>.Ok(null, "提交审核成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交审核失败");
                return StatusCode(500, ApiResponse<object>.Fail($"提交审核失败: {ex.Message}", 500));
            }
        }

        // ─── Contact History ──────────────────────────────────────────────────────

        [HttpGet("{customerId}/contact-history")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetCustomerContactHistory(string customerId)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return NotFound(ApiResponse<IEnumerable<object>>.Fail("客户不存在", 404));

                var list = await _customerService.GetContactHistoryAsync(customerId);
                var items = list.Select(h => new
                {
                    id = h.Id,
                    customerId = h.CustomerId,
                    type = h.Type,
                    subject = h.Subject,
                    content = h.Content,
                    contactPerson = h.ContactPerson,
                    time = h.Time.ToString("o"),
                    nextFollowUpTime = h.NextFollowUpTime.HasValue ? h.NextFollowUpTime.Value.ToString("o") : null,
                    result = h.Result,
                    createTime = h.CreateTime.ToString("o")
                });
                return Ok(ApiResponse<IEnumerable<object>>.Ok(items.ToList(), "获取联系历史成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取联系历史失败");
                return StatusCode(500, ApiResponse<IEnumerable<object>>.Fail($"获取联系历史失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{customerId}/contact-history")]
        public async Task<ActionResult<ApiResponse<object>>> AddContactHistory(string customerId, [FromBody] AddContactHistoryRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                var record = await _customerService.AddContactHistoryAsync(customerId, request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    id = record.Id,
                    customerId = record.CustomerId,
                    type = record.Type,
                    subject = record.Subject,
                    content = record.Content,
                    contactPerson = record.ContactPerson,
                    time = record.Time.ToString("o"),
                    nextFollowUpTime = record.NextFollowUpTime.HasValue ? record.NextFollowUpTime.Value.ToString("o") : null,
                    result = record.Result,
                    createTime = record.CreateTime.ToString("o")
                }, "添加联系记录成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加联系记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"添加联系记录失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{customerId}/contact-history/{historyId}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateContactHistory(string customerId, string historyId, [FromBody] UpdateContactHistoryRequest request)
        {
            try
            {
                var record = await _customerService.UpdateContactHistoryAsync(historyId, request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    id = record.Id,
                    customerId = record.CustomerId,
                    type = record.Type,
                    subject = record.Subject,
                    content = record.Content,
                    contactPerson = record.ContactPerson,
                    time = record.Time.ToString("o"),
                    nextFollowUpTime = record.NextFollowUpTime.HasValue ? record.NextFollowUpTime.Value.ToString("o") : null,
                    result = record.Result,
                    createTime = record.CreateTime.ToString("o")
                }, "更新联系记录成功"));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新联系记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新联系记录失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{customerId}/contact-history/{historyId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteContactHistory(string customerId, string historyId)
        {
            try
            {
                await _customerService.DeleteContactHistoryAsync(historyId);
                return Ok(ApiResponse<object>.Ok(null, "删除联系记录成功"));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除联系记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除联系记录失败: {ex.Message}", 500));
            }
        }



        // ─── Contacts ─────────────────────────────────────────────────────────────

        [HttpGet("{customerId}/contacts")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerContactInfo>>>> GetCustomerContacts(string customerId)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return NotFound(ApiResponse<IEnumerable<CustomerContactInfo>>.Fail("客户不存在", 404));

                return Ok(ApiResponse<IEnumerable<CustomerContactInfo>>.Ok(
                    customer.Contacts ?? new List<CustomerContactInfo>(), "获取联系人列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取联系人列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<CustomerContactInfo>>.Fail($"获取联系人列表失败: {ex.Message}", 500));
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

        [HttpPut("{customerId}/contacts/{contactId}")]
        public async Task<ActionResult<ApiResponse<CustomerContactInfo>>> UpdateContact(
            string customerId, string contactId, [FromBody] UpdateContactRequest request)
        {
            try
            {
                var contact = await _customerService.UpdateContactAsync(contactId, request);
                return Ok(ApiResponse<CustomerContactInfo>.Ok(contact, "更新联系人成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CustomerContactInfo>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新联系人失败");
                return StatusCode(500, ApiResponse<CustomerContactInfo>.Fail($"更新联系人失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{customerId}/contacts/{contactId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteContact(string customerId, string contactId)
        {
            try
            {
                await _customerService.DeleteContactAsync(contactId);
                return Ok(ApiResponse<object>.Ok(null, "删除联系人成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除联系人失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除联系人失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{customerId}/contacts/{contactId}/set-default")]
        public async Task<ActionResult<ApiResponse<object>>> SetDefaultContact(string customerId, string contactId)
        {
            try
            {
                await _customerService.SetDefaultContactAsync(contactId);
                return Ok(ApiResponse<object>.Ok(null, "设置默认联系人成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置默认联系人失败");
                return StatusCode(500, ApiResponse<object>.Fail($"设置默认联系人失败: {ex.Message}", 500));
            }
        }

        // ─── Addresses ────────────────────────────────────────────────────────────

        [HttpGet("{customerId}/addresses")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerAddress>>>> GetCustomerAddresses(string customerId)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                    return NotFound(ApiResponse<IEnumerable<CustomerAddress>>.Fail("客户不存在", 404));

                return Ok(ApiResponse<IEnumerable<CustomerAddress>>.Ok(
                    customer.Addresses ?? new List<CustomerAddress>(), "获取地址列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取地址列表失败");
                return StatusCode(500, ApiResponse<IEnumerable<CustomerAddress>>.Fail($"获取地址列表失败: {ex.Message}", 500));
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

        [HttpPut("{customerId}/addresses/{addressId}")]
        public async Task<ActionResult<ApiResponse<CustomerAddress>>> UpdateAddress(
            string customerId, string addressId, [FromBody] UpdateAddressRequest request)
        {
            try
            {
                var address = await _customerService.UpdateAddressAsync(addressId, request);
                return Ok(ApiResponse<CustomerAddress>.Ok(address, "更新地址成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CustomerAddress>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新地址失败");
                return StatusCode(500, ApiResponse<CustomerAddress>.Fail($"更新地址失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{customerId}/addresses/{addressId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteAddress(string customerId, string addressId)
        {
            try
            {
                await _customerService.DeleteAddressAsync(addressId);
                return Ok(ApiResponse<object>.Ok(null, "删除地址成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除地址失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除地址失败: {ex.Message}", 500));
            }
        }

        // ─── Banks ────────────────────────────────────────────────────────────────

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
                return StatusCode(500, ApiResponse<IEnumerable<CustomerBankInfo>>.Fail($"获取银行信息列表失败: {ex.Message}", 500));
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

        [HttpPut("{customerId}/banks/{bankId}")]
        public async Task<ActionResult<ApiResponse<CustomerBankInfo>>> UpdateBank(
            string customerId, string bankId, [FromBody] UpdateBankRequest request)
        {
            try
            {
                var bank = await _customerService.UpdateBankAsync(bankId, request);
                return Ok(ApiResponse<CustomerBankInfo>.Ok(bank, "更新银行信息成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CustomerBankInfo>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新银行信息失败");
                return StatusCode(500, ApiResponse<CustomerBankInfo>.Fail($"更新银行信息失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{customerId}/banks/{bankId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteBank(string customerId, string bankId)
        {
            try
            {
                await _customerService.DeleteBankAsync(bankId);
                return Ok(ApiResponse<object>.Ok(null, "删除银行信息成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除银行信息失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除银行信息失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{customerId}/banks/{bankId}/set-default")]
        public async Task<ActionResult<ApiResponse<object>>> SetDefaultBank(string customerId, string bankId)
        {
            try
            {
                await _customerService.SetDefaultBankAsync(bankId);
                return Ok(ApiResponse<object>.Ok(null, "设置黑名单客户成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置黑名单客户失败");
                return StatusCode(500, ApiResponse<object>.Fail($"设置黑名单客户失败: {ex.Message}", 500));
            }
        }

        // ===== 删除带理由 =====
        [HttpDelete("{id}/with-reason")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteWithReason(string id, [FromBody] DeleteCustomerRequest request)
        {
            try
            {
                await _customerService.DeleteCustomerWithReasonAsync(id, request.Reason, null, "系统用户");
                return Ok(ApiResponse<object>.Ok(null, "客户已删除"));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        // ===== 黑名单带理由 =====
        [HttpPost("{id}/blacklist")]
        public async Task<ActionResult<ApiResponse<object>>> SetBlackList(string id, [FromBody] SetBlackListRequest request)
        {
            try
            {
                await _customerService.SetBlackListAsync(id, request.Reason, null, "系统用户");
                return Ok(ApiResponse<object>.Ok(null, "已加入黑名单"));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        // ===== 移出黑名单（需原因，写入操作日志） =====
        [HttpPost("{id}/remove-blacklist")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveBlackList(string id, [FromBody] RemoveBlackListRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest(ApiResponse<object>.Fail("请填写移出黑名单原因", 400));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity?.Name;
                await _customerService.RemoveFromBlackListAsync(id, request.Reason.Trim(), userId, userName);
                return Ok(ApiResponse<object>.Ok(null, "已移出黑名单"));
            }
            catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message, 400)); }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        /// <summary>冻结客户（禁用），原因写入操作日志</summary>
        [HttpPost("{id}/freeze")]
        [RequirePermission("customer.write")]
        public async Task<ActionResult<ApiResponse<object>>> FreezeCustomer(string id, [FromBody] FreezeCustomerRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest(ApiResponse<object>.Fail("请填写冻结原因", 400));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity?.Name;
                await _customerService.FreezeCustomerAsync(id, request.Reason, userId, userName);
                return Ok(ApiResponse<object>.Ok(null, "客户已冻结"));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message, 400)); }
            catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message, 400)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "冻结客户失败");
                return StatusCode(500, ApiResponse<object>.Fail($"冻结客户失败: {ex.Message}", 500));
            }
        }

        /// <summary>启用客户（解除冻结），原因写入操作日志</summary>
        [HttpPost("{id}/unfreeze")]
        [RequirePermission("customer.write")]
        public async Task<ActionResult<ApiResponse<object>>> UnfreezeCustomer(string id, [FromBody] FreezeCustomerRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest(ApiResponse<object>.Fail("请填写启用原因", 400));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.Identity?.Name;
                await _customerService.UnfreezeCustomerAsync(id, request.Reason, userId, userName);
                return Ok(ApiResponse<object>.Ok(null, "客户已启用"));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message, 400)); }
            catch (ArgumentException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message, 400)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用客户失败");
                return StatusCode(500, ApiResponse<object>.Fail($"启用客户失败: {ex.Message}", 500));
            }
        }

        // ===== 恢复已删除客户 =====
        [HttpPost("{id}/restore")]
        public async Task<ActionResult<ApiResponse<object>>> RestoreCustomer(string id)
        {
            try
            {
                await _customerService.RestoreCustomerAsync(id, null, "系统用户");
                return Ok(ApiResponse<object>.Ok(null, "客户已恢复"));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message, 404)); }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        // ===== 回收站列表 =====
        [HttpGet("recycle-bin")]
        public async Task<ActionResult<ApiResponse<object>>> GetRecycleBin([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            try
            {
                var result = await _customerService.GetDeletedCustomersAsync(page, pageSize, keyword);
                return Ok(ApiResponse<object>.Ok(new { items = result.Items, totalCount = result.TotalCount, pageNumber = result.PageIndex, pageSize = result.PageSize, totalPages = result.TotalPages }));
            }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        // ===== 黑名单列表 =====
        [HttpGet("blacklist")]
        public async Task<ActionResult<ApiResponse<object>>> GetBlackList([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            try
            {
                var result = await _customerService.GetBlackListCustomersAsync(page, pageSize, keyword);
                return Ok(ApiResponse<object>.Ok(new { items = result.Items, totalCount = result.TotalCount, pageNumber = result.PageIndex, pageSize = result.PageSize, totalPages = result.TotalPages }));
            }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        // ===== 冻结客户列表 =====
        [HttpGet("frozen")]
        public async Task<ActionResult<ApiResponse<object>>> GetFrozen([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            try
            {
                var result = await _customerService.GetFrozenCustomersAsync(page, pageSize, keyword);
                return Ok(ApiResponse<object>.Ok(new { items = result.Items, totalCount = result.TotalCount, pageNumber = result.PageIndex, pageSize = result.PageSize, totalPages = result.TotalPages }));
            }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        // ===== 客户操作日志 =====
        [HttpGet("{id}/operation-logs")]
        public async Task<ActionResult<ApiResponse<object>>> GetOperationLogs(string id)
        {
            try
            {
                var logs = await _customerService.GetOperationLogsAsync(id);
                return Ok(ApiResponse<object>.Ok(logs));
            }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }

        // ===== 客户变更日志 =====
        [HttpGet("{id}/change-logs")]
        public async Task<ActionResult<ApiResponse<object>>> GetChangeLogs(string id)
        {
            try
            {
                var logs = await _customerService.GetChangeLogsAsync(id);
                return Ok(ApiResponse<object>.Ok(logs));
            }
            catch (Exception ex) { return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500)); }
        }
    }
}
