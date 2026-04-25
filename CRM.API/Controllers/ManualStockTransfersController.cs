using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

/// <summary>手工移库（形态 A：整行可迁量）。</summary>
[ApiController]
[Authorize]
[Route("api/v1/inventory/manual-transfers")]
public class ManualStockTransfersController : ControllerBase
{
    private readonly IManualStockTransferService _service;
    private readonly ILogger<ManualStockTransfersController> _logger;

    public ManualStockTransfersController(
        IManualStockTransferService service,
        ILogger<ManualStockTransfersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>预览：计算将迁数量与拦截原因（不写库）。</summary>
    [HttpGet("preview")]
    public async Task<ActionResult<ApiResponse<ManualStockTransferPreviewDto>>> Preview(
        [FromQuery] string sourceStockItemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await _service.PreviewAsync(sourceStockItemId, cancellationToken);
            return Ok(ApiResponse<ManualStockTransferPreviewDto>.Ok(dto, "预览成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手工移库预览失败");
            return StatusCode(500, ApiResponse<ManualStockTransferPreviewDto>.Fail($"预览失败: {ex.Message}", 500));
        }
    }

    /// <summary>执行移库（无数量入参，由服务端按可迁量过账）。</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ManualStockTransferExecuteResultDto>>> Execute(
        [FromBody] ManualStockTransferExecuteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.ExecuteAsync(request, actor, cancellationToken);
            return Ok(ApiResponse<ManualStockTransferExecuteResultDto>.Ok(result, "移库成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ManualStockTransferExecuteResultDto>.Fail(ex.Message, 400));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "手工移库执行失败");
            return StatusCode(500, ApiResponse<ManualStockTransferExecuteResultDto>.Fail($"执行失败: {ex.Message}", 500));
        }
    }
}
