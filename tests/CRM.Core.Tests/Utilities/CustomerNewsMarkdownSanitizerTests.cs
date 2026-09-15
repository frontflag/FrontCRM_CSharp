using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomerNewsMarkdownSanitizerTests
{
    [Fact]
    public void Strips_preamble_before_core_heading()
    {
        var raw = "让我继续搜索关于猎芯的新闻。\n\n## 核心摘要\n\n- 本期无重大事项。\n";
        var got = CustomerNewsMarkdownSanitizer.SanitizeForPersist(raw);
        Assert.StartsWith("## 核心摘要", got);
        Assert.DoesNotContain("让我继续搜索", got);
    }

    [Fact]
    public void Rejects_search_loop_without_heading()
    {
        var loop = string.Concat(Enumerable.Repeat(
            "让我继续搜索关于深圳市猎芯科技有限公司在2026年8月15日的新闻动态信息。让我调整搜索策略，搜索更多信息。",
            20));
        Assert.True(CustomerNewsMarkdownSanitizer.LooksLikeSearchLoop(loop));
        Assert.Equal(string.Empty, CustomerNewsMarkdownSanitizer.SanitizeForPersist(loop));
        var display = CustomerNewsMarkdownSanitizer.SanitizeForDisplay(loop);
        Assert.Contains("请重新抓取", display);
        Assert.DoesNotContain("让我继续搜索", display);
    }

    [Fact]
    public void Collapses_consecutive_duplicate_sentences()
    {
        var raw = "## 核心摘要\n\n本期无重大事项。本期无重大事项。本期无重大事项。\n\n## 事实详情\n本期无重大事项。\n";
        var got = CustomerNewsMarkdownSanitizer.SanitizeForPersist(raw);
        Assert.Contains("## 核心摘要", got);
        var count = CountOccurrences(got, "本期无重大事项");
        Assert.True(count <= 6, got);
    }

    static int CountOccurrences(string hay, string needle)
    {
        var n = 0;
        var from = 0;
        while (true)
        {
            var i = hay.IndexOf(needle, from, StringComparison.Ordinal);
            if (i < 0) return n;
            n++;
            from = i + needle.Length;
        }
    }
}
