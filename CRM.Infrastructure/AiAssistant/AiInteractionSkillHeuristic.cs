using System.Text.RegularExpressions;
using CRM.Core.Constants;

namespace CRM.Infrastructure.AiAssistant;

public static class AiInteractionSkillHeuristic
{
    private static readonly Regex FeedbackPattern = new(
        "反馈|建议|报错|故障|bug|不好用|无法|失败|改进|缺陷|截图",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FeedbackStrongPattern = new(
        "报错|故障|bug|无法|缺陷|截图",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HandbookPattern = new(
        "培训|教材|新人|怎么|如何|什么是|业务|知识点|学习",
        RegexOptions.Compiled);

    private static readonly Regex OpsPattern = new(
        "如何操作|步骤|公式|核销|提成|报关|装箱|拣货|审核|出库申请|新建|备货|收款|开票|报价|销售订单|出库",
        RegexOptions.Compiled);

    public static string Choose(string? text, IReadOnlyCollection<string> allowed)
    {
        if (allowed.Count == 0)
            throw new InvalidOperationException("没有可用技能");
        if (allowed.Count == 1)
            return allowed.First();

        var raw = text ?? "";
        var feedbackHit = FeedbackPattern.IsMatch(raw);
        var feedbackStrong = FeedbackStrongPattern.IsMatch(raw);
        var handbookHit = HandbookPattern.IsMatch(raw);
        var opsHit = OpsPattern.IsMatch(raw);
        var pick = feedbackStrong && feedbackHit
            ? AiAssistantSkills.Feedback
            : opsHit
                ? AiAssistantSkills.Ops
                : Fallback(raw, feedbackHit, handbookHit);

        if (allowed.Any(s => string.Equals(s, pick, StringComparison.OrdinalIgnoreCase)))
            return allowed.First(s => string.Equals(s, pick, StringComparison.OrdinalIgnoreCase));

        var next = pick == AiAssistantSkills.Ops ? Fallback(raw, feedbackHit, handbookHit) : pick;
        return allowed.FirstOrDefault(s => string.Equals(s, next, StringComparison.OrdinalIgnoreCase))
            ?? allowed.First();
    }

    private static string Fallback(string raw, bool feedbackHit, bool handbookHit)
    {
        if (feedbackHit && handbookHit)
            return FeedbackStrongPattern.IsMatch(raw) ? AiAssistantSkills.Feedback : AiAssistantSkills.Handbook;
        if (feedbackHit)
            return AiAssistantSkills.Feedback;
        if (handbookHit)
            return AiAssistantSkills.Handbook;
        return raw.Contains('？') || raw.Contains('?') ? AiAssistantSkills.Handbook : AiAssistantSkills.Feedback;
    }
}
