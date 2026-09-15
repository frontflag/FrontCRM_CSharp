using System.Text;
using System.Text.RegularExpressions;

namespace CRM.Core.Utilities;

/// <summary>
/// 客户新闻动态 Markdown：去掉检索过程前言，截断「让我继续搜索」式重复。
/// </summary>
public static class CustomerNewsMarkdownSanitizer
{
    public const string CoreHeading = "## 核心摘要";

    public const string EmptyBriefingMarkdown =
        "## 核心摘要\n\n本期无重大事项。\n\n"
        + "## 事实详情\n本期无重大事项。\n\n"
        + "## 风险雷达\n本期无重大事项。\n\n"
        + "## 变化追踪\n本期无重大事项。\n\n"
        + "## 待核实区\n本期无重大事项。\n";

    public const string InvalidFallbackMarkdown =
        "## 核心摘要\n\n本期未能生成有效简报，请重新抓取。\n\n"
        + "## 事实详情\n本期无重大事项。\n\n"
        + "## 风险雷达\n本期无重大事项。\n\n"
        + "## 变化追踪\n本期无重大事项。\n\n"
        + "## 待核实区\n本期无重大事项。\n";

    public const string OutputHardConstraint =
        "【输出硬约束】第一行必须是「## 核心摘要」。禁止输出检索过程，禁止写「让我继续搜索」「让我调整搜索策略」「让我进行一次更全面的搜索」及同义反复。找不到公开信息时各节只写「本期无重大事项」并立即停止。写完「待核实区」后必须停止，禁止重复同一句话。";

    static readonly string[] LoopHints =
    {
        "让我继续搜索",
        "让我调整搜索",
        "让我进行一次更全面",
        "让我再搜索",
        "让我扩大搜索",
        "让我尝试更多",
        "需要搜索更多"
    };

    public static bool LooksLikeSearchLoop(string? text)
    {
        var raw = text ?? string.Empty;
        if (raw.Length < 80)
            return false;
        var hits = 0;
        foreach (var hint in LoopHints)
        {
            var from = 0;
            while (true)
            {
                var i = raw.IndexOf(hint, from, StringComparison.Ordinal);
                if (i < 0) break;
                hits++;
                if (hits >= 3)
                    return true;
                from = i + hint.Length;
            }
        }

        return false;
    }

    /// <summary>落库用。无效则返回空，由调用方改写为短简报。</summary>
    public static string SanitizeForPersist(string? raw)
    {
        var cleaned = Clean(raw);
        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;
        if (!HasCoreHeading(cleaned))
            return string.Empty;
        if (LooksLikeSearchLoop(StripBeforeCore(cleaned)))
            return string.Empty;
        return cleaned;
    }

    /// <summary>已落库脏数据展示时回落为短提示，避免整页重复句。</summary>
    public static string SanitizeForDisplay(string? raw)
    {
        var cleaned = SanitizeForPersist(raw);
        return string.IsNullOrWhiteSpace(cleaned) ? InvalidFallbackMarkdown : cleaned;
    }

    static string Clean(string? raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.Length == 0)
            return string.Empty;

        text = StripBeforeCore(text);
        text = CollapseRepeatedSentences(text);
        return text.Trim();
    }

    static string StripBeforeCore(string text)
    {
        var idx = IndexOfCoreHeading(text);
        return idx >= 0 ? text[idx..].Trim() : text;
    }

    static bool HasCoreHeading(string text) => IndexOfCoreHeading(text) >= 0;

    static int IndexOfCoreHeading(string text)
    {
        var i = text.IndexOf(CoreHeading, StringComparison.Ordinal);
        if (i >= 0)
            return i;
        return text.IndexOf("## 核心摘要", StringComparison.Ordinal);
    }

    static string CollapseRepeatedSentences(string text)
    {
        var parts = Regex.Split(text, @"(?<=[。！？\n])");
        var sb = new StringBuilder(text.Length);
        string? lastNorm = null;
        var lastRun = 0;
        var global = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var part in parts)
        {
            if (part.Length == 0)
                continue;
            var norm = NormalizeSentence(part);
            if (norm.Length >= 8)
            {
                var consecutiveDup = string.Equals(norm, lastNorm, StringComparison.Ordinal);
                if (consecutiveDup)
                {
                    lastRun++;
                    if (lastRun >= 2)
                        continue;
                }
                else
                {
                    lastNorm = norm;
                    lastRun = 1;
                }

                if (norm.Length >= 20)
                {
                    global.TryGetValue(norm, out var n);
                    if (n >= 2)
                        continue;
                    global[norm] = n + 1;
                }
            }
            else
            {
                lastNorm = null;
                lastRun = 0;
            }

            sb.Append(part);
        }

        return sb.ToString();
    }

    static string NormalizeSentence(string part)
    {
        var t = part.Trim();
        if (t.Length == 0)
            return string.Empty;
        t = Regex.Replace(t, @"\s+", "");
        t = t.Trim('。', '！', '？', '，', ',', ';', '；');
        return t;
    }
}
