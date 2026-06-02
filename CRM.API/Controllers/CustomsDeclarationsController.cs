using CRM.API.Models.DTOs;
using CRM.API.Utilities;
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
    private readonly ICustomsV2FlowService _customsV2FlowService;
    private readonly IRbacService _rbacService;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CustomsDeclarationsController> _logger;

    public CustomsDeclarationsController(
        ICustomsDeclarationService service,
        ICustomsV2FlowService customsV2FlowService,
        IRbacService rbacService,
        IDataPermissionService dataPermissionService,
        ApplicationDbContext db,
        ILogger<CustomsDeclarationsController> logger)
    {
        _service = service;
        _customsV2FlowService = customsV2FlowService;
        _rbacService = rbacService;
        _dataPermissionService = dataPermissionService;
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            dq = await _dataPermissionService.ApplyLogisticsCreatorUserScopeAsync(
                userId,
                dq,
                d => d.CreateByUserId,
                CancellationToken.None);

            if (!string.IsNullOrEmpty(codeQ))
                dq = dq.Where(d => EF.Functions.ILike(d.DeclarationCode, $"%{codeQ}%"));
            if (!string.IsNullOrEmpty(sorQ))
            {
                var decIdsForSor = await _db.CustomsDeclarationItems.AsNoTracking()
                    .Where(i => i.StockOutRequestId == sorQ)
                    .Select(i => i.DeclarationId)
                    .Distinct()
                    .ToListAsync();
                dq = dq.Where(d => decIdsForSor.Contains(d.Id));
            }
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
            var decIds = rows.Select(x => x.d.Id).ToList();
            var firstSorByDec = await _db.CustomsDeclarationItems.AsNoTracking()
                .Where(i => decIds.Contains(i.DeclarationId))
                .GroupBy(i => i.DeclarationId)
                .Select(g => new { DeclarationId = g.Key, SorId = g.OrderBy(i => i.LineNo).Select(i => i.StockOutRequestId).FirstOrDefault() })
                .ToDictionaryAsync(x => x.DeclarationId, x => x.SorId);
            var list = rows.Select(x => new CustomsDeclarationListItemDto
            {
                Id = x.d.Id,
                DeclarationCode = x.d.DeclarationCode,
                PackingId = x.d.PackingId,
                StockOutRequestId = firstSorByDec.TryGetValue(x.d.Id, out var sor) ? sor : null,
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
        var item = await _db.CustomsDeclarationItems.AsNoTracking()
            .Where(i => i.StockOutRequestId == key)
            .OrderBy(i => i.LineNo)
            .Select(i => i.DeclarationId)
            .FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(item))
            return NotFound(ApiResponse<CustomsDeclaration>.Fail("未找到对应报关单", 404));
        var row = await _db.CustomsDeclarations
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == item);
        if (row == null)
            return NotFound(ApiResponse<CustomsDeclaration>.Fail("未找到对应报关单", 404));
        return Ok(ApiResponse<CustomsDeclaration>.Ok(row, "OK"));
    }

    public class SetClearanceStatusRequest
    {
        public short CustomsClearanceStatus { get; set; }
    }

    public class PatchCustomsDeclarationHeaderRequest
    {
        public string? ToWarehouseId { get; set; }
        public string? Remark { get; set; }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> PatchHeader(string id, [FromBody] PatchCustomsDeclarationHeaderRequest body)
    {
        try
        {
            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号物流数据为只读或禁止", 403));
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
            await _customsV2FlowService.UpdateDeclarationHeaderAsync(
                id, body?.ToWarehouseId, body?.Remark, uid);
            return Ok(ApiResponse<object>.Ok(null, "已更新报关单"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新报关单失败");
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpPatch("{id}/customs-clearance-status")]
    public async Task<ActionResult<ApiResponse<object>>> SetClearanceStatus(string id, [FromBody] SetClearanceStatusRequest body)
    {
        try
        {
            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号物流数据为只读或禁止", 403));
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
    public Task<ActionResult<ApiResponse<object>>> Complete(string id)
    {
        _ = id;
        return Task.FromResult<ActionResult<ApiResponse<object>>>(
            StatusCode(410, ApiResponse<object>.Fail(
                "报关 V2 已废弃「报关完成+移库一步过账」，请使用报关出库/入库流程。",
                410)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<object>.Fail("当前账号物流数据为只读或禁止", 403));
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
