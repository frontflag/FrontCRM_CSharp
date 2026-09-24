using CRM.Core.Knowledge;

namespace CRM.Core.Tests.Knowledge;

public class HandbookSectionTextTests
{
    [Fact]
    public void Merge_DropsOverlapFromTheNextSlice()
    {
        var head = new string('甲', 100);
        var overlap = new string('乙', 30);
        var tail = new string('丙', 40);
        var merged = HandbookSectionText.Merge(new[]
        {
            "第1章 1.1 标题\n" + head + overlap,
            "第1章 1.1 标题\n" + overlap + tail
        });

        Assert.Contains(tail, merged, StringComparison.Ordinal);
        Assert.Equal(1, CountOf(merged, overlap));
    }

    [Fact]
    public void SectionAnchor_StaysStableWithoutSectionNumber()
    {
        var a = HandbookAnchor.Section("3", "第3章 3.2 货品状态", "3.2", "货品状态");
        var b = HandbookAnchor.Section(null, "【案例】客户压价", null, "【案例】客户压价");
        Assert.Equal("c-3-s-3.2", a);
        Assert.StartsWith("c-front-s-t-", b, StringComparison.Ordinal);
        Assert.Equal(b, HandbookAnchor.Section(null, "【案例】客户压价", null, "【案例】客户压价"));
    }

    private static int CountOf(string text, string part)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(part, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += part.Length;
        }

        return count;
    }
}
