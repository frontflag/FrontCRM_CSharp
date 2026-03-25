using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    /// <summary>
    /// 与界面展示相关的系统配置（无需登录即可拉取时区，供全站日期格式化）
    /// </summary>
    [ApiController]
    [Route("api/v1/system")]
    public class SystemDisplayController : ControllerBase
    {
        private readonly IDisplayTimeZoneService _displayTimeZoneService;
        private readonly ILogger<SystemDisplayController> _logger;

        public SystemDisplayController(
            IDisplayTimeZoneService displayTimeZoneService,
            ILogger<SystemDisplayController> logger)
        {
            _displayTimeZoneService = displayTimeZoneService;
            _logger = logger;
        }

        /// <summary>
        /// 获取展示用设置（当前含显示时区）
        /// </summary>
        [HttpGet("display")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<DisplaySettingsDto>>> GetDisplaySettings(
            CancellationToken cancellationToken)
        {
            try
            {
                var tz = await _displayTimeZoneService.GetDisplayTimeZoneIdAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(tz))
                    tz = SysParamCodes.DefaultDisplayTimeZoneId;

                var dto = new DisplaySettingsDto { DisplayTimeZoneId = tz.Trim() };
                return Ok(ApiResponse<DisplaySettingsDto>.Ok(dto, "ok"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取显示时区失败，使用默认");
                var fallback = new DisplaySettingsDto
                {
                    DisplayTimeZoneId = SysParamCodes.DefaultDisplayTimeZoneId
                };
                return Ok(ApiResponse<DisplaySettingsDto>.Ok(fallback, "ok"));
            }
        }
    }
}
