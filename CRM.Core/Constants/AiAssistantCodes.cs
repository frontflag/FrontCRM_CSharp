namespace CRM.Core.Constants;

public static class AiAssistantScenarioCodes
{
    public const string FeedbackCollect = "assistant.feedback.collect";
}

public static class AiAssistantPermissionCodes
{
    /// <summary>历史权限码；反馈助手现已对任意已登录用户开放，仅作场景/脚本兼容。</summary>
    public const string Submit = "biz.feedback.submit";

    /// <summary>运维查看与处理用户反馈。</summary>
    public const string Admin = "biz.feedback.admin";

    /// <summary>AI 模式里查白名单业务数。查到的数仍受原业务的数据范围和金额权限约束。</summary>
    public const string DataQuery = "biz.ai.data.query";
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
    public const string Handbook = "handbook";
    public const string Ops = "ops";
    public const string Data = "data";
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
