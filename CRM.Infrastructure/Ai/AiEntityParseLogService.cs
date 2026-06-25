using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using CRM.Infrastructure.Ai.EntityParse;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Ai;

public sealed class AiEntityParseLogService : IAiEntityParseLogService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRbacService _rbacService;

    public AiEntityParseLogService(ApplicationDbContext db, IUnitOfWork unitOfWork, IRbacService rbacService)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _rbacService = rbacService;
    }

    public async Task<EntityParseLogCreateResult?> TryCreateParsedLogAsync(
        EntityParseLogCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!EntityParseNormalizer.IsEntityParseScenario(request.ScenarioCode))
            return null;

        var entityType = EntityParseNormalizer.EntityTypeFromScenario(request.ScenarioCode)
                         ?? (request.EntityType ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(entityType))
            return null;

        JsonElement? rawElement = null;
        if (request.RawLlmObject is JsonElement je)
            rawElement = je;
        else if (request.RawLlmObject != null)
        {
            var serialized = JsonSerializer.Serialize(request.RawLlmObject);
            using var doc = JsonDocument.Parse(serialized);
            rawElement = doc.RootElement.Clone();
        }

        if (rawElement == null || rawElement.Value.ValueKind != JsonValueKind.Object)
            return null;

        var normalized = EntityParseNormalizer.Normalize(request.ScenarioCode, rawElement.Value);
        if (normalized == null)
            return null;

        var normalizedJson = AiJsonHelper.CoerceJsonObjectForJsonb(normalized.ToJsonString()) ?? "{}";
        var parentBizType = EntityParseNormalizer.ParentBizTypeFromEntityType(entityType);
        var parentBizId = parentBizType == null
            ? null
            : string.IsNullOrWhiteSpace(request.ParentBizId) ? null : request.ParentBizId.Trim();

        var log = new AiEntityParseLog
        {
            Id = Guid.NewGuid().ToString(),
            InvocationId = request.InvocationId,
            ScenarioCode = request.ScenarioCode,
            EntityType = entityType,
            UserId = string.IsNullOrWhiteSpace(request.UserId) ? null : request.UserId.Trim(),
            ParentBizType = parentBizType,
            ParentBizId = parentBizId,
            RawText = request.RawText ?? string.Empty,
            ParseResultRaw = request.ParseResultRaw,
            ParseResultJson = normalizedJson,
            Outcome = AiEntityParseOutcomeCode.Parsed,
            TemplateVersion = request.TemplateVersion,
            ProviderCode = request.ProviderCode,
            Model = request.Model,
            FromCache = request.FromCache,
            LatencyMs = request.LatencyMs,
            CreatedAt = DateTime.UtcNow
        };

        await _db.AiEntityParseLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        return new EntityParseLogCreateResult
        {
            LogId = log.Id,
            NormalizedData = JsonSerializer.Deserialize<object>(normalizedJson)
        };
    }

    public async Task ConfirmAsync(
        string logId,
        string userId,
        JsonElement confirmedFields,
        CancellationToken cancellationToken = default)
    {
        var id = (logId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException("parseLogId 不能为空。");

        var uid = (userId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(uid))
            throw new InvalidOperationException("未登录，无法确认解析结果。");

        var log = await _db.AiEntityParseLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
                  ?? throw new InvalidOperationException("解析日志不存在。");

        if (!string.Equals(log.UserId, uid, StringComparison.Ordinal))
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(uid);
            if (!summary.IsSysAdmin)
                throw new InvalidOperationException("无权确认该解析日志。");
        }

        if (string.Equals(log.Outcome, AiEntityParseOutcomeCode.Confirmed, StringComparison.OrdinalIgnoreCase))
            return;

        if (confirmedFields.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("confirmedFields 必须为 JSON 对象。");

        var json = AiJsonHelper.CoerceJsonObjectForJsonb(confirmedFields.GetRawText())
                   ?? throw new InvalidOperationException("confirmedFields 不是合法 JSON 对象。");

        log.ConfirmedFieldsJson = json;
        log.Outcome = AiEntityParseOutcomeCode.Confirmed;
        log.ConfirmedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkSavedAsync(
        string logId,
        string userId,
        string savedBizId,
        CancellationToken cancellationToken = default)
    {
        var id = (logId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException("parseLogId 不能为空。");

        var bizId = (savedBizId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(bizId))
            throw new InvalidOperationException("savedBizId 不能为空。");

        var uid = (userId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(uid))
            throw new InvalidOperationException("未登录，无法记录保存结果。");

        var log = await _db.AiEntityParseLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
                  ?? throw new InvalidOperationException("解析日志不存在。");

        if (!string.Equals(log.UserId, uid, StringComparison.Ordinal))
        {
            var summary = await _rbacService.GetUserPermissionSummaryAsync(uid);
            if (!summary.IsSysAdmin)
                throw new InvalidOperationException("无权更新该解析日志。");
        }

        if (string.Equals(log.Outcome, AiEntityParseOutcomeCode.Saved, StringComparison.OrdinalIgnoreCase))
            return;

        log.SavedBizId = bizId;
        log.SavedAt = DateTime.UtcNow;
        log.Outcome = AiEntityParseOutcomeCode.Saved;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AiEntityParseLogListItemDto>> ListForAdminAsync(
        AiEntityParseLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(query.Take, 1, 500);
        var q = _db.AiEntityParseLogs.AsNoTracking().AsQueryable();

        var scenarioCode = (query.ScenarioCode ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(scenarioCode))
            q = q.Where(l => l.ScenarioCode == scenarioCode);

        var entityType = (query.EntityType ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(entityType))
            q = q.Where(l => l.EntityType == entityType);

        var outcome = (query.Outcome ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(outcome))
            q = q.Where(l => l.Outcome == outcome);

        var userId = (query.UserId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(userId))
            q = q.Where(l => l.UserId == userId);

        var rows = await q
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.Select(MapListItem).ToList();
    }

    public async Task<AiEntityParseLogDetailDto?> GetDetailForAdminAsync(
        string logId,
        CancellationToken cancellationToken = default)
    {
        var id = (logId ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id)) return null;

        var log = await _db.AiEntityParseLogs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (log == null) return null;

        var detail = new AiEntityParseLogDetailDto
        {
            Id = log.Id,
            InvocationId = log.InvocationId,
            ScenarioCode = log.ScenarioCode,
            EntityType = log.EntityType,
            UserId = log.UserId,
            ParentBizType = log.ParentBizType,
            ParentBizId = log.ParentBizId,
            Outcome = log.Outcome,
            SavedBizId = log.SavedBizId,
            RawTextLength = log.RawText?.Length ?? 0,
            FromCache = log.FromCache,
            LatencyMs = log.LatencyMs,
            ProviderCode = log.ProviderCode,
            Model = log.Model,
            CreatedAt = log.CreatedAt,
            ConfirmedAt = log.ConfirmedAt,
            SavedAt = log.SavedAt,
            RawText = log.RawText,
            ParseResultRaw = log.ParseResultRaw,
            ParseResultJson = DeserializeJsonObject(log.ParseResultJson),
            ConfirmedFieldsJson = DeserializeJsonObject(log.ConfirmedFieldsJson)
        };
        return detail;
    }

    public async Task<byte[]> ExportCsvAsync(
        AiEntityParseLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var exportQuery = new AiEntityParseLogQueryDto
        {
            Take = Math.Clamp(query.Take, 1, 5000),
            ScenarioCode = query.ScenarioCode,
            EntityType = query.EntityType,
            Outcome = query.Outcome,
            UserId = query.UserId
        };
        var rows = await ListForAdminAsync(exportQuery, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("id,invocation_id,scenario_code,entity_type,user_id,parent_biz_type,parent_biz_id,outcome,saved_biz_id,raw_text_length,from_cache,latency_ms,provider_code,model,created_at,confirmed_at,saved_at");
        foreach (var row in rows)
        {
            sb.Append(Csv(row.Id)).Append(',')
                .Append(Csv(row.InvocationId)).Append(',')
                .Append(Csv(row.ScenarioCode)).Append(',')
                .Append(Csv(row.EntityType)).Append(',')
                .Append(Csv(row.UserId)).Append(',')
                .Append(Csv(row.ParentBizType)).Append(',')
                .Append(Csv(row.ParentBizId)).Append(',')
                .Append(Csv(row.Outcome)).Append(',')
                .Append(Csv(row.SavedBizId)).Append(',')
                .Append(row.RawTextLength).Append(',')
                .Append(row.FromCache ? '1' : '0').Append(',')
                .Append(row.LatencyMs).Append(',')
                .Append(Csv(row.ProviderCode)).Append(',')
                .Append(Csv(row.Model)).Append(',')
                .Append(Csv(row.CreatedAt.ToString("O"))).Append(',')
                .Append(Csv(row.ConfirmedAt?.ToString("O"))).Append(',')
                .Append(Csv(row.SavedAt?.ToString("O")))
                .AppendLine();
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<int> PurgeOlderThanAsync(int keepDays, CancellationToken cancellationToken = default)
    {
        var days = Math.Clamp(keepDays, 1, 3650);
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _db.AiEntityParseLogs
            .Where(l => l.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static AiEntityParseLogListItemDto MapListItem(AiEntityParseLog log) => new()
    {
        Id = log.Id,
        InvocationId = log.InvocationId,
        ScenarioCode = log.ScenarioCode,
        EntityType = log.EntityType,
        UserId = log.UserId,
        ParentBizType = log.ParentBizType,
        ParentBizId = log.ParentBizId,
        Outcome = log.Outcome,
        SavedBizId = log.SavedBizId,
        RawTextLength = log.RawText?.Length ?? 0,
        FromCache = log.FromCache,
        LatencyMs = log.LatencyMs,
        ProviderCode = log.ProviderCode,
        Model = log.Model,
        CreatedAt = log.CreatedAt,
        ConfirmedAt = log.ConfirmedAt,
        SavedAt = log.SavedAt
    };

    private static object? DeserializeJsonObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return json;
        }
    }

    private static string Csv(string? value)
    {
        var s = value ?? string.Empty;
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return '"' + s.Replace("\"", "\"\"") + '"';
        return s;
    }
}
