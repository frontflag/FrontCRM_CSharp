using System.Text;
using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Document;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using CRM.Core.Models.AiAssistant;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.AiAssistant;

public sealed class AiAssistantService : IAiAssistantService
{
    private const int MaxUserTurnsBeforeDecline = 6;
    private const int MaxConsecutiveOffTopic = 3;

    private readonly ApplicationDbContext _db;
    private readonly IAiLlmProviderFactory _providerFactory;
    private readonly IDocumentService _documentService;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        ApplicationDbContext db,
        IAiLlmProviderFactory providerFactory,
        IDocumentService documentService,
        ILogger<AiAssistantService> logger)
    {
        _db = db;
        _providerFactory = providerFactory;
        _documentService = documentService;
        _logger = logger;
    }

    public async Task<AiAssistantSessionDto> CreateSessionAsync(
        CreateAiAssistantSessionRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户未登录", nameof(userId));

        var inferred = FeedbackAssistantTurnParser.InferBizRefFromRouteJson(
            request.RouteParamsJson, request.RouteQueryJson);

        var preferred = NormalizeCategory(request.PreferredCategory);
        var session = new AiAssistantSession
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId.Trim(),
            ActiveSkill = AiAssistantSkills.Feedback,
            Status = AiAssistantSessionStatus.Open,
            PreferredCategory = preferred,
            PageUrl = TrimOrNull(request.PageUrl, 500),
            RouteName = TrimOrNull(request.RouteName, 100),
            RouteParamsJson = request.RouteParamsJson,
            RouteQueryJson = request.RouteQueryJson,
            UserAgent = TrimOrNull(request.UserAgent, 500),
            InferredBizRef = inferred,
            CreateTime = DateTime.UtcNow
        };

        var welcome = BuildWelcome(preferred, inferred);
        var welcomeMsg = new AiAssistantMessage
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = session.Id,
            Role = AiAssistantMessageRoles.Assistant,
            Content = welcome,
            CreateTime = DateTime.UtcNow
        };

        _db.AiAssistantSessions.Add(session);
        _db.AiAssistantMessages.Add(welcomeMsg);
        await _db.SaveChangesAsync(cancellationToken);

        return new AiAssistantSessionDto
        {
            SessionId = session.Id,
            Status = session.Status,
            WelcomeMessage = welcome,
            InferredBizRef = inferred
        };
    }

    public async Task<AiAssistantChatTurnDto> SendMessageAsync(
        string sessionId,
        SendAiAssistantMessageRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("用户未登录", nameof(userId));

        var session = await _db.AiAssistantSessions.FirstOrDefaultAsync(
            s => s.Id == sessionId && s.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("会话不存在");

        if (!string.Equals(session.Status, AiAssistantSessionStatus.Open, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("会话已结束，请重新打开助手。");

        var text = (request.Text ?? string.Empty).Trim();
        string? attachmentId = string.IsNullOrWhiteSpace(request.AttachmentDocumentId)
            ? null
            : request.AttachmentDocumentId.Trim();

        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            attachmentId = await UploadImageAsync(
                session.Id, userId, request.ImageBase64!, request.ImageMimeType, request.ImageFileName, cancellationToken);
        }

        if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(attachmentId))
            throw new ArgumentException("请输入文字或粘贴图片");

        session.UserTurnCount += 1;
        var userMsg = new AiAssistantMessage
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = session.Id,
            Role = AiAssistantMessageRoles.User,
            Content = string.IsNullOrEmpty(text) ? "[图片附件]" : text,
            AttachmentDocumentId = attachmentId,
            CreateTime = DateTime.UtcNow
        };
        _db.AiAssistantMessages.Add(userMsg);

        var history = await _db.AiAssistantMessages.AsNoTracking()
            .Where(m => m.SessionId == session.Id)
            .OrderBy(m => m.CreateTime)
            .ToListAsync(cancellationToken);

        var llmResult = await InvokeModelOrHeuristicAsync(session, history, text, attachmentId != null, cancellationToken);
        ApplyGuards(session, llmResult);

        string? feedbackId = null;
        if (string.Equals(llmResult.ConversationAction, AiAssistantConversationActions.Finalize, StringComparison.OrdinalIgnoreCase))
        {
            feedbackId = await FinalizeFeedbackAsync(session, llmResult, cancellationToken);
            session.Status = AiAssistantSessionStatus.Submitted;
            session.ConsecutiveOffTopicCount = 0;
            llmResult.AssistantMessage = "已记录并通知开发团队，感谢反馈。";
        }
        else if (string.Equals(llmResult.ConversationAction, AiAssistantConversationActions.Decline, StringComparison.OrdinalIgnoreCase))
        {
            session.Status = AiAssistantSessionStatus.Abandoned;
        }
        else if (string.Equals(llmResult.ConversationAction, AiAssistantConversationActions.RejectOffTopic, StringComparison.OrdinalIgnoreCase))
        {
            session.ConsecutiveOffTopicCount += 1;
            if (session.ConsecutiveOffTopicCount >= MaxConsecutiveOffTopic)
            {
                session.Status = AiAssistantSessionStatus.Abandoned;
                llmResult.AssistantMessage =
                    "看起来这次不是系统问题反馈。我先结束本轮对话；若您遇到故障或有改进建议，可重新打开助手再告诉我。";
            }
        }
        else
        {
            session.ConsecutiveOffTopicCount = 0;
        }

        var assistantMsg = new AiAssistantMessage
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = session.Id,
            Role = AiAssistantMessageRoles.Assistant,
            Content = llmResult.AssistantMessage,
            CreateTime = DateTime.UtcNow
        };
        _db.AiAssistantMessages.Add(assistantMsg);
        session.ModifyTime = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var messages = await _db.AiAssistantMessages.AsNoTracking()
            .Where(m => m.SessionId == session.Id)
            .OrderBy(m => m.CreateTime)
            .Select(m => new AiAssistantMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                AttachmentDocumentId = m.AttachmentDocumentId,
                CreateTime = m.CreateTime
            })
            .ToListAsync(cancellationToken);

        return new AiAssistantChatTurnDto
        {
            SessionId = session.Id,
            Status = session.Status,
            AssistantMessage = llmResult.AssistantMessage,
            ConversationAction = llmResult.ConversationAction,
            FeedbackId = feedbackId,
            Messages = messages
        };
    }

    private void ApplyGuards(AiAssistantSession session, FeedbackAssistantLlmResult result)
    {
        if (string.Equals(result.Intent, "off_topic", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(result.ConversationAction, AiAssistantConversationActions.RejectOffTopic, StringComparison.OrdinalIgnoreCase))
        {
            result.ConversationAction = AiAssistantConversationActions.RejectOffTopic;
        }

        if (string.Equals(result.ConversationAction, AiAssistantConversationActions.Finalize, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(result.Summary) || string.IsNullOrWhiteSpace(result.Category))
            {
                result.ConversationAction = AiAssistantConversationActions.Ask;
                result.AssistantMessage = string.IsNullOrWhiteSpace(result.Category)
                    ? "请问这是系统故障，还是功能改进建议？"
                    : "请再补充一下具体现象或期望，方便我们记录。";
            }
            else if (string.Equals(result.Category, FeedbackCategories.Bug, StringComparison.OrdinalIgnoreCase)
                     && string.IsNullOrWhiteSpace(result.BizRef)
                     && string.IsNullOrWhiteSpace(session.InferredBizRef)
                     && !(result.Summary?.Contains("无单号", StringComparison.Ordinal) ?? false))
            {
                // 仅缺陷类：无单号且摘要未注明时继续追问
                result.ConversationAction = AiAssistantConversationActions.Ask;
                result.AssistantMessage = "方便的话，相关单据号是多少？若没有可直接回复「没有单号」。";
            }
        }

        if (session.UserTurnCount >= MaxUserTurnsBeforeDecline
            && string.Equals(result.ConversationAction, AiAssistantConversationActions.Ask, StringComparison.OrdinalIgnoreCase))
        {
            result.ConversationAction = AiAssistantConversationActions.Decline;
            result.AssistantMessage = "目前信息还不太完整，我先不创建正式记录了。您之后想清楚或方便补充时，随时再找我。感谢理解。";
        }
    }

    private async Task<FeedbackAssistantLlmResult> InvokeModelOrHeuristicAsync(
        AiAssistantSession session,
        List<AiAssistantMessage> historyIncludingNewUser,
        string userText,
        bool hasImage,
        CancellationToken cancellationToken)
    {
        var heuristic = FeedbackAssistantTurnParser.Heuristic(
            userText,
            session.PreferredCategory,
            session.InferredBizRef,
            null,
            hasImage,
            session.UserTurnCount,
            session.ConsecutiveOffTopicCount);

        try
        {
            var scenario = await _db.AiScenarios.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == AiAssistantScenarioCodes.FeedbackCollect && !s.IsDeleted && s.IsEnabled, cancellationToken);
            if (scenario == null)
                return heuristic;

            var template = await _db.AiPromptTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == scenario.PromptTemplateId && !t.IsDeleted, cancellationToken);
            var provider = await _db.AiProviders.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == scenario.ProviderCode && !p.IsDeleted && p.IsEnabled, cancellationToken);
            if (template == null || provider == null)
                return heuristic;

            // mock 场景走启发式，便于本地联调 ask→finalize；真实 Provider 才调 LLM
            if (string.Equals(provider.Code, "mock", StringComparison.OrdinalIgnoreCase))
                return heuristic;

            var messages = BuildChatMessages(template.SystemPrompt, session, historyIncludingNewUser);
            var llm = _providerFactory.Create(provider);
            var completion = await llm.ChatAsync(new AiChatCompletionRequest
            {
                ProviderCode = provider.Code,
                Model = string.IsNullOrWhiteSpace(scenario.Model) ? provider.DefaultModel : scenario.Model,
                Messages = messages,
                MaxTokens = scenario.MaxTokens,
                Temperature = scenario.Temperature,
                TimeoutSeconds = provider.TimeoutSeconds,
                EnableWebSearch = false
            }, cancellationToken);

            var parsed = FeedbackAssistantTurnParser.TryParseJson(completion.Content);
            if (parsed != null)
                return parsed;

            _logger.LogWarning("Feedback assistant LLM returned non-JSON; falling back to heuristic. preview={Preview}",
                (completion.Content ?? string.Empty).Length > 200
                    ? completion.Content![..200]
                    : completion.Content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feedback assistant LLM invoke failed; using heuristic");
        }

        return heuristic;
    }

    private static List<AiChatMessageDto> BuildChatMessages(
        string systemPrompt,
        AiAssistantSession session,
        List<AiAssistantMessage> history)
    {
        var ctx = new StringBuilder();
        ctx.AppendLine(systemPrompt);
        ctx.AppendLine();
        ctx.AppendLine("【页面上下文】");
        ctx.AppendLine($"routeName={session.RouteName}");
        ctx.AppendLine($"pageUrl={session.PageUrl}");
        ctx.AppendLine($"inferredBizRef={session.InferredBizRef}");
        ctx.AppendLine($"preferredCategory={session.PreferredCategory}");
        ctx.AppendLine($"userTurnCount={session.UserTurnCount}");
        ctx.AppendLine($"consecutiveOffTopic={session.ConsecutiveOffTopicCount}");

        var list = new List<AiChatMessageDto>
        {
            new() { Role = "system", Content = ctx.ToString() }
        };

        foreach (var m in history)
        {
            if (string.Equals(m.Role, AiAssistantMessageRoles.System, StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add(new AiChatMessageDto
            {
                Role = string.Equals(m.Role, AiAssistantMessageRoles.Assistant, StringComparison.OrdinalIgnoreCase)
                    ? "assistant"
                    : "user",
                Content = m.Content ?? ""
            });
        }

        return list;
    }

    private async Task<string> FinalizeFeedbackAsync(
        AiAssistantSession session,
        FeedbackAssistantLlmResult result,
        CancellationToken cancellationToken)
    {
        var category = NormalizeCategory(result.Category) ?? FeedbackCategories.Other;
        var title = string.IsNullOrWhiteSpace(result.Title)
            ? (result.Summary?.Length > 40 ? result.Summary[..40] + "…" : result.Summary) ?? "用户反馈"
            : result.Title!.Trim();
        var summary = (result.Summary ?? string.Empty).Trim();
        if (summary.Length == 0)
            summary = title;

        var bizRef = FirstNonEmpty(result.BizRef, session.InferredBizRef);
        if (summary.Contains("无单号", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(result.BizRef))
            bizRef = session.InferredBizRef; // 用户明确无单号时仅保留路由推断值

        var feedback = new UserFeedback
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = session.Id,
            Category = category,
            Title = title.Length > 200 ? title[..200] : title,
            Summary = summary,
            BizRef = TrimOrNull(bizRef, 200),
            ReproSteps = result.ReproSteps,
            PageUrl = session.PageUrl,
            RouteName = session.RouteName,
            RouteParamsJson = session.RouteParamsJson,
            RouteQueryJson = session.RouteQueryJson,
            SubmitUserId = session.UserId,
            NeedsHandling = true,
            IsHandled = false,
            CreateTime = DateTime.UtcNow
        };
        _db.UserFeedbacks.Add(feedback);

        // 将会话附件改挂到工单（同 BizType，更新 BizId）
        var docs = await _db.UploadDocuments
            .Where(d => d.BizType == FeedbackDocumentBizType.Feedback && d.BizId == session.Id && !d.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var d in docs)
            d.BizId = feedback.Id;

        return feedback.Id;
    }

    private async Task<string> UploadImageAsync(
        string sessionId,
        string userId,
        string imageBase64,
        string? mime,
        string? fileName,
        CancellationToken cancellationToken)
    {
        var raw = imageBase64.Trim();
        var comma = raw.IndexOf(',');
        if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            raw = raw[(comma + 1)..];

        var bytes = Convert.FromBase64String(raw);
        await using var stream = new MemoryStream(bytes);
        var ext = ".png";
        var contentType = string.IsNullOrWhiteSpace(mime) ? "image/png" : mime.Trim();
        if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase)) ext = ".jpg";
        else if (contentType.Contains("webp", StringComparison.OrdinalIgnoreCase)) ext = ".webp";
        else if (contentType.Contains("gif", StringComparison.OrdinalIgnoreCase)) ext = ".gif";

        var name = string.IsNullOrWhiteSpace(fileName) ? $"feedback-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}" : fileName!;
        var uploaded = await _documentService.UploadAsync(new DocumentUploadRequest
        {
            BizType = FeedbackDocumentBizType.Feedback,
            BizId = sessionId,
            UploadUserId = userId,
            Files =
            {
                new DocumentUploadFile
                {
                    Stream = stream,
                    FileName = name,
                    ContentType = contentType
                }
            }
        });
        cancellationToken.ThrowIfCancellationRequested();
        return uploaded[0].Id;
    }

    private static string BuildWelcome(string? preferredCategory, string? inferredBizRef)
    {
        var sb = new StringBuilder();
        sb.Append("您好，我是系统反馈助手，可帮您反馈使用中的问题或提出改进建议（不提供闲聊）。");
        if (!string.IsNullOrWhiteSpace(preferredCategory))
            sb.Append(string.Equals(preferredCategory, FeedbackCategories.Suggestion, StringComparison.OrdinalIgnoreCase)
                ? "当前按「改进建议」收集。"
                : "当前按「问题反馈」收集。");
        if (!string.IsNullOrWhiteSpace(inferredBizRef))
            sb.Append($"已关联当前页面业务标识：{inferredBizRef}。");
        sb.Append("请直接描述情况；信息不够我会继续问您。");
        return sb.ToString();
    }

    private static string? NormalizeCategory(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var c = raw.Trim().ToLowerInvariant();
        return c switch
        {
            FeedbackCategories.Bug or "problem" or "缺陷" or "问题" => FeedbackCategories.Bug,
            FeedbackCategories.Suggestion or "建议" or "改进" => FeedbackCategories.Suggestion,
            FeedbackCategories.Other => FeedbackCategories.Other,
            _ => null
        };
    }

    private static string? TrimOrNull(string? s, int max)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Length <= max ? t : t[..max];
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
