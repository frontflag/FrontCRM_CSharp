using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customs-brokers")]
public class CustomsBrokersController : ControllerBase
{
    private readonly ICustomsBrokerService _service;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomsBrokersController> _logger;

    public CustomsBrokersController(
        ICustomsBrokerService service,
        ApplicationDbContext db,
        ILogger<CustomsBrokersController> logger)
    {
        _service = service;
        _db = db;
        _logger = logger;
    }

    /// <param name="all">为 true 时返回全部（管理页）；默认 false 仅启用（下拉等）。</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomsBroker>>>> GetList([FromQuery] bool all = false)
    {
        try
        {
            var list = all
                ? await _service.GetAllOrderedForAdminAsync()
                : await _service.GetActiveListAsync();
            return Ok(ApiResponse<IReadOnlyList<CustomsBroker>>.Ok(list, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取报关公司列表失败");
            return StatusCode(500, ApiResponse<IReadOnlyList<CustomsBroker>>.Fail(ex.Message, 500));
        }
    }

    public class CreateCustomsBrokerRequest
    {
        public string Cname { get; set; } = string.Empty;
        public string? Ename { get; set; }
        /// <summary>10=深圳 20=香港</summary>
        public short Type { get; set; } = 10;
        public string? Remark { get; set; }
    }

    public class UpdateCustomsBrokerRequest
    {
        public string Cname { get; set; } = string.Empty;
        public string? Ename { get; set; }
        public short Type { get; set; } = 10;
        public string? Remark { get; set; }
    }

    public class SetCustomsBrokerStatusRequest
    {
        /// <summary>1=启用，0=停用</summary>
        public short Status { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomsBroker>>> Create([FromBody] CreateCustomsBrokerRequest body)
    {
        try
        {
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            var row = await _service.CreateAsync(body.Cname, body.Ename, body.Type, body.Remark, uid);
            return Ok(ApiResponse<CustomsBroker>.Ok(row, "创建成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CustomsBroker>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<CustomsBroker>.Fail(ex.Message, 409));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建报关公司失败");
            return StatusCode(500, ApiResponse<CustomsBroker>.Fail(ex.Message, 500));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CustomsBroker>>> Update(string id, [FromBody] UpdateCustomsBrokerRequest body)
    {
        try
        {
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            var row = await _service.UpdateAsync(id, body.Cname, body.Ename, body.Type, body.Remark, uid);
            return Ok(ApiResponse<CustomsBroker>.Ok(row, "保存成功"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<CustomsBroker>.Fail(ex.Message, 404));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CustomsBroker>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新报关公司失败 {Id}", id);
            return StatusCode(500, ApiResponse<CustomsBroker>.Fail(ex.Message, 500));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<CustomsBroker>>> SetStatus(string id, [FromBody] SetCustomsBrokerStatusRequest body)
    {
        try
        {
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            var row = await _service.SetStatusAsync(id, body.Status, uid);
            return Ok(ApiResponse<CustomsBroker>.Ok(row, "状态已更新"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<CustomsBroker>.Fail(ex.Message, 404));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CustomsBroker>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新报关公司状态失败 {Id}", id);
            return StatusCode(500, ApiResponse<CustomsBroker>.Fail(ex.Message, 500));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> SoftDelete(string id)
    {
        try
        {
            var key = id.Trim();
            var inUse = await _db.CustomsDeclarations.AsNoTracking().AnyAsync(d => d.CustomsBrokerId == key);
            if (inUse)
                return BadRequest(ApiResponse<object>.Fail("该报关公司仍被报关单引用，无法删除。", 400));

            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            await _service.SoftDeleteAsync(key, uid);
            return Ok(ApiResponse<object>.Ok(null, "已删除"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "软删除报关公司失败 {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }
}
