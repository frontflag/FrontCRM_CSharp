using System.Text.Json;
using System.Text.RegularExpressions;
using CRM.Core.Constants;

namespace CRM.Infrastructure.AiAssistant;

internal sealed class FeedbackAssistantLlmResult
{
    public string AssistantMessage { get; set; } = string.Empty;
    public string Intent { get; set; } = "feedback";
    public string ConversationAction { get; set; } = AiAssistantConversationActions.Ask;
    public string? Category { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? BizRef { get; set; }
    public string? ReproSteps { get; set; }
    public List<string> MissingSlots { get; set; } = new();
}

internal static class FeedbackAssistantTurnParser
{
    private static readonly Regex OffTopicRegex = new(
        @"天气|笑话|讲个故事|随便聊聊|写一篇|写首诗|你是谁|星座|股票行情|帮我做作业",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NoBizRefRegex = new(
        @"没有单号|不知道单号|不清楚单号|无单号|记不得",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SuggestionRegex = new(
        @"建议|希望|能不能加|可以增加|改进|优化一下|体验不好",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BugRegex = new(
        @"报错|错误|失败|没反应|点不了|坏了|异常|打不开|空白|卡住",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static FeedbackAssistantLlmResult? TryParseJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = content.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
                text = text[start..(end + 1)];
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            var result = new FeedbackAssistantLlmResult
            {
                AssistantMessage = root.TryGetProperty("assistantMessage", out var am) ? am.GetString() ?? "" : "",
                Intent = root.TryGetProperty("intent", out var intent) ? intent.GetString() ?? "feedback" : "feedback",
                ConversationAction = root.TryGetProperty("conversationAction", out var act)
                    ? act.GetString() ?? AiAssistantConversationActions.Ask
                    : AiAssistantConversationActions.Ask
            };

            if (root.TryGetProperty("slots", out var slots) && slots.ValueKind == JsonValueKind.Object)
            {
                result.Category = GetString(slots, "category");
                result.Title = GetString(slots, "title");
                result.Summary = GetString(slots, "summary");
                result.BizRef = GetString(slots, "bizRef");
                result.ReproSteps = GetString(slots, "reproSteps");
            }

            if (root.TryGetProperty("missingSlots", out var missing) && missing.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in missing.EnumerateArray())
                {
                    var s = m.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        result.MissingSlots.Add(s!);
                }
            }

            return string.IsNullOrWhiteSpace(result.AssistantMessage) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    public static FeedbackAssistantLlmResult Heuristic(
        string userText,
        string? preferredCategory,
        string? inferredBizRef,
        string? knownBizRefFromSlots,
        bool hasImage,
        int userTurnCount,
        int consecutiveOffTopic)
    {
        var text = (userText ?? string.Empty).Trim();
        if (OffTopicRegex.IsMatch(text) && !BugRegex.IsMatch(text) && !SuggestionRegex.IsMatch(text))
        {
            var next = consecutiveOffTopic + 1;
            if (next >= 3)
            {
                return new FeedbackAssistantLlmResult
                {
                    Intent = "off_topic",
                    ConversationAction = AiAssistantConversationActions.RejectOffTopic,
                    AssistantMessage =
                        "看起来这次不是系统问题反馈。我先结束本轮对话；若您遇到故障或有改进建议，可重新打开助手再告诉我。"
                };
            }

            return new FeedbackAssistantLlmResult
            {
                Intent = "off_topic",
                ConversationAction = AiAssistantConversationActions.RejectOffTopic,
                AssistantMessage =
                    "我是系统反馈助手，只能帮您反馈使用中的问题或改进建议。您可以点「反馈问题」，或简单描述遇到了什么情况。"
            };
        }

        var category = preferredCategory;
        if (string.IsNullOrWhiteSpace(category))
        {
            if (SuggestionRegex.IsMatch(text)) category = FeedbackCategories.Suggestion;
            else if (BugRegex.IsMatch(text)) category = FeedbackCategories.Bug;
        }

        var bizRef = FirstNonEmpty(knownBizRefFromSlots, inferredBizRef);
        var userSaidNoBiz = NoBizRefRegex.IsMatch(text);
        if (string.IsNullOrWhiteSpace(bizRef) && LooksLikeBizCode(text))
            bizRef = text.Length <= 200 ? text : text[..200];

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(category))
            missing.Add("category");
        // 仅「问题/缺陷」强制追问单号；改进建议不要求业务单号
        var requireBizRef = string.Equals(category, FeedbackCategories.Bug, StringComparison.OrdinalIgnoreCase);
        if (requireBizRef
            && string.IsNullOrWhiteSpace(bizRef)
            && !userSaidNoBiz
            && string.IsNullOrWhiteSpace(inferredBizRef))
            missing.Add("bizRef");
        if (text.Length < 8 && !hasImage)
            missing.Add("summary");

        if (userTurnCount >= 6 && missing.Count > 0)
        {
            return new FeedbackAssistantLlmResult
            {
                Intent = "feedback",
                ConversationAction = AiAssistantConversationActions.Decline,
                Category = category,
                AssistantMessage = "目前信息还不太完整，我先不创建正式记录了。您之后想清楚或方便补充时，随时再找我。感谢理解。"
            };
        }

        if (missing.Contains("category"))
        {
            return new FeedbackAssistantLlmResult
            {
                Intent = "feedback",
                ConversationAction = AiAssistantConversationActions.Ask,
                MissingSlots = missing,
                AssistantMessage = "请问这是系统故障（功能异常），还是功能改进建议？"
            };
        }

        if (missing.Contains("bizRef"))
        {
            return new FeedbackAssistantLlmResult
            {
                Intent = "feedback",
                ConversationAction = AiAssistantConversationActions.Ask,
                Category = category,
                MissingSlots = missing,
                AssistantMessage = "方便的话，相关单据号是多少？若没有或不知道，也可以直接告诉我「没有单号」。"
            };
        }

        if (missing.Contains("summary") || (userTurnCount == 1 && text.Length < 15 && !hasImage))
        {
            return new FeedbackAssistantLlmResult
            {
                Intent = "feedback",
                ConversationAction = AiAssistantConversationActions.Ask,
                Category = category,
                BizRef = bizRef,
                MissingSlots = new List<string> { "summary" },
                AssistantMessage = string.Equals(category, FeedbackCategories.Suggestion, StringComparison.OrdinalIgnoreCase)
                    ? "请再补充一下：现在哪里不方便，您希望改成什么样？"
                    : "请再补充一下具体现象，例如点了什么、期望怎样、实际怎样。方便的话也可以贴张截图。"
            };
        }

        var summary = text;
        if (userSaidNoBiz && !summary.Contains("无单号", StringComparison.Ordinal))
            summary += "（用户表示无单号）";

        var title = summary.Length <= 40 ? summary : summary[..40] + "…";
        return new FeedbackAssistantLlmResult
        {
            Intent = "feedback",
            ConversationAction = AiAssistantConversationActions.Finalize,
            Category = category ?? FeedbackCategories.Other,
            Title = title,
            Summary = summary,
            BizRef = userSaidNoBiz ? null : bizRef,
            ReproSteps = string.Equals(category, FeedbackCategories.Bug, StringComparison.OrdinalIgnoreCase)
                ? "详见用户描述"
                : null,
            AssistantMessage = "已记录并通知开发团队，感谢反馈。"
        };
    }

    public static string? InferBizRefFromRouteJson(string? paramsJson, string? queryJson)
    {
        foreach (var json in new[] { paramsJson, queryJson })
        {
            if (string.IsNullOrWhiteSpace(json)) continue;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) continue;
                foreach (var key in new[] { "id", "qcId", "noticeId", "stockInId", "sellOrderId", "purchaseOrderId", "rfqId" })
                {
                    if (doc.RootElement.TryGetProperty(key, out var el))
                    {
                        var v = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
                        if (!string.IsNullOrWhiteSpace(v) && v != "undefined" && v != "null")
                            return v!.Trim();
                    }
                }
            }
            catch
            {
                /* ignore */
            }
        }

        return null;
    }

    private static string? GetString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var el) && el.ValueKind != JsonValueKind.Null
            ? el.GetString()
            : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private static bool LooksLikeBizCode(string text)
    {
        var t = text.Trim();
        if (t.Length is < 6 or > 64) return false;
        if (t.Contains(' ') || t.Contains('？') || t.Contains('?')) return false;
        return Regex.IsMatch(t, @"^[A-Za-z0-9\-_./]+$");
    }
}
