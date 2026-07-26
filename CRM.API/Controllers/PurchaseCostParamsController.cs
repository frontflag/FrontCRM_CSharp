using System.Security.Claims;
using CRM.API.Authorization;using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/purchase-cost-params")]
public class PurchaseCostParamsController : ControllerBase
{
    private readonly IPurchaseCostParamService _service;
    private readonly IRbacService _rbacService;
    private readonly ILogger<PurchaseCostParamsController> _logger;

    public PurchaseCostParamsController(
        IPurchaseCostParamService service,
        IRbacService rbacService,
        ILogger<PurchaseCostParamsController> logger)
    {
        _service = service;
        _rbacService = rbacService;
        _logger = logger;
    }

    /// <summary>当前生效采购系数（报关试算只读；财务参数管理页可读）。</summary>
    [HttpGet("effective")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PurchaseCostParamDto>>> GetEffective(CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var canCustoms = await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User);
            var canManage = false;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);
                canManage = summary.IsSysAdmin || summary.PermissionCodes.Any(c =>
                    string.Equals(c, "system.params.finance.purchase-cost-params.read", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c, "system.params.finance.read", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c, "rbac.manage", StringComparison.OrdinalIgnoreCase));
            }

            if (!canCustoms && !canManage)
                return StatusCode(403, ApiResponse<PurchaseCostParamDto>.Fail("当前账号无权访问采购系数", 403));

            var dto = await _service.GetEffectiveAsync(cancellationToken: ct);
            return Ok(ApiResponse<PurchaseCostParamDto>.Ok(dto, "OK"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PurchaseCostParamDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取采购系数失败");
            return StatusCode(500, ApiResponse<PurchaseCostParamDto>.Fail(ex.Message, 500));
        }
    }

    [HttpGet]
    [RequirePermission("system.params.finance.purchase-cost-params.read")]
    public async Task<ActionResult<ApiResponse<PurchaseCostParamPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var (items, total) = await _service.ListPagedAsync(page, pageSize, ct);
            return Ok(ApiResponse<PurchaseCostParamPageDto>.Ok(new PurchaseCostParamPageDto
            {
                Items = items.ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "采购系数列表失败");
            return StatusCode(500, ApiResponse<PurchaseCostParamPageDto>.Fail(ex.Message, 500));
        }
    }

    public sealed class CreatePurchaseCostParamRequest
    {
        public decimal Ratio { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public string? Remark { get; set; }
    }

    [HttpPost]
    [RequirePermission("system.params.finance.purchase-cost-params.write")]
    public async Task<ActionResult<ApiResponse<PurchaseCostParamDto>>> Create(
        [FromBody] CreatePurchaseCostParamRequest body,
        CancellationToken ct)
    {
        if (body == null)
            return BadRequest(ApiResponse<PurchaseCostParamDto>.Fail("请求体为空", 400));

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value
                         ?? User.FindFirst("userId")?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var dto = await _service.CreateAsync(body.Ratio, body.StartTimeUtc, body.Remark, userId, userName, ct);
            return Ok(ApiResponse<PurchaseCostParamDto>.Ok(dto, "创建成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PurchaseCostParamDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建采购系数失败");
            return StatusCode(500, ApiResponse<PurchaseCostParamDto>.Fail(ex.Message, 500));
        }
    }

    [HttpDelete("{id}")]
    [RequirePermission("system.params.finance.purchase-cost-params.write")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value
                         ?? User.FindFirst("userId")?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            await _service.SoftDeleteAsync(id, userId, userName, ct);
            return Ok(ApiResponse<object>.Ok(null, "已删除"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除采购系数失败 {Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("change-log")]
    [RequirePermission("system.params.finance.purchase-cost-params.read")]
    public async Task<ActionResult<ApiResponse<PurchaseCostParamChangeLogPageDto>>> GetChangeLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var (items, total) = await _service.GetChangeLogPagedAsync(page, pageSize, ct);
            return Ok(ApiResponse<PurchaseCostParamChangeLogPageDto>.Ok(new PurchaseCostParamChangeLogPageDto
            {
                Items = items.ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }, "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "采购系数变更日志失败");
            return StatusCode(500, ApiResponse<PurchaseCostParamChangeLogPageDto>.Fail(ex.Message, 500));
        }
    }
}

public sealed class PurchaseCostParamPageDto
{
    public List<PurchaseCostParamDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class PurchaseCostParamChangeLogPageDto
{
    public List<PurchaseCostParamChangeLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
