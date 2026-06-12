using CRM.API.Models.DTOs;
using CRM.API.Utilities;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/customs-pendlists")]
public class CustomsPendlistsController : ControllerBase
{
    private readonly ICustomsPendlistService _service;
    private readonly IRbacService _rbacService;
    private readonly ILogger<CustomsPendlistsController> _logger;

    public CustomsPendlistsController(
        ICustomsPendlistService service,
        IRbacService rbacService,
        ILogger<CustomsPendlistsController> logger)
    {
        _service = service;
        _rbacService = rbacService;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成报关出库通知失败 PendlistId={Id}", id);
            return StatusCode(500, ApiResponse<CreateCustomsOutNotifyResultDto>.Fail(ex.Message, 500));
        }
    }
}
