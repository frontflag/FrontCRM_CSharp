using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/kb")]
[Authorize]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IKbHandbookService _kb;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<KnowledgeBaseController> _logger;

    public KnowledgeBaseController(
        IKbHandbookService kb,
        IWebHostEnvironment env,
        ILogger<KnowledgeBaseController> logger)
    {
        _kb = kb;
        _env = env;
        _logger = logger;
    }

    [HttpGet("documents/{code}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KbDocumentVersionDto>>>> ListVersions(
        string code,
        CancellationToken cancellationToken)
    {
        try
        {
            await _kb.EnsurePermissionAsync(UserId, KbHandbookCodes.AdminPermission, cancellationToken);
            var rows = await _kb.ListVersionsAsync(code, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<KbDocumentVersionDto>>.Ok(rows));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<KbDocumentVersionDto>>.Fail(ex.Message));
        }
    }

    [HttpGet("versions/{versionId}/chunks")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<KbChunkListItemDto>>>> ListChunks(
        string versionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _kb.EnsurePermissionAsync(UserId, KbHandbookCodes.AdminPermission, cancellationToken);
            var rows = await _kb.ListChunksAsync(versionId, page, pageSize, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<KbChunkListItemDto>>.Ok(rows));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<IReadOnlyList<KbChunkListItemDto>>.Fail(ex.Message));
        }
    }

    [HttpPost("documents/{code}/versions")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<KbImportResultDto>>> Upload(
        string code,
        [FromForm] string? title,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            await _kb.EnsurePermissionAsync(UserId, KbHandbookCodes.AdminPermission, cancellationToken);
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<KbImportResultDto>.Fail("请上传 docx 文件。"));
            if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(ApiResponse<KbImportResultDto>.Fail("只接受 docx 文件。"));

            var dir = Path.Combine(_env.ContentRootPath, "Uploads", "KB_DOCUMENT");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, Guid.NewGuid().ToString("N") + ".docx");
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream, cancellationToken);

            var result = await _kb.EnqueueDocxAsync(
                path,
                Path.GetFileName(file.FileName),
                string.IsNullOrWhiteSpace(code) ? KbHandbookCodes.DocumentCode : code.Trim(),
                string.IsNullOrWhiteSpace(title) ? "电子元器件分销行业新人培养教材（行业通用版）" : title.Trim(),
                cancellationToken);
            return Ok(ApiResponse<KbImportResultDto>.Ok(result, "已排队导入"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "KB upload rejected");
            return BadRequest(ApiResponse<KbImportResultDto>.Fail(ex.Message));
        }
    }

    [HttpPost("versions/{versionId}/activate")]
    public async Task<ActionResult<ApiResponse<bool>>> Activate(string versionId, CancellationToken cancellationToken)
    {
        try
        {
            await _kb.EnsurePermissionAsync(UserId, KbHandbookCodes.AdminPermission, cancellationToken);
            await _kb.ActivateAsync(versionId, cancellationToken);
            return Ok(ApiResponse<bool>.Ok(true, "已启用"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    [HttpGet("reader")]
    public async Task<ActionResult<ApiResponse<KbHandbookReaderDto>>> Reader(
        [FromQuery] string? versionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _kb.GetReaderAsync(UserId ?? "", versionId, cancellationToken);
            return Ok(ApiResponse<KbHandbookReaderDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<KbHandbookReaderDto>.Fail(ex.Message));
        }
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ApiResponse<KbAskResultDto>>> Ask(
        [FromBody] KbAskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _kb.AskAsync(UserId ?? "", request?.Question ?? "", cancellationToken);
            return Ok(ApiResponse<KbAskResultDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<KbAskResultDto>.Fail(ex.Message));
        }
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}

public sealed class KbAskRequest
{
    public string Question { get; set; } = "";
}
