using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.IndustryNews;

public sealed class IndustryNewsService : IIndustryNewsService
{
    readonly ApplicationDbContext _db;
    readonly IAiOrchestrator _orchestrator;
    readonly ILogger<IndustryNewsService> _logger;

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public IndustryNewsService(
        ApplicationDbContext db,
        IAiOrchestrator orchestrator,
        ILogger<IndustryNewsService> logger)
    {
        _db = db;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<IndustryNewsLatestDto> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var today = CommissionShanghai.Today();
        var row = await _db.IndustryNewsBriefings.AsNoTracking()
            .Where(x => x.Status == IndustryNewsCodes.StatusSuccess)
            .OrderByDescending(x => x.BriefingDate)
            .ThenByDescending(x => x.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
            return new IndustryNewsLatestDto();

        return ToLatest(row, today);
    }

    public async Task<IndustryNewsRunResultDto> RunForTodayAsync(
        bool force,
        CancellationToken cancellationToken = default)
    {
        var today = CommissionShanghai.Today();
        var existing = await _db.IndustryNewsBriefings
            .FirstOrDefaultAsync(x => x.BriefingDate == today, cancellationToken);
        var hadSuccess = existing is { Status: IndustryNewsCodes.StatusSuccess };

        if (!force && hadSuccess && existing != null)
        {
            return new IndustryNewsRunResultDto
            {
                Ran = false,
                Success = true,
                Message = "今日简报已生成",
                Latest = ToLatest(existing, today)
            };
        }

        var (start, end) = IndustryNewsWindow.ForBriefingDate(today);
        try
        {
            var invoke = await _orchestrator.InvokeSystemAsync(
                new AiInvokeRequestDto
                {
                    ScenarioCode = AiScenarioCodes.IndustryNewsBriefing,
                    TriggerType = AiInvocationTriggerType.Auto,
                    ForceRefresh = true,
                    Input = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["start_date"] = start.ToString("yyyy-MM-dd"),
                        ["end_date"] = end.ToString("yyyy-MM-dd")
                    }
                },
                IndustryNewsCodes.JobActorId,
                cancellationToken);

            if (!IndustryNewsBriefingParser.TryParse(invoke.Content, out var parsed))
                throw new InvalidOperationException("模型返回无法解析为行业新闻 JSON。");

            var now = DateTime.UtcNow;
            existing ??= new IndustryNewsBriefing
            {
                Id = Guid.NewGuid().ToString("D"),
                BriefingDate = today,
                CreateTime = now
            };
            existing.PeriodStart = start;
            existing.PeriodEnd = end;
            existing.ItemsJson = JsonSerializer.Serialize(parsed.Items, JsonOpts);
            existing.Markdown = parsed.Markdown;
            existing.Status = IndustryNewsCodes.StatusSuccess;
            existing.ErrorMessage = null;
            existing.InvocationId = invoke.InvocationId;
            existing.GeneratedAt = now;
            existing.ModifyTime = now;

            if (_db.Entry(existing).State == EntityState.Detached)
                _db.IndustryNewsBriefings.Add(existing);

            await _db.SaveChangesAsync(cancellationToken);
            return new IndustryNewsRunResultDto
            {
                Ran = true,
                Success = true,
                Message = "已生成",
                Latest = ToLatest(existing, today)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Industry news briefing failed briefingDate={Date}", today);
            if (hadSuccess && existing != null)
            {
                return new IndustryNewsRunResultDto
                {
                    Ran = true,
                    Success = false,
                    Message = ex.Message,
                    Latest = ToLatest(existing, today)
                };
            }

            var now = DateTime.UtcNow;
            existing ??= new IndustryNewsBriefing
            {
                Id = Guid.NewGuid().ToString("D"),
                BriefingDate = today,
                PeriodStart = start,
                PeriodEnd = end,
                CreateTime = now
            };
            existing.PeriodStart = start;
            existing.PeriodEnd = end;
            existing.Status = IndustryNewsCodes.StatusFailed;
            existing.ErrorMessage = Truncate(ex.Message, 500);
            existing.GeneratedAt = now;
            existing.ModifyTime = now;
            if (_db.Entry(existing).State == EntityState.Detached)
                _db.IndustryNewsBriefings.Add(existing);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx, "Failed to persist industry news failure row");
            }

            return new IndustryNewsRunResultDto
            {
                Ran = true,
                Success = false,
                Message = ex.Message,
                Latest = await GetLatestAsync(cancellationToken)
            };
        }
    }

    static IndustryNewsLatestDto ToLatest(IndustryNewsBriefing row, DateOnly today)
    {
        var items = new List<IndustryNewsItemDto>();
        try
        {
            var parsed = JsonSerializer.Deserialize<List<IndustryNewsParsedItem>>(row.ItemsJson, JsonOpts);
            if (parsed != null)
            {
                foreach (var p in parsed.Take(IndustryNewsBriefingParser.MaxItems))
                {
                    items.Add(new IndustryNewsItemDto
                    {
                        Category = p.Category,
                        Title = p.Title,
                        OccurredOn = p.OccurredOn,
                        Summary = p.Summary,
                        Importance = p.Importance,
                        IsBackground = p.IsBackground,
                        Unconfirmed = p.Unconfirmed
                    });
                }
            }
        }
        catch (JsonException)
        {
            // keep empty items, still return markdown
        }

        return new IndustryNewsLatestDto
        {
            HasBriefing = true,
            IsStale = row.BriefingDate != today,
            BriefingDate = row.BriefingDate,
            PeriodStart = row.PeriodStart,
            PeriodEnd = row.PeriodEnd,
            GeneratedAt = row.GeneratedAt,
            Items = items,
            Markdown = row.Markdown ?? string.Empty
        };
    }

    static string Truncate(string? text, int max)
    {
        var v = text ?? string.Empty;
        return v.Length <= max ? v : v[..max];
    }
}
