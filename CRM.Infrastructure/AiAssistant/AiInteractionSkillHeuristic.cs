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

    public static string Choose(string? text, IReadOnlyCollection<string> allowed)
    {
        if (allowed.Count == 0)
            throw new InvalidOperationException("没有可用技能");
        if (allowed.Count == 1)
            return allowed.First();

        var raw = text ?? "";
        var feedbackHit = FeedbackPattern.IsMatch(raw);
        var handbookHit = HandbookPattern.IsMatch(raw);
        string pick;
        if (feedbackHit && handbookHit)
            pick = FeedbackStrongPattern.IsMatch(raw) ? AiAssistantSkills.Feedback : AiAssistantSkills.Handbook;
        else if (feedbackHit)
            pick = AiAssistantSkills.Feedback;
        else if (handbookHit)
            pick = AiAssistantSkills.Handbook;
        else
            pick = raw.Contains('？') || raw.Contains('?') ? AiAssistantSkills.Handbook : AiAssistantSkills.Feedback;

        return allowed.FirstOrDefault(s => string.Equals(s, pick, StringComparison.OrdinalIgnoreCase))
            ?? allowed.First();
    }
}
