using System.IO.Compression;
using System.Text;
using CRM.Core.Knowledge;

namespace CRM.Core.Tests.Knowledge;

public class HandbookDocxReaderTests
{
    [Fact]
    public void ReadParagraphs_ReadsBodyAndSkipsEmpty()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
              <w:body>
                <w:p><w:r><w:t>第10章 报价</w:t></w:r></w:p>
                <w:p><w:r><w:t></w:t></w:r></w:p>
                <w:tbl>
                  <w:tr>
                    <w:tc><w:p><w:r><w:t>栏一</w:t></w:r></w:p></w:tc>
                    <w:tc><w:p><w:r><w:t>栏二</w:t></w:r></w:p></w:tc>
                  </w:tr>
                </w:tbl>
              </w:body>
            </w:document>
            """;
        using var docx = BuildDocx(xml);
        var lines = HandbookDocxReader.ReadParagraphs(docx);
        Assert.Equal(new[] { "第10章 报价", "栏一 | 栏二" }, lines);
    }

    private static MemoryStream BuildDocx(string documentXml)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(documentXml);
        }

        stream.Position = 0;
        return stream;
    }
}
