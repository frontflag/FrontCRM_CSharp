using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/freight-forwarder-companies")]
public class FreightForwarderCompaniesController : ControllerBase
{
    private readonly IFreightForwarderCompanyService _service;
    private readonly ILogger<FreightForwarderCompaniesController> _logger;

    public FreightForwarderCompaniesController(
        IFreightForwarderCompanyService service,
        ILogger<FreightForwarderCompaniesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [RequirePermission("finance-receipt.read")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FreightForwarderCompany>>>> GetList([FromQuery] bool all = false)
    {
        try
        {
            var list = all
                ? await _service.GetAllOrderedForAdminAsync()
                : await _service.GetActiveListAsync();
            return Ok(ApiResponse<IReadOnlyList<FreightForwarderCompany>>.Ok(list, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货代公司列表失败");
            return StatusCode(500, ApiResponse<IReadOnlyList<FreightForwarderCompany>>.Fail(ex.Message, 500));
        }
    }

    [RequirePermission("finance-receipt.read")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FreightForwarderCompany>>> GetById(string id, [FromQuery] bool includeBanks = false)
    {
        try
        {
            var row = await _service.GetByIdAsync(id, includeBanks);
            if (row == null)
                return NotFound(ApiResponse<FreightForwarderCompany>.Fail("货代公司不存在", 404));
            return Ok(ApiResponse<FreightForwarderCompany>.Ok(row, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货代公司失败: {Id}", id);
            return StatusCode(500, ApiResponse<FreightForwarderCompany>.Fail(ex.Message, 500));
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<FreightForwarderCompany>>> Create([FromBody] CreateFreightForwarderCompanyRequest body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var row = await _service.CreateAsync(body.Cname, body.Ename, body.Remark, userId);
            return Ok(ApiResponse<FreightForwarderCompany>.Ok(row, "创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建货代公司失败");
            return BadRequest(ApiResponse<FreightForwarderCompany>.Fail(ex.Message, 400));
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<FreightForwarderCompany>>> Update(string id, [FromBody] UpdateFreightForwarderCompanyRequest body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var row = await _service.UpdateAsync(id, body.Cname, body.Ename, body.Remark, userId);
            return Ok(ApiResponse<FreightForwarderCompany>.Ok(row, "更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新货代公司失败: {Id}", id);
            return BadRequest(ApiResponse<FreightForwarderCompany>.Fail(ex.Message, 400));
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<FreightForwarderCompany>>> SetStatus(string id, [FromBody] SetFreightForwarderCompanyStatusRequest body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var row = await _service.SetStatusAsync(id, body.Status, userId);
            return Ok(ApiResponse<FreightForwarderCompany>.Ok(row, "状态已更新"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新货代公司状态失败: {Id}", id);
            return BadRequest(ApiResponse<FreightForwarderCompany>.Fail(ex.Message, 400));
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _service.SoftDeleteAsync(id, userId);
            return Ok(ApiResponse<object>.Ok(new { }, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除货代公司失败: {Id}", id);
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [RequirePermission("finance-receipt.read")]
    [HttpGet("{companyId}/banks")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FreightForwarderCompanyBank>>>> GetBanks(string companyId)
    {
        try
        {
            var banks = await _service.GetBanksAsync(companyId);
            return Ok(ApiResponse<IReadOnlyList<FreightForwarderCompanyBank>>.Ok(banks, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取货代收款账户失败: {CompanyId}", companyId);
            return StatusCode(500, ApiResponse<IReadOnlyList<FreightForwarderCompanyBank>>.Fail(ex.Message, 500));
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPost("{companyId}/banks")]
    public async Task<ActionResult<ApiResponse<FreightForwarderCompanyBank>>> CreateBank(
        string companyId, [FromBody] UpsertFreightForwarderCompanyBankRequest body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var row = await _service.CreateBankAsync(
                companyId, body.BankName, body.AccountName, body.AccountNo, body.Currency, body.IsDefault, userId);
            return Ok(ApiResponse<FreightForwarderCompanyBank>.Ok(row, "创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建货代收款账户失败: {CompanyId}", companyId);
            return BadRequest(ApiResponse<FreightForwarderCompanyBank>.Fail(ex.Message, 400));
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpPut("banks/{bankId}")]
    public async Task<ActionResult<ApiResponse<FreightForwarderCompanyBank>>> UpdateBank(
        string bankId, [FromBody] UpsertFreightForwarderCompanyBankRequest body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var row = await _service.UpdateBankAsync(
                bankId, body.BankName, body.AccountName, body.AccountNo, body.Currency,
                body.IsDefault, body.IsDisabled, userId);
            return Ok(ApiResponse<FreightForwarderCompanyBank>.Ok(row, "更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新货代收款账户失败: {BankId}", bankId);
            return BadRequest(ApiResponse<FreightForwarderCompanyBank>.Fail(ex.Message, 400));
        }
    }

    [RequirePermission("finance-receipt.write")]
    [HttpDelete("banks/{bankId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBank(string bankId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _service.DeleteBankAsync(bankId, userId);
            return Ok(ApiResponse<object>.Ok(new { }, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除货代收款账户失败: {BankId}", bankId);
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }
}
