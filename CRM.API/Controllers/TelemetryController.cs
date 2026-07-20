using System.Security.Claims;
using CRM.API.Authorization;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
[Authorize]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetry;

    public TelemetryController(ITelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public sealed class TelemetryEventDto
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string? SessionId { get; set; }
        public string? PageKey { get; set; }
        public string? RoutePath { get; set; }
        public string? Browser { get; set; }
        public string? Os { get; set; }
        public string? DeviceType { get; set; }
        public int? ScreenW { get; set; }
        public int? ScreenH { get; set; }
        public string? UserAgent { get; set; }
        public object? Payload { get; set; }
        public string? PayloadJson { get; set; }
    }

    public sealed class TelemetryBatchRequest
    {
        public List<TelemetryEventDto> Events { get; set; } = new();
    }

    [HttpPost("events")]
    public async Task<ActionResult<ApiResponse<TelemetryIngestResult>>> Ingest(
        [FromBody] TelemetryBatchRequest? request,
        CancellationToken cancellationToken)
    {
        var events = request?.Events ?? new List<TelemetryEventDto>();
        if (events.Count == 0)
            return Ok(ApiResponse<TelemetryIngestResult>.Ok(new TelemetryIngestResult()));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;

        var items = events.Select(e => new TelemetryIngestItem
        {
            EventId = e.EventId,
            EventType = e.EventType,
            EventName = e.EventName,
            OccurredAt = e.OccurredAt,
            SessionId = e.SessionId,
            PageKey = e.PageKey,
            RoutePath = e.RoutePath,
            Browser = e.Browser,
            Os = e.Os,
            DeviceType = e.DeviceType,
            ScreenW = e.ScreenW,
            ScreenH = e.ScreenH,
            UserAgent = e.UserAgent,
            PayloadJson = ResolvePayloadJson(e)
        }).ToList();

        var result = await _telemetry.IngestAsync(items, userId, userName, cancellationToken);
        return Ok(ApiResponse<TelemetryIngestResult>.Ok(result));
    }

    [HttpGet("analytics/top-pages")]
    [RequirePermission(TelemetryPermissionCodes.Analytics)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TelemetryPageRankRow>>>> TopPages(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (start, end) = NormalizeRange(startDate, endDate);
            var rows = await _telemetry.GetTopPagesAsync(start, end, take, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<TelemetryPageRankRow>>.Ok(rows));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IReadOnlyList<TelemetryPageRankRow>>.Fail(
                $"查询高频页面失败: {ex.Message}", 500));
        }
    }

    [HttpGet("analytics/top-actions")]
    [RequirePermission(TelemetryPermissionCodes.Analytics)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TelemetryActionRankRow>>>> TopActions(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (start, end) = NormalizeRange(startDate, endDate);
            var rows = await _telemetry.GetTopActionsAsync(start, end, take, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<TelemetryActionRankRow>>.Ok(rows));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IReadOnlyList<TelemetryActionRankRow>>.Fail(
                $"查询高频操作失败: {ex.Message}", 500));
        }
    }

    [HttpGet("analytics/top-apis")]
    [RequirePermission(TelemetryPermissionCodes.Analytics)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TelemetryApiRankRow>>>> TopApis(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (start, end) = NormalizeRange(startDate, endDate);
            var rows = await _telemetry.GetTopApisAsync(start, end, take, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<TelemetryApiRankRow>>.Ok(rows));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IReadOnlyList<TelemetryApiRankRow>>.Fail(
                $"查询 API 耗时失败: {ex.Message}", 500));
        }
    }

    private static (DateOnly start, DateOnly end) NormalizeRange(string? startDate, string? endDate)
    {
        var end = DateOnly.TryParse(endDate, out var e)
            ? e
            : DateOnly.FromDateTime(DateTime.UtcNow);
        var start = DateOnly.TryParse(startDate, out var s)
            ? s
            : end.AddDays(-7);
        if (start > end) (start, end) = (end, start);
        if (end.DayNumber - start.DayNumber > 90) start = end.AddDays(-90);
        return (start, end);
    }

    private static string? ResolvePayloadJson(TelemetryEventDto e)
    {
        if (!string.IsNullOrWhiteSpace(e.PayloadJson)) return e.PayloadJson;
        if (e.Payload == null) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Serialize(e.Payload);
        }
        catch
        {
            return null;
        }
    }
}
