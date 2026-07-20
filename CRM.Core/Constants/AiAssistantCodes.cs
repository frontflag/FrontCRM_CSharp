namespace CRM.Core.Constants;

public static class AiAssistantScenarioCodes
{
    public const string FeedbackCollect = "assistant.feedback.collect";
}

public static class AiAssistantPermissionCodes
{
    /// <summary>登录用户使用反馈助手（也可仅要求 authenticated）。</summary>
    public const string Submit = "biz.feedback.submit";

    /// <summary>运维查看与处理用户反馈。</summary>
    public const string Admin = "biz.feedback.admin";
}

public static class AiAssistantSessionStatus
{
    public const string Open = "open";
    public const string Submitted = "submitted";
    public const string Abandoned = "abandoned";
}

public static class AiAssistantSkills
{
    public const string Feedback = "feedback";
}

public static class AiAssistantMessageRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string System = "system";
}

public static class FeedbackCategories
{
    public const string Bug = "bug";
    public const string Suggestion = "suggestion";
    public const string Other = "other";
}

public static class FeedbackDocumentBizType
{
    public const string Feedback = "Feedback";
}

public static class AiAssistantConversationActions
{
    public const string Ask = "ask";
    public const string Finalize = "finalize";
    public const string Decline = "decline";
    public const string RejectOffTopic = "reject_offtopic";
}
