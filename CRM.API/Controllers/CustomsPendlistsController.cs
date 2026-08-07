using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customs-pendlists")]
public class CustomsPendlistsController : ControllerBase
{
    private readonly ICustomsPendlistService _service;
    private readonly ICustomsPendlistFlowService _flowService;
    private readonly IRbacService _rbacService;
    private readonly ILogger<CustomsPendlistsController> _logger;

    public CustomsPendlistsController(
        ICustomsPendlistService service,
        ICustomsPendlistFlowService flowService,
        IRbacService rbacService,
        ILogger<CustomsPendlistsController> logger)
    {
        _service = service;
        _flowService = flowService;
        _rbacService = rbacService;
        _logger = logger;
    }

    public class ForceDeleteCustomsPendlistRequest
    {
        public string ConfirmPendlistId { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomsPendlistListItemDto>>>> GetList(
        [FromQuery] short? status,
        [FromQuery] string? keyword,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<List<CustomsPendlistListItemDto>>.Fail("当前账号无权访问报关模块", 403));

            var list = await _service.GetListAsync(
                status,
                keyword,
                take,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                cancellationToken);
            return Ok(ApiResponse<List<CustomsPendlistListItemDto>>.Ok(list.ToList(), "OK"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待报关列表失败");
            return StatusCode(500, ApiResponse<List<CustomsPendlistListItemDto>>.Fail(ex.Message, 500));
        }
    }

    [HttpGet("{id}/flow-aggregates")]
    public async Task<ActionResult<ApiResponse<CustomsPendlistFlowAggregatesDto>>> GetFlowAggregates(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<CustomsPendlistFlowAggregatesDto>.Fail("当前账号无权访问报关模块", 403));

            var data = await _flowService.GetFlowAggregatesAsync(id, cancellationToken);
            return Ok(ApiResponse<CustomsPendlistFlowAggregatesDto>.Ok(data, "OK"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CustomsPendlistFlowAggregatesDto>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<CustomsPendlistFlowAggregatesDto>.Fail(ex.Message, 404));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待报关流程聚合失败 PendlistId={Id}", id);
            return StatusCode(500, ApiResponse<CustomsPendlistFlowAggregatesDto>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("{id}/customs-out-notify")]
    public async Task<ActionResult<ApiResponse<CreateCustomsOutNotifyResultDto>>> CreateCustomsOutNotify(
        string id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await CustomsModuleAccessHttp.CanAccessAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<CreateCustomsOutNotifyResultDto>.Fail("当前账号无权访问报关模块", 403));

            if (!await LogisticsDataAccessHttp.CanWriteAsync(_rbacService, User))
                return StatusCode(403, ApiResponse<CreateCustomsOutNotifyResultDto>.Fail("当前账号物流数据为只读或禁止", 403));

            var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? User.FindFirst("userId")?.Value;
            var result = await _service.CreateCustomsOutNotifyAsync(id, uid, cancellationToken);
            return Ok(ApiResponse<CreateCustomsOutNotifyResultDto>.Ok(result, "已生成报关出库通知"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CreateCustomsOutNotifyResultDto>.Fail(ex.Message, 400));
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "生成报关出库通知保存失败 PendlistId={Id}", id);
            return StatusCode(500, ApiResponse<CreateCustomsOutNotifyResultDto>.Fail(msg, 500));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成报关出库通知失败 PendlistId={Id}", id);
            return StatusCode(500, ApiResponse<CreateCustomsOutNotifyResultDto>.Fail(ex.Message, 500));
        }
    }

    [HttpPost("{id}/force-delete")]
    public async Task<ActionResult<ApiResponse<object>>> ForceDelete(
        string id,
        [FromBody] ForceDeleteCustomsPendlistRequest? body)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return StatusCode(403, ApiResponse<object>.Fail("未登录或身份无效", 403));

            var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
            if (!ManagementAccountPolicy.CanForceDelete(summary))
                return StatusCode(403, ApiResponse<object>.Fail("仅系统管理员或平台管理员可执行强制删除", 403));

            if (body == null || string.IsNullOrWhiteSpace(body.ConfirmPendlistId))
                return BadRequest(ApiResponse<object>.Fail("请填写 confirmPendlistId", 400));

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            await _service.ForceDeleteAsync(
                id,
                body.ConfirmPendlistId.Trim(),
                userId.Trim(),
                string.IsNullOrWhiteSpace(userName) ? null : userName.Trim());

            return Ok(ApiResponse<object>.Ok(null, "强制删除待报关记录成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "强制删除待报关记录失败 PendlistId={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, 500));
        }
    }
}
