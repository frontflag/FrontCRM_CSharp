using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiOrchestrator _orchestrator;
    private readonly IAiAdminService _adminService;
    private readonly IAiEntityParseLogService _entityParseLogService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiOrchestrator orchestrator,
        IAiAdminService adminService,
        IAiEntityParseLogService entityParseLogService,
        ILogger<AiController> logger)
    {
        _orchestrator = orchestrator;
        _adminService = adminService;
        _entityParseLogService = entityParseLogService;
        _logger = logger;
    }

    /// <summary>按场景调用 AI（非流式）。权限与限流由 Orchestrator 处理。</summary>
    [HttpPost("invoke")]
    public async Task<ActionResult<ApiResponse<AiInvokeResultDto>>> Invoke(
        [FromBody] AiInvokeRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ScenarioCode))
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail("scenarioCode 不能为空"));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            var result = await _orchestrator.InvokeAsync(request, userId, cancellationToken);
            var msg = result.FromCache ? "命中缓存" : "调用成功";
            return Ok(ApiResponse<AiInvokeResultDto>.Ok(result, msg));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI invoke rejected scenario={Scenario}", request.ScenarioCode);
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail(ex.Message));
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "AI invoke DB save failed scenario={Scenario}", request.ScenarioCode);
            return StatusCode(500, ApiResponse<AiInvokeResultDto>.Fail(
                $"AI 调用日志/缓存写入失败，请确认已执行 AI 模块数据库脚本。详情: {detail}", 500));
        }
        catch (OperationCanceledException ex) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI invoke canceled (client/proxy timeout) scenario={Scenario}", request.ScenarioCode);
            return StatusCode(504, ApiResponse<AiInvokeResultDto>.Fail(
                "AI 调用超时：网关或浏览器在结果返回前断开连接。请让运维将 Nginx proxy_read_timeout 调整为至少 300s（见 scripts/nginx-ai-invoke-timeout.snippet.conf）。", 504));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI invoke failed scenario={Scenario}", request.ScenarioCode);
            return StatusCode(500, ApiResponse<AiInvokeResultDto>.Fail($"调用失败: {ex.Message}", 500));
        }
    }

    /// <summary>上传名片图片并按场景调用 AI（multipart/form-data）。file 为正面（或单张合图），fileBack 为反面（可选）。</summary>
    [HttpPost("invoke-business-card")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<AiInvokeResultDto>>> InvokeBusinessCard(
        [FromForm] string scenarioCode,
        [FromForm] string? bizType,
        [FromForm] string? bizId,
        [FromForm] IFormFile? file,
        [FromForm] IFormFile? fileBack,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(scenarioCode))
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail("scenarioCode 不能为空"));
        if (file == null || file.Length <= 0)
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail("请上传名片正面图片"));

        var front = await ReadImageBase64Async(file, cancellationToken);
        if (front == null)
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail("名片正面图片格式无效"));

        var input = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["image_base64"] = front.Value.Base64,
            ["image_mime"] = front.Value.Mime
        };

        if (fileBack != null && fileBack.Length > 0)
        {
            var back = await ReadImageBase64Async(fileBack, cancellationToken);
            if (back == null)
                return BadRequest(ApiResponse<AiInvokeResultDto>.Fail("名片反面图片格式无效"));
            input["image_base64_2"] = back.Value.Base64;
            input["image_mime_2"] = back.Value.Mime;
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var request = new AiInvokeRequestDto
        {
            ScenarioCode = scenarioCode.Trim(),
            BizType = bizType,
            BizId = bizId,
            Input = input
        };

        try
        {
            var result = await _orchestrator.InvokeAsync(request, userId, cancellationToken);
            var msg = result.FromCache ? "命中缓存" : "调用成功";
            return Ok(ApiResponse<AiInvokeResultDto>.Ok(result, msg));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI business card invoke rejected scenario={Scenario}", scenarioCode);
            return BadRequest(ApiResponse<AiInvokeResultDto>.Fail(ex.Message));
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "AI business card invoke DB save failed scenario={Scenario}", scenarioCode);
            return StatusCode(500, ApiResponse<AiInvokeResultDto>.Fail(
                $"AI 调用日志写入失败，请确认已执行 AI 模块数据库脚本。详情: {detail}", 500));
        }
        catch (OperationCanceledException ex) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI business card invoke canceled scenario={Scenario}", scenarioCode);
            return StatusCode(504, ApiResponse<AiInvokeResultDto>.Fail("AI 调用超时，请稍后重试。", 504));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI business card invoke failed scenario={Scenario}", scenarioCode);
            return StatusCode(500, ApiResponse<AiInvokeResultDto>.Fail($"调用失败: {ex.Message}", 500));
        }
    }

    /// <summary>当前用户可调用的 AI 场景列表。</summary>
    [HttpGet("scenarios")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiScenarioListItemDto>>>> ListScenarios(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var list = await _adminService.ListInvokableScenariosForUserAsync(userId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AiScenarioListItemDto>>.Ok(list, "ok"));
    }

    /// <summary>entity.parse.* 用户在确认弹窗编辑后上报 confirmed_fields_json。</summary>
    [HttpPost("entity-parse-logs/{id}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmEntityParseLog(
        string id,
        [FromBody] AiEntityParseLogConfirmDto body,
        CancellationToken cancellationToken)
    {
        if (body.ConfirmedFields.ValueKind != System.Text.Json.JsonValueKind.Object)
            return BadRequest(ApiResponse<object>.Fail("confirmedFields 必须为 JSON 对象"));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            await _entityParseLogService.ConfirmAsync(id, userId ?? string.Empty, body.ConfirmedFields, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { id }, "已记录确认结果"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>entity.parse.* 用户保存成功后回写 saved_biz_id。</summary>
    [HttpPost("entity-parse-logs/{id}/saved")]
    public async Task<ActionResult<ApiResponse<object>>> MarkEntityParseLogSaved(
        string id,
        [FromBody] AiEntityParseLogSavedDto body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body?.SavedBizId))
            return BadRequest(ApiResponse<object>.Fail("savedBizId 不能为空"));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        try
        {
            await _entityParseLogService.MarkSavedAsync(id, userId ?? string.Empty, body.SavedBizId.Trim(), cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { id }, "已记录保存结果"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private static async Task<(string Base64, string Mime)?> ReadImageBase64Async(IFormFile file, CancellationToken cancellationToken)
    {
        var mime = (file.ContentType ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(mime))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            mime = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".heic" => "image/heic",
                _ => "image/jpeg"
            };
        }

        if (!mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return null;

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return (Convert.ToBase64String(ms.ToArray()), mime);
    }
}
