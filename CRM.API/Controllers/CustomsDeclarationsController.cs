using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customs-declarations")]
public class CustomsDeclarationsController : ControllerBase
{
    private readonly ICustomsDeclarationService _service;
    private readonly IRbacService _rbacService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomsDeclarationsController> _logger;

    public CustomsDeclarationsController(
        ICustomsDeclarationService service,
        IRbacService rbacService,
        ApplicationDbContext db,
        ILogger<CustomsDeclarationsController> logger)
    {
        _service = service;
        _rbacService = rbacService;
        _db = db;
        _logger = logger;
    }

    public class ForceDeleteCustomsDeclarationRequest
    {
        public string ConfirmBillCode { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomsDeclarationListItemDto>>>> GetList(
        [FromQuery] string? declarationCode,
        [FromQuery] string? stockOutRequestId,
        [FromQuery] short? internalStatus,
        [FromQuery] short? customsClearanceStatus,
        [FromQuery] short? declarationType,
        [FromQuery] DateTime? declareDateFrom,
        [FromQuery] DateTime? declareDateTo,
        [FromQuery] int take = 500)
    {
        try
        {
            var n = Math.Clamp(take, 1, 1000);
            var codeQ = (declarationCode ?? string.Empty).Trim();
            var sorQ = (stockOutRequestId ?? string.Empty).Trim();

            var dq = _db.CustomsDeclarations.AsNoTracking();
            if (!string.IsNullOrEmpty(codeQ))
                dq = dq.Where(d => EF.Functions.ILike(d.DeclarationCode, $"%{codeQ}%"));
            if (!string.IsNullOrEmpty(sorQ))
                dq = dq.Where(d => d.StockOutRequestId == sorQ);
            if (internalStatus.HasValue)
                dq = dq.Where(d => d.InternalStatus == internalStatus.Value);
            if (customsClearanceStatus.HasValue)
                dq = dq.Where(d => d.CustomsClearanceStatus == customsClearanceStatus.Value);
            if (declarationType.HasValue)
                dq = dq.Where(d => d.DeclarationType == declarationType.Value);
            if (declareDateFrom.HasValue)
            {
                var from = declareDateFrom.Value.Date;
                dq = dq.Where(d => d.DeclareDate >= from);
            }

            if (declareDateTo.HasValue)
            {
                var toExclusive = declareDateTo.Value.Date.AddDays(1);
                dq = dq.Where(d => d.DeclareDate < toExclusive);
            }

            var query =
                from d in dq
                join b in _db.CustomsBrokers.AsNoTracking().IgnoreQueryFilters() on d.CustomsBrokerId equals b.Id
                join u in _db.Users.AsNoTracking() on d.CreateByUserId equals u.Id into uj
                from u in uj.DefaultIfEmpty()
                orderby d.DeclareDate descending, d.CreateTime descending
                select new { d, b, u };

            var rows = await query.Take(n).ToListAsync();
            var list = rows.Select(x => new CustomsDeclarationListItemDto
            {
                Id = x.d.Id,
                DeclarationCode = x.d.DeclarationCode,
                StockOutRequestId = x.d.StockOutRequestId,
                CustomsBrokerId = x.d.CustomsBrokerId,
                CustomsBrokerName = x.b.Cname,
                DeclarationType = x.d.DeclarationType,
                InternalStatus = x.d.InternalStatus,
                CustomsClearanceStatus = x.d.CustomsClearanceStatus,
                DeclareDate = x.d.DeclareDate,
                TotalTaxAmount = x.d.TotalTaxAmount,
                Remark = x.d.Remark,
                CreateTime = x.d.CreateTime,
                CreateByUserId = x.d.CreateByUserId,
                CreateUserDisplay = x.u != null && !string.IsNullOrWhiteSpace(x.u.UserName)
                    ? x.u.UserName.Trim()
                    : null
            }).ToList();

            return Ok(ApiResponse<List<CustomsDeclarationListItemDto>>.Ok(list, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取报关单列表失败");
            return StatusCode(500, ApiResponse<List<CustomsDeclarationListItemDto>>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CustomsDeclaration>>> GetById(string id)
    {
        var row = await _db.CustomsDeclarations
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (row == null)
            return NotFound(ApiResponse<CustomsDeclaration>.Fail("报关单不存在", 404));
        return Ok(ApiResponse<CustomsDeclaration>.Ok(row, "OK"));
    }

    [HttpGet("by-stock-out-request/{stockOutRequestId}")]
    public async Task<ActionResult<ApiResponse<CustomsDeclaration>>> GetByStockOutRequest(string stockOutRequestId)
    {
        var key = stockOutRequestId.Trim();
        var row = await _db.CustomsDeclarations
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.StockOutRequestId == key);
        if (row == null)
            return NotFound(ApiResponse<CustomsDeclaration>.Fail("未找到对应报关单", 404));
        return Ok(ApiResponse<CustomsDeclaration>.Ok(row, "OK"));
    }

    public class SetClearanceStatusRequest
    {
        public short CustomsClearanceStatus { get; set; }
    }

    [HttpPatch("{id}/customs-clearance-status")]
    public async Task<ActionResult<ApiResponse<object>>> SetClearanceStatus(string id, [FromBody] SetClearanceStatusRequest body)
    {
        try
        {
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            await _service.SetCustomsClearanceStatusAsync(id, body.CustomsClearanceStatus, uid);
            return Ok(ApiResponse<object>.Ok(null, "已更新海关状态"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新海关状态失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> Complete(string id)
    {
        try
        {
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            await _service.CompleteDeclarationAndTransferAsync(id, uid);
            return Ok(ApiResponse<object>.Ok(null, "报关完成并已移库"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "报关完成失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            await _service.DeleteDeclarationAsync(id);
            return Ok(ApiResponse<object>.Ok(null, "删除报关单成功"));
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("不存在", StringComparison.Ordinal))
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除报关单失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("{id}/force-delete")]
    public async Task<ActionResult<ApiResponse<object>>> ForceDelete(string id, [FromBody] ForceDeleteCustomsDeclarationRequest? body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
            if (summary?.IsSysAdmin != true)
                return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员可执行强制删除", 403));

            if (body == null || string.IsNullOrWhiteSpace(body.ConfirmBillCode))
                return BadRequest(ApiResponse<object>.Fail("请填写 confirmBillCode", 400));

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            await _service.ForceDeleteDeclarationAsync(
                id,
                body.ConfirmBillCode.Trim(),
                userId.Trim(),
                string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

            return Ok(ApiResponse<object>.Ok(null, "强制删除报关单成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "强制删除报关单失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }
}
