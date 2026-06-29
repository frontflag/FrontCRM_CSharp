using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Tag;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly ITagApplyService _tagApplyService;
        private readonly ITagFilterService _tagFilterService;
        private readonly IRfqTagService _rfqTagService;
        private readonly IEntityLookupService _entityLookup;
        private readonly ILogger<TagsController> _logger;

        public TagsController(
            ITagService tagService,
            ITagApplyService tagApplyService,
            ITagFilterService tagFilterService,
            IRfqTagService rfqTagService,
            IEntityLookupService entityLookup,
            ILogger<TagsController> logger)
        {
            _tagService = tagService;
            _tagApplyService = tagApplyService;
            _tagFilterService = tagFilterService;
            _rfqTagService = rfqTagService;
            _entityLookup = entityLookup;
            _logger = logger;
        }

        private string? GetCurrentUserId() =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Trim();

        private async Task<string?> GetCurrentUserDisplayNameAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _entityLookup.GetUserDisplayNameAsync(userId);
        }

        private static bool IsRfqEntityType(string? entityType) =>
            string.Equals(entityType?.Trim(), RfqTagConstants.EntityType, StringComparison.OrdinalIgnoreCase);

        // ===== 标签管理 =====

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetTags(
            [FromQuery] short? type,
            [FromQuery] string? category,
            [FromQuery] string? entityType,
            [FromQuery] string? keyword,
            [FromQuery] short? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var request = new TagSearchRequest
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Type = type,
                    Category = category,
                    EntityType = entityType,
                    Keyword = keyword,
                    Status = status
                };

                var result = await _tagService.SearchTagsAsync(request);
                var items = result.Items.AsEnumerable();
                if (IsRfqEntityType(entityType))
                {
                    var userId = GetCurrentUserId();
                    if (string.IsNullOrWhiteSpace(userId))
                        items = items.Where(t => t.Type == 1);
                    else
                        items = items.Where(t => t.Type == 1 || RfqTagConstants.IsOwnedByUser(t, userId));
                }

                var list = items.ToList();
                return Ok(ApiResponse<object>.Ok(new
                {
                    items = list,
                    totalCount = list.Count,
                    pageNumber = result.PageIndex,
                    pageSize = result.PageSize,
                    totalPages = result.TotalPages
                }, "获取标签列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取标签列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取标签列表失败: {ex.Message}", 500));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TagDefinition>>> CreateTag([FromBody] CreateTagRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<TagDefinition>.Fail("未登录", 401));

                var scope = !string.IsNullOrWhiteSpace(request.Scope)
                    ? request.Scope
                    : request.EntityType;
                if (IsRfqEntityType(scope))
                {
                    var tag = await _rfqTagService.CreateUserRfqTagAsync(request.Name, request.Color, userId);
                    return Ok(ApiResponse<TagDefinition>.Ok(tag, "创建标签成功"));
                }

                var created = await _tagService.CreateTagAsync(request, 0);
                return Ok(ApiResponse<TagDefinition>.Ok(created, "创建标签成功"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<TagDefinition>.Fail(ex.Message, 400));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<TagDefinition>.Fail(ex.Message, 400));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建标签失败");
                return StatusCode(500, ApiResponse<TagDefinition>.Fail($"创建标签失败: {ex.Message}", 500));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<TagDefinition>>> UpdateTag(string id, [FromBody] UpdateTagRequest request)
        {
            try
            {
                var tag = await _tagService.UpdateTagAsync(id, request, 0);
                return Ok(ApiResponse<TagDefinition>.Ok(tag, "更新标签成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<TagDefinition>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新标签失败");
                return StatusCode(500, ApiResponse<TagDefinition>.Fail($"更新标签失败: {ex.Message}", 500));
            }
        }

        [HttpPatch("{id}/enable")]
        public async Task<ActionResult<ApiResponse<object>>> EnableTag(string id)
        {
            try
            {
                await _tagService.EnableTagAsync(id, 0);
                return Ok(ApiResponse<object>.Ok(null, "启用标签成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用标签失败");
                return StatusCode(500, ApiResponse<object>.Fail($"启用标签失败: {ex.Message}", 500));
            }
        }

        [HttpPatch("{id}/disable")]
        public async Task<ActionResult<ApiResponse<object>>> DisableTag(string id)
        {
            try
            {
                await _tagService.DisableTagAsync(id, 0);
                return Ok(ApiResponse<object>.Ok(null, "停用标签成功"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停用标签失败");
                return StatusCode(500, ApiResponse<object>.Fail($"停用标签失败: {ex.Message}", 500));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteTag(string id)
        {
            try
            {
                await _tagService.DeleteTagAsync(id);
                return Ok(ApiResponse<object>.Ok(null, "删除标签成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除标签失败");
                return StatusCode(500, ApiResponse<object>.Fail($"删除标签失败: {ex.Message}", 500));
            }
        }

        // ===== 标签应用 =====

        [HttpPost("apply")]
        public async Task<ActionResult<ApiResponse<object>>> ApplyTags([FromBody] AddTagsToEntityRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

                if (IsRfqEntityType(request.EntityType))
                {
                    if (request.EntityIds == null || request.EntityIds.Count != 1)
                        return BadRequest(ApiResponse<object>.Fail("需求标签仅支持单条记录操作", 400));
                    var userName = await GetCurrentUserDisplayNameAsync(userId);
                    await _rfqTagService.ApplyTagsAsync(
                        request.EntityIds[0],
                        request.TagIds ?? new List<string>(),
                        userId,
                        userName);
                    return Ok(ApiResponse<object>.Ok(null, "打标签成功"));
                }

                request.AppliedByUserId = 0;
                await _tagApplyService.AddTagsToEntityAsync(request);
                return Ok(ApiResponse<object>.Ok(null, "打标签成功"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打标签失败");
                return StatusCode(500, ApiResponse<object>.Fail($"打标签失败: {ex.Message}", 500));
            }
        }

        [HttpPost("remove")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveTags([FromBody] RemoveTagsFromEntityRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                var userId = GetCurrentUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

                if (IsRfqEntityType(request.EntityType))
                {
                    if (request.EntityIds == null || request.EntityIds.Count != 1)
                        return BadRequest(ApiResponse<object>.Fail("需求标签仅支持单条记录操作", 400));
                    var userName = await GetCurrentUserDisplayNameAsync(userId);
                    await _rfqTagService.RemoveTagsAsync(
                        request.EntityIds[0],
                        request.TagIds ?? new List<string>(),
                        userId,
                        userName);
                    return Ok(ApiResponse<object>.Ok(null, "移除标签成功"));
                }

                request.AppliedByUserId = 0;
                await _tagApplyService.RemoveTagsFromEntityAsync(request);
                return Ok(ApiResponse<object>.Ok(null, "移除标签成功"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除标签失败");
                return StatusCode(500, ApiResponse<object>.Fail($"移除标签失败: {ex.Message}", 500));
            }
        }

        [HttpGet("entities/{entityType}/{entityId}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EntityTagDto>>>> GetEntityTags(string entityType, string entityId)
        {
            try
            {
                if (IsRfqEntityType(entityType))
                {
                    var userId = GetCurrentUserId();
                    var tags = await _rfqTagService.GetTagsForRfqAsync(entityId, userId);
                    return Ok(ApiResponse<IReadOnlyList<EntityTagDto>>.Ok(tags, "获取实体标签成功"));
                }

                var legacyTags = await _tagApplyService.GetTagsForEntityAsync(entityType, entityId);
                var dtos = legacyTags.Select(t => new EntityTagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Color = t.Color,
                    Type = t.Type
                }).ToList();
                return Ok(ApiResponse<IReadOnlyList<EntityTagDto>>.Ok(dtos, "获取实体标签成功"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<IReadOnlyList<EntityTagDto>>.Fail(ex.Message, 403));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<IReadOnlyList<EntityTagDto>>.Fail(ex.Message, 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体标签失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<EntityTagDto>>.Fail($"获取实体标签失败: {ex.Message}", 500));
            }
        }

        [HttpGet("user/common")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TagDefinition>>>> GetUserCommonTags([FromQuery] string? entityType)
        {
            try
            {
                var tags = await _tagApplyService.GetUserCommonTagsAsync(0, entityType);
                return Ok(ApiResponse<IReadOnlyList<TagDefinition>>.Ok(tags, "获取常用标签成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取常用标签失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<TagDefinition>>.Fail($"获取常用标签失败: {ex.Message}", 500));
            }
        }

        [HttpGet("user/recent")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TagDefinition>>>> GetUserRecentTags([FromQuery] string? entityType)
        {
            try
            {
                var tags = await _tagApplyService.GetUserRecentTagsAsync(0, entityType);
                return Ok(ApiResponse<IReadOnlyList<TagDefinition>>.Ok(tags, "获取最近使用标签成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取最近使用标签失败");
                return StatusCode(500, ApiResponse<IReadOnlyList<TagDefinition>>.Fail($"获取最近使用标签失败: {ex.Message}", 500));
            }
        }

        // ===== 标签筛选 =====

        [HttpPost("filter")]
        public async Task<ActionResult<ApiResponse<object>>> FilterEntities([FromBody] TagFilterRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<object>.Fail("请求体不能为空", 400));

                var ids = await _tagFilterService.QueryEntityIdsByTagsAsync(request);
                return Ok(ApiResponse<object>.Ok(new { entityIds = ids }, "标签筛选成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标签筛选失败");
                return StatusCode(500, ApiResponse<object>.Fail($"标签筛选失败: {ex.Message}", 500));
            }
        }
    }
}
