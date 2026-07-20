using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services;

public sealed class TelemetryService : ITelemetryService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        TelemetryEventTypes.Session,
        TelemetryEventTypes.Page,
        TelemetryEventTypes.Engagement,
        TelemetryEventTypes.Action,
        TelemetryEventTypes.Result,
        TelemetryEventTypes.Error,
        TelemetryEventTypes.Perf
    };

    private readonly ApplicationDbContext _db;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(ApplicationDbContext db, ILogger<TelemetryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TelemetryIngestResult> IngestAsync(
        IReadOnlyList<TelemetryIngestItem> items,
        string? userId,
        string? userName,
        CancellationToken cancellationToken = default)
    {
        var result = new TelemetryIngestResult();
        if (items.Count == 0) return result;

        var now = DateTime.UtcNow;
        var incomingIds = items
            .Select(x => (x.EventId ?? string.Empty).Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(TelemetryLimits.MaxEventsPerBatch)
            .ToList();

        var existing = await _db.TelemetryEvents.AsNoTracking()
            .Where(e => incomingIds.Contains(e.EventId))
            .Select(e => e.EventId)
            .ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toInsert = new List<TelemetryEvent>();
        foreach (var raw in items.Take(TelemetryLimits.MaxEventsPerBatch))
        {
            var eventId = (raw.EventId ?? string.Empty).Trim();
            var eventType = (raw.EventType ?? string.Empty).Trim();
            var eventName = (raw.EventName ?? string.Empty).Trim();
            if (eventId.Length == 0 || eventType.Length == 0 || eventName.Length == 0)
            {
                result.Rejected++;
                continue;
            }
            if (!AllowedTypes.Contains(eventType))
            {
                result.Rejected++;
                continue;
            }
            if (existingSet.Contains(eventId))
            {
                result.Duplicate++;
                continue;
            }

            existingSet.Add(eventId);
            var occurred = raw.OccurredAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(raw.OccurredAt, DateTimeKind.Utc)
                : raw.OccurredAt.ToUniversalTime();
            if (occurred > now.AddMinutes(10) || occurred < now.AddDays(-7))
                occurred = now;

            var payload = raw.PayloadJson;
            if (payload != null && payload.Length > TelemetryLimits.MaxPayloadChars)
                payload = payload[..TelemetryLimits.MaxPayloadChars];

            toInsert.Add(new TelemetryEvent
            {
                EventId = eventId.Length > 36 ? eventId[..36] : eventId,
                EventType = eventType.Length > 32 ? eventType[..32] : eventType,
                EventName = eventName.Length > 64 ? eventName[..64] : eventName,
                OccurredAt = occurred,
                ReceivedAt = now,
                SessionId = Trim(raw.SessionId, 36),
                UserId = Trim(userId, 36),
                UserName = Trim(userName, 50),
                PageKey = Trim(raw.PageKey, 200),
                RoutePath = Trim(raw.RoutePath, 500),
                Browser = Trim(raw.Browser, 80),
                Os = Trim(raw.Os, 80),
                DeviceType = Trim(raw.DeviceType, 40),
                ScreenW = raw.ScreenW,
                ScreenH = raw.ScreenH,
                UserAgent = Trim(raw.UserAgent, 500),
                PayloadJson = payload
            });
        }

        if (toInsert.Count == 0) return result;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _db.TelemetryEvents.AddRange(toInsert);
            await _db.SaveChangesAsync(cancellationToken);
            await ApplyDailyAggregatesAsync(toInsert, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            result.Accepted = toInsert.Count;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Telemetry ingest failed");
            throw;
        }

        return result;
    }

    private async Task ApplyDailyAggregatesAsync(
        IReadOnlyList<TelemetryEvent> events,
        CancellationToken cancellationToken)
    {
        var pageDeltas = new Dictionary<(DateOnly Day, string PageKey), PageDelta>();
        var actionDeltas = new Dictionary<(DateOnly Day, string PageKey, string ActionId), long>();
        var apiDeltas = new Dictionary<(DateOnly Day, string Method, string Path), ApiDelta>();

        foreach (var ev in events)
        {
            var day = DateOnly.FromDateTime(ev.OccurredAt);
            if (string.Equals(ev.EventName, "page_view", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(ev.PageKey))
            {
                GetPageDelta(pageDeltas, day, ev.PageKey!).ViewCount++;
            }
            else if (string.Equals(ev.EventName, "page_timing", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(ev.PageKey))
            {
                var (visible, active) = ReadTiming(ev.PayloadJson);
                var delta = GetPageDelta(pageDeltas, day, ev.PageKey!);
                delta.VisibleMsSum += visible;
                delta.ActiveMsSum += active;
            }
            else if (string.Equals(ev.EventType, TelemetryEventTypes.Action, StringComparison.OrdinalIgnoreCase))
            {
                var actionId = ReadString(ev.PayloadJson, "actionId")
                               ?? ReadString(ev.PayloadJson, "action_id");
                if (string.IsNullOrWhiteSpace(actionId)) continue;
                var key = (day, ev.PageKey ?? string.Empty, actionId);
                actionDeltas[key] = actionDeltas.GetValueOrDefault(key) + 1;
            }
            else if (string.Equals(ev.EventName, "api_timing", StringComparison.OrdinalIgnoreCase))
            {
                // 仅用 api_timing 汇总，避免与 api_error 双记
                var method = (ReadString(ev.PayloadJson, "method") ?? "GET").ToUpperInvariant();
                var path = ReadString(ev.PayloadJson, "pathTemplate")
                           ?? ReadString(ev.PayloadJson, "path_template")
                           ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path)) continue;
                var duration = (int)Math.Clamp(ReadLong(ev.PayloadJson, "durationMs"), 0, int.MaxValue);
                var status = (int)ReadLong(ev.PayloadJson, "status");
                var fail = status >= 400 || status == 0;
                var delta = GetApiDelta(apiDeltas, day, method, path);
                delta.CallCount++;
                if (fail) delta.FailCount++;
                delta.DurationMsSum += duration;
                if (duration > delta.DurationMsMax) delta.DurationMsMax = duration;
            }
        }

        foreach (var (key, delta) in pageDeltas)
        {
            await _db.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO telemetry_daily_page (stat_date, page_key, view_count, visible_ms_sum, active_ms_sum)
                VALUES ({key.Day}, {key.PageKey}, {delta.ViewCount}, {delta.VisibleMsSum}, {delta.ActiveMsSum})
                ON CONFLICT (stat_date, page_key) DO UPDATE SET
                  view_count = telemetry_daily_page.view_count + EXCLUDED.view_count,
                  visible_ms_sum = telemetry_daily_page.visible_ms_sum + EXCLUDED.visible_ms_sum,
                  active_ms_sum = telemetry_daily_page.active_ms_sum + EXCLUDED.active_ms_sum
                """,
                cancellationToken);
        }

        foreach (var (key, clickCount) in actionDeltas)
        {
            await _db.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO telemetry_daily_action (stat_date, page_key, action_id, click_count)
                VALUES ({key.Day}, {key.PageKey}, {key.ActionId}, {clickCount})
                ON CONFLICT (stat_date, page_key, action_id) DO UPDATE SET
                  click_count = telemetry_daily_action.click_count + EXCLUDED.click_count
                """,
                cancellationToken);
        }

        foreach (var (key, delta) in apiDeltas)
        {
            await _db.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO telemetry_daily_api (stat_date, method, path_template, call_count, fail_count, duration_ms_sum, duration_ms_max)
                VALUES ({key.Day}, {key.Method}, {key.Path}, {delta.CallCount}, {delta.FailCount}, {delta.DurationMsSum}, {delta.DurationMsMax})
                ON CONFLICT (stat_date, method, path_template) DO UPDATE SET
                  call_count = telemetry_daily_api.call_count + EXCLUDED.call_count,
                  fail_count = telemetry_daily_api.fail_count + EXCLUDED.fail_count,
                  duration_ms_sum = telemetry_daily_api.duration_ms_sum + EXCLUDED.duration_ms_sum,
                  duration_ms_max = GREATEST(telemetry_daily_api.duration_ms_max, EXCLUDED.duration_ms_max)
                """,
                cancellationToken);
        }
    }

    private static PageDelta GetPageDelta(
        Dictionary<(DateOnly Day, string PageKey), PageDelta> map, DateOnly day, string pageKey)
    {
        var key = (day, pageKey);
        if (!map.TryGetValue(key, out var delta))
        {
            delta = new PageDelta();
            map[key] = delta;
        }
        return delta;
    }

    private static ApiDelta GetApiDelta(
        Dictionary<(DateOnly Day, string Method, string Path), ApiDelta> map,
        DateOnly day,
        string method,
        string path)
    {
        var key = (day, method, path);
        if (!map.TryGetValue(key, out var delta))
        {
            delta = new ApiDelta();
            map[key] = delta;
        }
        return delta;
    }

    private sealed class PageDelta
    {
        public long ViewCount;
        public long VisibleMsSum;
        public long ActiveMsSum;
    }

    private sealed class ApiDelta
    {
        public long CallCount;
        public long FailCount;
        public long DurationMsSum;
        public int DurationMsMax;
    }

    public async Task<IReadOnlyList<TelemetryPageRankRow>> GetTopPagesAsync(
        DateOnly start, DateOnly end, int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        // 先拉取再内存聚合，避免 DateOnly/GroupBy 翻译差异导致 500
        var daily = await _db.TelemetryDailyPages.AsNoTracking()
            .Where(x => x.StatDate >= start && x.StatDate <= end)
            .ToListAsync(cancellationToken);

        List<TelemetryPageRankRow> rows;
        if (daily.Count > 0)
        {
            rows = daily
                .GroupBy(x => x.PageKey ?? string.Empty)
                .Select(g => new TelemetryPageRankRow
                {
                    PageKey = g.Key,
                    ViewCount = g.Sum(x => x.ViewCount),
                    VisibleMsSum = g.Sum(x => x.VisibleMsSum),
                    ActiveMsSum = g.Sum(x => x.ActiveMsSum)
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(take)
                .ToList();
        }
        else
        {
            // 日汇总为空时，从明细事件回退（page_view / page_timing）
            var from = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var events = await _db.TelemetryEvents.AsNoTracking()
                .Where(e => e.OccurredAt >= from && e.OccurredAt < to
                            && (e.EventName == "page_view" || e.EventName == "page_timing"))
                .Select(e => new { e.EventName, e.PageKey, e.PayloadJson })
                .ToListAsync(cancellationToken);

            rows = events
                .GroupBy(e => e.PageKey ?? string.Empty)
                .Select(g =>
                {
                    long visible = 0, active = 0;
                    foreach (var ev in g.Where(x => x.EventName == "page_timing"))
                    {
                        var (v, a) = ReadTiming(ev.PayloadJson);
                        visible += v;
                        active += a;
                    }
                    return new TelemetryPageRankRow
                    {
                        PageKey = g.Key,
                        ViewCount = g.Count(x => x.EventName == "page_view"),
                        VisibleMsSum = visible,
                        ActiveMsSum = active
                    };
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(take)
                .ToList();
        }

        foreach (var row in rows)
            row.Description = TelemetryCatalog.DescribePage(row.PageKey);
        return rows;
    }

    public async Task<IReadOnlyList<TelemetryActionRankRow>> GetTopActionsAsync(
        DateOnly start, DateOnly end, int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var daily = await _db.TelemetryDailyActions.AsNoTracking()
            .Where(x => x.StatDate >= start && x.StatDate <= end)
            .ToListAsync(cancellationToken);

        List<TelemetryActionRankRow> rows;
        if (daily.Count > 0)
        {
            rows = daily
                .GroupBy(x => new { PageKey = x.PageKey ?? string.Empty, ActionId = x.ActionId ?? string.Empty })
                .Select(g => new TelemetryActionRankRow
                {
                    PageKey = g.Key.PageKey,
                    ActionId = g.Key.ActionId,
                    ClickCount = g.Sum(x => x.ClickCount)
                })
                .OrderByDescending(x => x.ClickCount)
                .Take(take)
                .ToList();
        }
        else
        {
            var from = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var events = await _db.TelemetryEvents.AsNoTracking()
                .Where(e => e.OccurredAt >= from && e.OccurredAt < to
                            && e.EventType == TelemetryEventTypes.Action)
                .Select(e => new { e.PageKey, e.PayloadJson })
                .ToListAsync(cancellationToken);

            rows = events
                .Select(e => new
                {
                    PageKey = e.PageKey ?? string.Empty,
                    ActionId = ReadString(e.PayloadJson, "actionId")
                               ?? ReadString(e.PayloadJson, "action_id")
                               ?? string.Empty
                })
                .Where(x => x.ActionId.Length > 0)
                .GroupBy(x => new { x.PageKey, x.ActionId })
                .Select(g => new TelemetryActionRankRow
                {
                    PageKey = g.Key.PageKey,
                    ActionId = g.Key.ActionId,
                    ClickCount = g.Count()
                })
                .OrderByDescending(x => x.ClickCount)
                .Take(take)
                .ToList();
        }

        foreach (var row in rows)
            row.Description = TelemetryCatalog.DescribeAction(row.ActionId, row.PageKey);
        return rows;
    }

    public async Task<IReadOnlyList<TelemetryApiRankRow>> GetTopApisAsync(
        DateOnly start, DateOnly end, int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var daily = await _db.TelemetryDailyApis.AsNoTracking()
            .Where(x => x.StatDate >= start && x.StatDate <= end)
            .ToListAsync(cancellationToken);

        List<TelemetryApiRankRow> rows;
        if (daily.Count > 0)
        {
            rows = daily
                .GroupBy(x => new
                {
                    Method = (x.Method ?? "GET").ToUpperInvariant(),
                    Path = x.PathTemplate ?? string.Empty
                })
                .Select(g =>
                {
                    var callCount = g.Sum(x => x.CallCount);
                    var sum = g.Sum(x => x.DurationMsSum);
                    return new TelemetryApiRankRow
                    {
                        Method = g.Key.Method,
                        PathTemplate = g.Key.Path,
                        CallCount = callCount,
                        FailCount = g.Sum(x => x.FailCount),
                        AvgDurationMs = callCount == 0 ? 0 : Math.Round((double)sum / callCount, 1),
                        MaxDurationMs = g.Max(x => x.DurationMsMax)
                    };
                })
                .OrderByDescending(x => x.CallCount)
                .Take(take)
                .ToList();
        }
        else
        {
            var from = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var events = await _db.TelemetryEvents.AsNoTracking()
                .Where(e => e.OccurredAt >= from && e.OccurredAt < to && e.EventName == "api_timing")
                .Select(e => e.PayloadJson)
                .ToListAsync(cancellationToken);

            rows = events
                .Select(payload =>
                {
                    var method = (ReadString(payload, "method") ?? "GET").ToUpperInvariant();
                    var path = ReadString(payload, "pathTemplate")
                               ?? ReadString(payload, "path_template")
                               ?? string.Empty;
                    var duration = (int)Math.Clamp(ReadLong(payload, "durationMs"), 0, int.MaxValue);
                    var status = (int)ReadLong(payload, "status");
                    return new { method, path, duration, fail = status >= 400 || status == 0 };
                })
                .Where(x => x.path.Length > 0)
                .GroupBy(x => new { x.method, x.path })
                .Select(g => new TelemetryApiRankRow
                {
                    Method = g.Key.method,
                    PathTemplate = g.Key.path,
                    CallCount = g.Count(),
                    FailCount = g.Count(x => x.fail),
                    AvgDurationMs = Math.Round(g.Average(x => (double)x.duration), 1),
                    MaxDurationMs = g.Max(x => x.duration)
                })
                .OrderByDescending(x => x.CallCount)
                .Take(take)
                .ToList();
        }

        foreach (var row in rows)
            row.Description = TelemetryCatalog.DescribeApi(row.Method, row.PathTemplate);
        return rows;
    }

    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var eventCutoff = DateTime.UtcNow.AddDays(-TelemetryLimits.EventRetentionDays);
        var dailyCutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-TelemetryLimits.DailyRetentionDays));

        var deletedEvents = await _db.TelemetryEvents
            .Where(e => e.OccurredAt < eventCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var deletedPages = await _db.TelemetryDailyPages
            .Where(e => e.StatDate < dailyCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var deletedActions = await _db.TelemetryDailyActions
            .Where(e => e.StatDate < dailyCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var deletedApis = await _db.TelemetryDailyApis
            .Where(e => e.StatDate < dailyCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var total = deletedEvents + deletedPages + deletedActions + deletedApis;
        if (total > 0)
            _logger.LogInformation(
                "Telemetry cleanup: events={Events}, pages={Pages}, actions={Actions}, apis={Apis}",
                deletedEvents, deletedPages, deletedActions, deletedApis);
        return total;
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        return s.Length <= max ? s : s[..max];
    }

    private static (long visible, long active) ReadTiming(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (0, 0);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var visible = root.TryGetProperty("visibleMs", out var v) ? v.GetInt64()
                : root.TryGetProperty("visible_ms", out var v2) ? v2.GetInt64() : 0;
            var active = root.TryGetProperty("activeMs", out var a) ? a.GetInt64()
                : root.TryGetProperty("active_ms", out var a2) ? a2.GetInt64() : 0;
            return (Math.Max(0, visible), Math.Max(0, active));
        }
        catch
        {
            return (0, 0);
        }
    }

    private static string? ReadString(string? json, string name)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
        }
        catch
        {
            // ignore
        }
        return null;
    }

    private static long ReadLong(string? json, string name)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(name, out var p)) return 0;
            return p.ValueKind switch
            {
                JsonValueKind.Number => p.TryGetInt64(out var n) ? n : 0,
                JsonValueKind.String => long.TryParse(p.GetString(), out var n) ? n : 0,
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }
}
