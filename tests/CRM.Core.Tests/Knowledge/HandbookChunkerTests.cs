using CRM.Core.Knowledge;

namespace CRM.Core.Tests.Knowledge;

public class HandbookChunkerTests
{
    [Fact]
    public void Chunk_SkipsToc_BodyStartsAtRepeatedPreface()
    {
        var paragraphs = new[]
        {
            "目 录",
            "前言：使用说明",
            "第1章 行业全景",
            "1.1 产业链地图",
            "前言：使用说明",
            "本段是正文，不是目录。",
            "第1章 行业全景",
            "1.1 产业链地图",
            "正文里的产业链说明。"
        };

        var chunks = HandbookChunker.Chunk(paragraphs);

        Assert.Contains(chunks, c => c.Content.Contains("本段是正文，不是目录。", StringComparison.Ordinal));
        Assert.DoesNotContain(chunks, c => c.Content == "前言：使用说明");
        Assert.Equal("1", chunks[^1].ChapterNo);
        Assert.Equal("1.1", chunks[^1].SectionNo);
        Assert.Contains("正文里的产业链说明。", chunks[^1].Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Chunk_SetsChapterAndSectionNumbers()
    {
        var chunks = HandbookChunker.Chunk(new[]
        {
            "第10章 报价策略与 GP 管理",
            "10.1 GP 公式与底线",
            "【标准】GP = 售价 - 成本。"
        });

        var chunk = Assert.Single(chunks);
        Assert.Equal("10", chunk.ChapterNo);
        Assert.Equal("报价策略与 GP 管理", chunk.ChapterTitle);
        Assert.Equal("10.1", chunk.SectionNo);
        Assert.Equal("GP 公式与底线", chunk.SectionTitle);
        Assert.Equal("第10章 10.1 GP 公式与底线", chunk.Heading);
        Assert.Contains("【标准】", chunk.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Chunk_SplitsLongSection_SecondChunkOverlaps()
    {
        var sentence = new string('甲', 40) + "。";
        var body = string.Concat(Enumerable.Repeat(sentence, 40));
        var chunks = HandbookChunker.Chunk(new[]
        {
            "第2章 电子元器件基础知识",
            "2.5 货品状态七档",
            body
        });

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, c => Assert.True(c.Content.Length <= HandbookChunker.HardMax));
        var firstBody = chunks[0].Content["第2章 2.5 货品状态七档\n".Length..];
        var overlap = firstBody[^HandbookChunker.Overlap..];
        Assert.Contains(overlap, chunks[1].Content, StringComparison.Ordinal);
        Assert.Equal(0, chunks[0].ChunkIndex);
        Assert.Equal(1, chunks[1].ChunkIndex);
    }

    [Fact]
    public void Chunk_MergesShortSectionIntoNext()
    {
        var chunks = HandbookChunker.Chunk(new[]
        {
            "第3章 术语",
            "3.1 短节",
            "只有几个字。",
            "3.2 下一节",
            "这一节足够长，用来承接被合并的短节内容，避免单独成块。"
        });

        Assert.Single(chunks);
        Assert.Equal("3.2", chunks[0].SectionNo);
        Assert.Contains("只有几个字。", chunks[0].Content, StringComparison.Ordinal);
        Assert.Contains("这一节足够长", chunks[0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Chunk_KeepsTrailingShortSection()
    {
        var chunks = HandbookChunker.Chunk(new[]
        {
            "第4章 收尾",
            "4.1 末节",
            "很短。"
        });

        var chunk = Assert.Single(chunks);
        Assert.Equal("4.1", chunk.SectionNo);
        Assert.Contains("很短。", chunk.Content, StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(chunk.ContentSha256));
    }
}
