using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers
{
    [ApiController]
    [Route("api/v1/log-recent")]
    [Authorize]
    public class LogRecentController : ControllerBase
    {
        private readonly ILogRecentService _logRecentService;
        private readonly ILogger<LogRecentController> _logger;

        public LogRecentController(ILogRecentService logRecentService, ILogger<LogRecentController> logger)
        {
            _logRecentService = logRecentService;
            _logger = logger;
        }

        public class RecordLogRecentRequest
        {
            public string BizType { get; set; } = string.Empty;
            public string RecordId { get; set; } = string.Empty;
            public string? RecordCode { get; set; }
            /// <summary>detail | edit</summary>
            public string OpenKind { get; set; } = "detail";
        }

        public class LogRecentItemDto
        {
            public string RecordId { get; set; } = string.Empty;
            public string? RecordCode { get; set; }
            public DateTime AccessedAt { get; set; }
            public string OpenKind { get; set; } = string.Empty;
        }

        public class LogRecentListDto
        {
            public List<LogRecentItemDto> Items { get; set; } = new();
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Record([FromBody] RecordLogRecentRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<object>.Fail("未登录", 401));

                if (request == null ||
                    string.IsNullOrWhiteSpace(request.BizType) ||
                    string.IsNullOrWhiteSpace(request.RecordId))
                    return BadRequest(ApiResponse<object>.Fail("BizType 与 RecordId 不能为空", 400));

                var kind = (request.OpenKind ?? "detail").Trim().ToLowerInvariant();
                if (kind != "detail" && kind != "edit")
                    return BadRequest(ApiResponse<object>.Fail("OpenKind 仅支持 detail 或 edit", 400));

                if (request.BizType.Length > 64)
                    return BadRequest(ApiResponse<object>.Fail("BizType 过长", 400));

                await _logRecentService.RecordAsync(
                    userId,
                    request.BizType.Trim(),
                    request.RecordId.Trim(),
                    string.IsNullOrWhiteSpace(request.RecordCode) ? null : request.RecordCode.Trim(),
                    kind);

                return Ok(ApiResponse<object>.Ok(null, "已记录"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录最近访问失败");
                return StatusCode(500, ApiResponse<object>.Fail($"记录失败: {ex.Message}", 500));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<LogRecentListDto>>> List([FromQuery] string bizType, [FromQuery] int take = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(ApiResponse<LogRecentListDto>.Fail("未登录", 401));

                if (string.IsNullOrWhiteSpace(bizType))
                    return BadRequest(ApiResponse<LogRecentListDto>.Fail("bizType 不能为空", 400));

                var rows = await _logRecentService.ListAsync(userId, bizType.Trim(), take);
                var dto = new LogRecentListDto
                {
                    Items = rows.Select(r => new LogRecentItemDto
                    {
                        RecordId = r.RecordId,
                        RecordCode = r.RecordCode,
                        AccessedAt = r.AccessedAt,
                        OpenKind = r.OpenKind
                    }).ToList()
                };
                return Ok(ApiResponse<LogRecentListDto>.Ok(dto, "查询成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询最近访问失败");
                return StatusCode(500, ApiResponse<LogRecentListDto>.Fail($"查询失败: {ex.Message}", 500));
            }
        }
    }
}
