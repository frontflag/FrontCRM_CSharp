using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(IFavoriteService favoriteService, ILogger<FavoritesController> logger)
        {
            _favoriteService = favoriteService;
            _logger = logger;
        }

        private long GetCurrentUserId()
        {
            // 占位实现：后续可从登录态中解析
            return 0;
        }

        public class FavoriteToggleRequest
        {
            public string EntityType { get; set; } = string.Empty;
            public string EntityId { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> AddFavorite([FromBody] FavoriteToggleRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.EntityType) || string.IsNullOrWhiteSpace(request.EntityId))
                    return BadRequest(ApiResponse<object>.Fail("实体类型和实体ID不能为空", 400));

                await _favoriteService.AddFavoriteAsync(GetCurrentUserId(), request.EntityType, request.EntityId);
                return Ok(ApiResponse<object>.Ok(null, "收藏成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "收藏失败");
                return StatusCode(500, ApiResponse<object>.Fail($"收藏失败: {ex.Message}", 500));
            }
        }

        [HttpDelete]
        public async Task<ActionResult<ApiResponse<object>>> RemoveFavorite([FromQuery] string entityType, [FromQuery] string entityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
                    return BadRequest(ApiResponse<object>.Fail("实体类型和实体ID不能为空", 400));

                await _favoriteService.RemoveFavoriteAsync(GetCurrentUserId(), entityType, entityId);
                return Ok(ApiResponse<object>.Ok(null, "取消收藏成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消收藏失败");
                return StatusCode(500, ApiResponse<object>.Fail($"取消收藏失败: {ex.Message}", 500));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetFavoriteEntityIds([FromQuery] string entityType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entityType))
                    return BadRequest(ApiResponse<object>.Fail("实体类型不能为空", 400));

                var ids = await _favoriteService.GetFavoriteEntityIdsAsync(GetCurrentUserId(), entityType);
                return Ok(ApiResponse<object>.Ok(new { entityIds = ids }, "获取收藏列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收藏列表失败");
                return StatusCode(500, ApiResponse<object>.Fail($"获取收藏列表失败: {ex.Message}", 500));
            }
        }

        [HttpGet("check")]
        public async Task<ActionResult<ApiResponse<object>>> CheckFavorite([FromQuery] string entityType, [FromQuery] string entityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
                    return BadRequest(ApiResponse<object>.Fail("实体类型和实体ID不能为空", 400));

                var isFavorite = await _favoriteService.IsFavoriteAsync(GetCurrentUserId(), entityType, entityId);
                return Ok(ApiResponse<object>.Ok(new { isFavorite }, "查询收藏状态成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询收藏状态失败");
                return StatusCode(500, ApiResponse<object>.Fail($"查询收藏状态失败: {ex.Message}", 500));
            }
        }
    }
}
