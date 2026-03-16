using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    /// <summary>
    /// 物料查询控制器
    /// 提供物料详情查询接口，支持24小时数据库缓存
    /// </summary>
    [ApiController]
    [Route("api/v1/components")]
    [Authorize]
    public class ComponentsController : ControllerBase
    {
        private readonly IComponentCacheService _cacheService;
        private readonly ILogger<ComponentsController> _logger;

        public ComponentsController(
            IComponentCacheService cacheService,
            ILogger<ComponentsController> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// 根据 MPN（物料型号）查询物料详情
        /// - 24小时内有缓存：直接返回数据库数据（IsFromCache=true）
        /// - 超过24小时或无缓存：调用外部数据服务并写入数据库（IsFromCache=false）
        /// </summary>
        /// <param name="mpn">物料型号，如 LM358N、NE555P、STM32F103C8T6</param>
        [HttpGet("{mpn}")]
        public async Task<IActionResult> GetByMpn(string mpn)
        {
            if (string.IsNullOrWhiteSpace(mpn))
                return BadRequest(ApiResponse<object>.Fail("物料型号不能为空"));

            try
            {
                var data = await _cacheService.GetByMpnAsync(mpn);
                if (data == null)
                    return NotFound(ApiResponse<object>.Fail($"未找到物料 '{mpn}' 的数据"));

                return Ok(ApiResponse<object>.Ok(data,
                    data.IsFromCache ? $"数据来自缓存（{data.FetchedAt:yyyy-MM-dd HH:mm} UTC）" : "数据已更新"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询物料失败: MPN={Mpn}", mpn);
                return StatusCode(500, ApiResponse<object>.Fail($"查询失败: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// 强制刷新物料缓存（忽略24小时限制，重新从外部数据源获取）
        /// </summary>
        [HttpPost("{mpn}/refresh")]
        public async Task<IActionResult> RefreshByMpn(string mpn)
        {
            if (string.IsNullOrWhiteSpace(mpn))
                return BadRequest(ApiResponse<object>.Fail("物料型号不能为空"));

            try
            {
                var data = await _cacheService.RefreshByMpnAsync(mpn);
                if (data == null)
                    return NotFound(ApiResponse<object>.Fail($"未找到物料 '{mpn}' 的数据"));

                return Ok(ApiResponse<object>.Ok(data, "缓存已强制刷新"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制刷新物料缓存失败: MPN={Mpn}", mpn);
                return StatusCode(500, ApiResponse<object>.Fail($"刷新失败: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// 获取物料缓存元信息（上次获取时间、数据来源、查询次数、是否已过期）
        /// </summary>
        [HttpGet("{mpn}/cache-info")]
        public async Task<IActionResult> GetCacheInfo(string mpn)
        {
            if (string.IsNullOrWhiteSpace(mpn))
                return BadRequest(ApiResponse<object>.Fail("物料型号不能为空"));

            try
            {
                var meta = await _cacheService.GetCacheMetaAsync(mpn);
                if (meta == null)
                    return NotFound(ApiResponse<object>.Fail($"物料 '{mpn}' 尚无缓存记录"));

                return Ok(ApiResponse<object>.Ok(meta, "获取缓存信息成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存信息失败: MPN={Mpn}", mpn);
                return StatusCode(500, ApiResponse<object>.Fail($"获取失败: {ex.Message}", 500));
            }
        }
    }
}
