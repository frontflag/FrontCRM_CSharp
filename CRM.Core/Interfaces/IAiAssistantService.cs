namespace CRM.Core.Interfaces;

public interface IAiAssistantService
{
    Task<AiAssistantSessionDto> CreateSessionAsync(CreateAiAssistantSessionRequest request, string userId, CancellationToken cancellationToken = default);

    Task<AiAssistantChatTurnDto> SendMessageAsync(
        string sessionId,
        SendAiAssistantMessageRequest request,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>跳过对话模型，直接落一条待处理用户反馈（会话标记为已提交）。</summary>
    Task<DirectFeedbackSubmitResult> SubmitDirectFeedbackAsync(
        SubmitDirectFeedbackRequest request,
        string userId,
        CancellationToken cancellationToken = default);
}

public interface IUserFeedbackAdminService
{
    Task<UserFeedbackPagedResult> GetAdminListAsync(UserFeedbackAdminQuery query, CancellationToken cancellationToken = default);

    Task<UserFeedbackDetailDto?> GetAdminDetailAsync(string id, bool includeMessages, CancellationToken cancellationToken = default);

    Task<UserFeedbackDetailDto> PatchAdminAsync(string id, PatchUserFeedbackRequest request, string actorUserId, CancellationToken cancellationToken = default);
}

public sealed class CreateAiAssistantSessionRequest
{
    public string? PageUrl { get; set; }
    public string? RouteName { get; set; }
    public string? RouteParamsJson { get; set; }
    public string? RouteQueryJson { get; set; }
    public string? UserAgent { get; set; }
    /// <summary>bug / suggestion，可选 chip。</summary>
    public string? PreferredCategory { get; set; }
}

public sealed class SendAiAssistantMessageRequest
{
    public string? Text { get; set; }
    /// <summary>可选：已上传的文档 Id（BizType=Feedback, BizId=sessionId）。</summary>
    public string? AttachmentDocumentId { get; set; }
    /// <summary>可选：base64 图片（不含 data: 前缀时需配合 mime）。</summary>
    public string? ImageBase64 { get; set; }
    public string? ImageMimeType { get; set; }
    public string? ImageFileName { get; set; }
}

public sealed class AiAssistantSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string WelcomeMessage { get; set; } = string.Empty;
    public string? InferredBizRef { get; set; }
}

public sealed class AiAssistantChatTurnDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssistantMessage { get; set; } = string.Empty;
    public string ConversationAction { get; set; } = string.Empty;
    public string? FeedbackId { get; set; }
    public IReadOnlyList<AiAssistantMessageDto> Messages { get; set; } = Array.Empty<AiAssistantMessageDto>();
}

public sealed class AiAssistantMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? AttachmentDocumentId { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class UserFeedbackAdminQuery
{
    public string? Category { get; set; }
    public bool? NeedsHandling { get; set; }
    public bool? IsHandled { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class UserFeedbackListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? BizRef { get; set; }
    public string SubmitUserId { get; set; } = string.Empty;
    public string? SubmitUserName { get; set; }
    public bool NeedsHandling { get; set; }
    public bool IsHandled { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime CreateTime { get; set; }
    public string? PageUrl { get; set; }
    public string? RouteName { get; set; }
}

public class UserFeedbackDetailDto : UserFeedbackListItemDto
{
    public string SessionId { get; set; } = string.Empty;
    public string? ReproSteps { get; set; }
    public string? HandleRemark { get; set; }
    public string? RouteParamsJson { get; set; }
    public string? RouteQueryJson { get; set; }
    public IReadOnlyList<AiAssistantMessageDto>? Messages { get; set; }
    public IReadOnlyList<string>? AttachmentDocumentIds { get; set; }
}

public sealed class PatchUserFeedbackRequest
{
    public bool? NeedsHandling { get; set; }
    public bool? IsHandled { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? HandleRemark { get; set; }
    public string? Category { get; set; }
}

public sealed class UserFeedbackPagedResult
{
    public IReadOnlyList<UserFeedbackListItemDto> Items { get; set; } = Array.Empty<UserFeedbackListItemDto>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class SubmitDirectFeedbackRequest
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? PageUrl { get; set; }
    public string? RouteName { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class DirectFeedbackSubmitResult
{
    public string FeedbackId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
