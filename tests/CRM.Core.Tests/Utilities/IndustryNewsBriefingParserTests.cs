using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class IndustryNewsBriefingParserTests
{
    [Fact]
    public void Parses_Items_And_Markdown()
    {
        const string json = """
            {
              "items": [
                {
                  "category": "关键厂商新闻",
                  "title": "某原厂扩产",
                  "occurred_on": "2026-09-13",
                  "summary": "一句话",
                  "importance": 3,
                  "is_background": false,
                  "unconfirmed": true
                }
              ],
              "markdown": "## 本周要闻 Top 5\n正文"
            }
            """;

        Assert.True(IndustryNewsBriefingParser.TryParse(json, out var parsed));
        Assert.Single(parsed.Items);
        Assert.Equal("关键厂商", parsed.Items[0].Category);
        Assert.Equal("某原厂扩产", parsed.Items[0].Title);
        Assert.Equal("2026-09-13", parsed.Items[0].OccurredOn);
        Assert.Equal(3, parsed.Items[0].Importance);
        Assert.True(parsed.Items[0].Unconfirmed);
        Assert.Contains("本周要闻", parsed.Markdown);
    }

    [Fact]
    public void Caps_At_Five_And_Clamps_Importance()
    {
        var items = string.Join(',', Enumerable.Range(1, 8).Select(i =>
            $"{{\"category\":\"行业动态\",\"title\":\"t{i}\",\"occurred_on\":\"2026-09-13\",\"summary\":\"s\",\"importance\":9}}"));
        var json = $"{{\"items\":[{items}],\"markdown\":\"md\"}}";

        Assert.True(IndustryNewsBriefingParser.TryParse(json, out var parsed));
        Assert.Equal(5, parsed.Items.Count);
        Assert.All(parsed.Items, x => Assert.Equal(3, x.Importance));
    }

    [Fact]
    public void Strips_Fence_And_Rejects_Garbage()
    {
        var fenced = "```json\n{\"items\":[{\"title\":\"A\",\"category\":\"展会信息\",\"occurred_on\":\"2026-09-12\",\"summary\":\"s\",\"importance\":1}],\"markdown\":\"## 本周展望\"}\n```";
        Assert.True(IndustryNewsBriefingParser.TryParse(fenced, out var parsed));
        Assert.Equal("展会信息", parsed.Items[0].Category);

        Assert.False(IndustryNewsBriefingParser.TryParse("not-json", out _));
        Assert.False(IndustryNewsBriefingParser.TryParse("{\"items\":[]}", out _));
    }
}
