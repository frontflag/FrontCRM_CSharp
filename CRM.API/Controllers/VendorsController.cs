using Microsoft.AspNetCore.Mvc;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [RequirePermission("vendor.read")]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly IApprovalRecordService _approvalRecordService;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly ILogger<VendorsController> _logger;

        public VendorsController(IVendorService vendorService, IApprovalRecordService approvalRecordService, IDataPermissionService dataPermissionService, ILogger<VendorsController> logger)
        {
            _vendorService = vendorService;
            _approvalRecordService = approvalRecordService;
            _dataPermissionService = dataPermissionService;
            _logger = logger;
        }

        public class VendorBlacklistRequest
        {
            public string? Reason { get; set; }
        }

        public class AddVendorAddressDto
        {
            public short AddressType { get; set; } = 1;
            public short? Country { get; set; }
            public string? Province { get; set; }
            public string? City { get; set; }
            public string? Area { get; set; }
            public string? Address { get; set; }
            public string? ContactName { get; set; }
            public string? ContactPhone { get; set; }
            public bool IsDefault { get; set; } = false;
        }

        public class UpdateVendorAddressDto
        {
            public short? AddressType { get; set; }
            public short? Country { get; set; }
            public string? Province { get; set; }
            public string? City { get; set; }
            public string? Area { get; set; }
            public string? Address { get; set; }
            public string? ContactName { get; set; }
            public string? ContactPhone { get; set; }
            public bool? IsDefault { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetVendors(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null,
            [FromQuery] short? status = null)
        {
            try
            {
                var request = new VendorQueryRequest
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Keyword = keyword,
                    Status = status,
                    CurrentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await _vendorService.GetPagedAsync(request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageNumber = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages
                }, "获取供应商列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取供应商列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<object>>> GetVendorStatistics()
        {
            try
            {
                var vendors = (await _vendorService.GetAllAsync()).ToList();
                var now = DateTime.UtcNow;
                var firstDayOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var totalVendors = vendors.Count;
                var activeVendors = vendors.Count(v => (v.Status == 10 || v.Status == 20) && !v.IsDeleted);
                var newThisMonth = vendors.Count(v => v.CreateTime >= firstDayOfMonth && !v.IsDeleted);

                var byLevel = vendors
                    .Where(v => v.Level.HasValue)
                    .GroupBy(v => v.Level!.Value)
                    .ToDictionary(
                        g => g.Key.ToString(),
                        g => g.Count());

                var byIndustry = vendors
                    .Where(v => !string.IsNullOrEmpty(v.Industry))
                    .GroupBy(v => v.Industry!)
                    .ToDictionary(g => g.Key, g => g.Count());

                return Ok(ApiResponse<object>.Ok(new
                {
                    totalVendors,
                    activeVendors,
                    newThisMonth,
                    byLevel,
                    byIndustry
                }, "获取供应商统计成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商统计失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取供应商统计失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<VendorInfo>>> GetVendorById(string id)
        {
            try
            {
                var vendor = await _vendorService.GetByIdAsync(id);
                if (vendor == null)
                    return NotFound(ApiResponse<VendorInfo>.Fail("供应商不存在", 404));
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId) && !await _dataPermissionService.CanAccessVendorAsync(userId, vendor))
                    return StatusCode(403, ApiResponse<VendorInfo>.Fail("无权限访问该供应商", 403));
                return Ok(ApiResponse<VendorInfo>.Ok(vendor, "获取供应商详情成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商详情失败");
                return StatusCode(500, ApiResponse<VendorInfo>.Fail($"获取供应商详情失败: {ex.Message}", 500));
            }
        }

        [HttpPost]
        [RequirePermission("vendor.write")]
        public async Task<ActionResult<ApiResponse<VendorInfo>>> CreateVendor([FromBody] CreateVendorRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorInfo>.Fail("请求体不能为空", 400));
                var vendor = await _vendorService.CreateAsync(request);
                return Ok(ApiResponse<VendorInfo>.Ok(vendor, "创建供应商成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<VendorInfo>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建供应商失败");
                return StatusCode(500, ApiResponse<VendorInfo>.Fail($"创建供应商失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{id}")]
        [RequirePermission("vendor.write")]
        public async Task<ActionResult<ApiResponse<VendorInfo>>> UpdateVendor(string id, [FromBody] UpdateVendorRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorInfo>.Fail("请求体不能为空", 400));
                var vendor = await _vendorService.UpdateAsync(id, request);
                return Ok(ApiResponse<VendorInfo>.Ok(vendor, "更新供应商成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<VendorInfo>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新供应商失败");
                return StatusCode(500, ApiResponse<VendorInfo>.Fail($"更新供应商失败: {ex.Message}", 500));
            }
        }

        public class DeleteVendorRequest
        {
            public string? Reason { get; set; }
        }

        [HttpDelete("{id}")]
        [RequirePermission("vendor.write")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteVendor(string id, [FromBody] DeleteVendorRequest? request)
        {
            try
            {
                await _vendorService.DeleteAsync(id, request?.Reason);
                return Ok(ApiResponse<object>.Ok(null, "删除供应商成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除供应商失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除供应商失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/blacklist")]
        public async Task<ActionResult<ApiResponse<object>>> AddToBlacklist(string id, [FromBody] VendorBlacklistRequest request)
        {
            try
            {
                await _vendorService.AddToBlacklistAsync(id, request?.Reason);
                return Ok(ApiResponse<object>.Ok(null, "加入黑名单成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加入供应商黑名单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"加入供应商黑名单失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{id}/blacklist")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveFromBlacklist(string id)
        {
            try
            {
                await _vendorService.RemoveFromBlacklistAsync(id);
                return Ok(ApiResponse<object>.Ok(null, "移出黑名单成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移出供应商黑名单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"移出供应商黑名单失败: {ex.Message}", 500));
            }
        }

        [HttpGet("blacklist")]
        public async Task<ActionResult<ApiResponse<object>>> GetBlacklist(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var request = new VendorQueryRequest
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Keyword = keyword
                };
                var result = await _vendorService.GetBlacklistAsync(request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageNumber = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
                }, "获取供应商黑名单成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商黑名单失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取供应商黑名单失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/activate")]
        public async Task<ActionResult<ApiResponse<object>>> ActivateVendor(string id)
        {
            try
            {
                await _vendorService.UpdateStatusAsync(id, 10);
                return Ok(ApiResponse<object>.Ok(null, "激活供应商成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "激活供应商失败");
                return StatusCode(500, ApiResponse<object>.Fail($"激活供应商失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult<ApiResponse<object>>> DeactivateVendor(string id)
        {
            try
            {
                await _vendorService.UpdateStatusAsync(id, 1);
                return Ok(ApiResponse<object>.Ok(null, "停用供应商成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停用供应商失败");
                return StatusCode(500, ApiResponse<object>.Fail($"停用供应商失败: {ex.Message}", 500));
            }
        }

        /// <summary>提交审核：新建(1) -> 待审核(2)</summary>
        [HttpPost("{id}/submit-audit")]
        [RequirePermission("vendor.write")]
        public async Task<ActionResult<ApiResponse<object>>> SubmitAudit(string id)
        {
            try
            {
                var vendor = await _vendorService.GetByIdAsync(id);
                if (vendor == null)
                    return NotFound(ApiResponse<object>.Fail("供应商不存在", 404));
                var before = vendor.Status;
                await _vendorService.UpdateStatusAsync(id, 2);
                await _approvalRecordService.RecordSubmitAsync(
                    "VENDOR",
                    vendor.Id,
                    vendor.Code,
                    $"供应商：{(vendor.OfficialName ?? vendor.NickName ?? vendor.Code)}；采购员：{(vendor.PurchaseUserId ?? "—")}；付款方式：{(vendor.PaymentMethod ?? "—")}",
                    before,
                    2,
                    null,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    User.Identity?.Name);
                return Ok(ApiResponse<object>.Ok(null, "提交审核成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交供应商审核失败");
                return StatusCode(500, ApiResponse<object>.Fail($"提交审核失败: {ex.Message}", 500));
            }
        }

        [HttpGet("recycle-bin")]
        public async Task<ActionResult<ApiResponse<object>>> GetRecycleBin(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var result = await _vendorService.GetDeletedAsync(pageNumber, pageSize, keyword);
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = result.Items,
                    totalCount = result.TotalCount,
                    pageNumber = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize)
                }, "获取供应商回收站列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商回收站列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取供应商回收站列表失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{id}/restore")]
        public async Task<ActionResult<ApiResponse<object>>> RestoreVendor(string id)
        {
            try
            {
                await _vendorService.RestoreAsync(id);
                return Ok(ApiResponse<object>.Ok(null, "恢复供应商成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复供应商失败");
                return StatusCode(500, ApiResponse<object>.Fail($"恢复供应商失败: {ex.Message}", 500));
            }
        }

        // ===== 供应商操作日志 / 变更日志 =====

        [HttpGet("{id}/operation-logs")]
        public async Task<ActionResult<ApiResponse<object>>> GetOperationLogs(string id)
        {
            try
            {
                var logs = await _vendorService.GetOperationLogsAsync(id);
                return Ok(ApiResponse<object>.Ok(logs, "获取供应商操作日志成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商操作日志失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取供应商操作日志失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{id}/field-change-logs")]
        public async Task<ActionResult<ApiResponse<object>>> GetFieldChangeLogs(string id)
        {
            try
            {
                var logs = await _vendorService.GetChangeLogsAsync(id);
                return Ok(ApiResponse<object>.Ok(logs, "获取供应商字段变更日志成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商字段变更日志失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取供应商字段变更日志失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{vendorId}/contacts")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorContactInfo>>>> GetVendorContacts(string vendorId)
        {
            try
            {
                var vendor = await _vendorService.GetByIdAsync(vendorId);
                if (vendor == null)
                    return NotFound(ApiResponse<IEnumerable<VendorContactInfo>>.Fail("供应商不存在", 404));
                var contacts = await _vendorService.GetContactsByVendorIdAsync(vendorId);
                return Ok(ApiResponse<IEnumerable<VendorContactInfo>>.Ok(contacts, "获取联系人列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商联系人失败");
                return StatusCode(500, ApiResponse<IEnumerable<VendorContactInfo>>.Fail($"获取供应商联系人失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{vendorId}/contacts")]
        public async Task<ActionResult<ApiResponse<VendorContactInfo>>> AddVendorContact(string vendorId, [FromBody] AddVendorContactRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorContactInfo>.Fail("请求体不能为空", 400));
                var contact = await _vendorService.AddContactAsync(vendorId, request);
                return Ok(ApiResponse<VendorContactInfo>.Ok(contact, "添加联系人成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<VendorContactInfo>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加供应商联系人失败");
                return StatusCode(500, ApiResponse<VendorContactInfo>.Fail($"添加供应商联系人失败: {ex.Message}", 500));
            }
        }

        // ─── Contact History ──────────────────────────────────────────────────────

        [HttpGet("{vendorId}/contact-history")]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetVendorContactHistory(string vendorId)
        {
            try
            {
                var vendor = await _vendorService.GetByIdAsync(vendorId);
                if (vendor == null)
                    return NotFound(ApiResponse<IEnumerable<object>>.Fail("供应商不存在", 404));

                var list = await _vendorService.GetContactHistoryAsync(vendorId);
                var items = list.Select(h => new
                {
                    id = h.Id,
                    vendorId = h.VendorId,
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
                _logger.LogError(ex, "获取供应商联系历史失败");
                return StatusCode(500, ApiResponse<IEnumerable<object>>.Fail($"获取供应商联系历史失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{vendorId}/contact-history")]
        public async Task<ActionResult<ApiResponse<object>>> AddVendorContactHistory(string vendorId, [FromBody] AddVendorContactHistoryRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                var record = await _vendorService.AddContactHistoryAsync(vendorId, request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    id = record.Id,
                    vendorId = record.VendorId,
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
                _logger.LogError(ex, "添加供应商联系记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"添加供应商联系记录失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{vendorId}/contact-history/{historyId}")]
        public async Task<ActionResult<ApiResponse<object>>> UpdateVendorContactHistory(string vendorId, string historyId, [FromBody] UpdateVendorContactHistoryRequest request)
        {
            try
            {
                var record = await _vendorService.UpdateContactHistoryAsync(historyId, request);
                return Ok(ApiResponse<object>.Ok(new
                {
                    id = record.Id,
                    vendorId = record.VendorId,
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新供应商联系记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"更新供应商联系记录失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{vendorId}/contact-history/{historyId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteVendorContactHistory(string vendorId, string historyId)
        {
            try
            {
                await _vendorService.DeleteContactHistoryAsync(historyId);
                return Ok(ApiResponse<object>.Ok(null, "删除联系记录成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除供应商联系记录失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除供应商联系记录失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{vendorId}/addresses")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorAddress>>>> GetVendorAddresses(string vendorId)
        {
            try
            {
                var vendor = await _vendorService.GetByIdAsync(vendorId);
                if (vendor == null)
                    return NotFound(ApiResponse<IEnumerable<VendorAddress>>.Fail("供应商不存在", 404));
                var addresses = await _vendorService.GetAddressesByVendorIdAsync(vendorId);
                return Ok(ApiResponse<IEnumerable<VendorAddress>>.Ok(addresses, "获取地址列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商地址失败");
                return StatusCode(500, ApiResponse<IEnumerable<VendorAddress>>.Fail($"获取供应商地址失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{vendorId}/addresses")]
        public async Task<ActionResult<ApiResponse<VendorAddress>>> AddVendorAddress(string vendorId, [FromBody] AddVendorAddressDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorAddress>.Fail("请求体不能为空", 400));
                var result = await _vendorService.AddAddressAsync(vendorId, new AddVendorAddressRequest
                {
                    AddressType = request.AddressType,
                    Country = request.Country,
                    Province = request.Province,
                    City = request.City,
                    Area = request.Area,
                    Address = request.Address,
                    ContactName = request.ContactName,
                    ContactPhone = request.ContactPhone,
                    IsDefault = request.IsDefault
                });
                return Ok(ApiResponse<VendorAddress>.Ok(result, "添加地址成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<VendorAddress>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加供应商地址失败");
                return StatusCode(500, ApiResponse<VendorAddress>.Fail($"添加供应商地址失败: {ex.Message}", 500));
            }
        }

        public class AddVendorBankDto
        {
            public string? BankName { get; set; }
            public string? BankAccount { get; set; }
            public string? AccountName { get; set; }
            public string? BankBranch { get; set; }
            public short? Currency { get; set; }
            public bool IsDefault { get; set; } = false;
            public string? Remark { get; set; }
        }

        [HttpGet("{vendorId}/banks")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorBankInfo>>>> GetVendorBanks(string vendorId)
        {
            try
            {
                var vendor = await _vendorService.GetByIdAsync(vendorId);
                if (vendor == null)
                    return NotFound(ApiResponse<IEnumerable<VendorBankInfo>>.Fail("供应商不存在", 404));
                var banks = await _vendorService.GetBanksByVendorIdAsync(vendorId);
                return Ok(ApiResponse<IEnumerable<VendorBankInfo>>.Ok(banks, "获取银行信息列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取供应商银行信息失败");
                return StatusCode(500, ApiResponse<IEnumerable<VendorBankInfo>>.Fail($"获取供应商银行信息失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{vendorId}/banks")]
        public async Task<ActionResult<ApiResponse<VendorBankInfo>>> AddVendorBank(string vendorId, [FromBody] AddVendorBankDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorBankInfo>.Fail("请求体不能为空", 400));
                var bank = await _vendorService.AddBankAsync(vendorId, new AddVendorBankRequest
                {
                    BankName = request.BankName,
                    BankAccount = request.BankAccount,
                    AccountName = request.AccountName,
                    BankBranch = request.BankBranch,
                    Currency = request.Currency,
                    IsDefault = request.IsDefault,
                    Remark = request.Remark
                });
                return Ok(ApiResponse<VendorBankInfo>.Ok(bank, "添加银行信息成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<VendorBankInfo>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加供应商银行信息失败");
                return StatusCode(500, ApiResponse<VendorBankInfo>.Fail($"添加供应商银行信息失败: {ex.Message}", 500));
            }
        }

        [HttpPut("addresses/{addressId}")]
        public async Task<ActionResult<ApiResponse<VendorAddress>>> UpdateVendorAddress(string addressId, [FromBody] UpdateVendorAddressDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<VendorAddress>.Fail("请求体不能为空", 400));
                var result = await _vendorService.UpdateAddressAsync(addressId, new UpdateVendorAddressRequest
                {
                    AddressType = request.AddressType,
                    Country = request.Country,
                    Province = request.Province,
                    City = request.City,
                    Area = request.Area,
                    Address = request.Address,
                    ContactName = request.ContactName,
                    ContactPhone = request.ContactPhone,
                    IsDefault = request.IsDefault
                });
                return Ok(ApiResponse<VendorAddress>.Ok(result, "更新地址成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<VendorAddress>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新供应商地址失败");
                return StatusCode(500, ApiResponse<VendorAddress>.Fail($"更新供应商地址失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("addresses/{addressId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteVendorAddress(string addressId)
        {
            try
            {
                await _vendorService.DeleteAddressAsync(addressId);
                return Ok(ApiResponse<object>.Ok(null, "删除地址成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除供应商地址失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除供应商地址失败: {ex.Message}", 500));
            }
        }

        [HttpPost("addresses/{addressId}/set-default")]
        public async Task<ActionResult<ApiResponse<object>>> SetDefaultVendorAddress(string addressId)
        {
            try
            {
                await _vendorService.SetDefaultAddressAsync(addressId);
                return Ok(ApiResponse<object>.Ok(null, "设置默认地址成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置供应商默认地址失败");
                return StatusCode(500, ApiResponse<object>.Fail($"设置供应商默认地址失败: {ex.Message}", 500));
            }
        }
    }
}
