using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DraftsController : ControllerBase
    {
        private readonly IDraftService _draftService;
        private readonly ILogger<DraftsController> _logger;

        public DraftsController(IDraftService draftService, ILogger<DraftsController> logger)
        {
            _draftService = draftService;
            _logger = logger;
        }

        private long GetCurrentUserId()
        {
            // 占位实现：后续可从登录态中解析
            return 0;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<DraftDto>>> SaveDraft([FromBody] SaveDraftRequest request)
        {
            try
            {
                var draft = await _draftService.SaveDraftAsync(GetCurrentUserId(), request);
                return Ok(ApiResponse<DraftDto>.Ok(draft, "保存草稿成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<DraftDto>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存草稿失败");
                return StatusCode(500, ApiResponse<DraftDto>.Fail($"保存草稿失败: {ex.Message}", 500));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<DraftDto>>>> GetDrafts(
            [FromQuery] string? entityType = null,
            [FromQuery] short? status = null,
            [FromQuery] string? keyword = null)
        {
            try
            {
                var items = await _draftService.GetDraftsAsync(GetCurrentUserId(), new GetDraftsRequest
                {
                    EntityType = entityType,
                    Status = status,
                    Keyword = keyword
                });
                return Ok(ApiResponse<IReadOnlyList<DraftDto>>.Ok(items, "获取草稿列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草稿列表失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<DraftDto>>.Fail($"获取草稿列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("{draftId}")]
        public async Task<ActionResult<ApiResponse<DraftDto>>> GetDraftById(string draftId)
        {
            try
            {
                var draft = await _draftService.GetDraftByIdAsync(GetCurrentUserId(), draftId);
                if (draft == null)
                    return NotFound(ApiResponse<DraftDto>.Fail("草稿不存在", 404));

                return Ok(ApiResponse<DraftDto>.Ok(draft, "获取草稿详情成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草稿详情失败: {DraftId}", draftId);
                return StatusCode(500, ApiResponse<DraftDto>.Fail($"获取草稿详情失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{draftId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteDraft(string draftId)
        {
            try
            {
                await _draftService.DeleteDraftAsync(GetCurrentUserId(), draftId);
                return Ok(ApiResponse<object>.Ok(null, "删除草稿成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除草稿失败: {DraftId}", draftId);
                return StatusCode(500, ApiResponse<object>.Fail($"删除草稿失败: {ex.Message}", 500));
            }
        }

        [HttpPost("{draftId}/convert")]
        public async Task<ActionResult<ApiResponse<DraftConvertResultDto>>> ConvertDraft(string draftId)
        {
            try
            {
                var result = await _draftService.ConvertDraftAsync(GetCurrentUserId(), draftId);
                return Ok(ApiResponse<DraftConvertResultDto>.Ok(result, "草稿转正式成功"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 400));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 409));
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 400));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<DraftConvertResultDto>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "草稿转正式失败: {DraftId}", draftId);
                return StatusCode(500, ApiResponse<DraftConvertResultDto>.Fail($"草稿转正式失败: {ex.Message}", 500));
            }
        }
    }
}
