using System.IO.Compression;
using System.Xml.Linq;

namespace CRM.Core.Knowledge;

public static class HandbookDocxReader
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static IReadOnlyList<string> ReadParagraphs(Stream docx)
    {
        using var zip = new ZipArchive(docx, ZipArchiveMode.Read, leaveOpen: true);
        var entry = zip.GetEntry("word/document.xml")
                    ?? throw new InvalidOperationException("docx 缺少正文。");
        using var reader = entry.Open();
        var doc = XDocument.Load(reader);
        var body = doc.Descendants(W + "body").FirstOrDefault()
                   ?? throw new InvalidOperationException("docx 缺少正文。");

        var lines = new List<string>();
        foreach (var node in body.Elements())
        {
            if (node.Name == W + "p")
            {
                AddParagraph(lines, node);
            }
            else if (node.Name == W + "tbl")
            {
                foreach (var row in node.Elements(W + "tr"))
                {
                    var cells = row.Elements(W + "tc")
                        .Select(cell => string.Concat(cell.Descendants(W + "t").Select(t => t.Value)).Trim())
                        .Where(cell => cell.Length > 0);
                    var text = string.Join(" | ", cells);
                    if (text.Length > 0)
                        lines.Add(text);
                }
            }
        }

        return lines;
    }

    private static void AddParagraph(List<string> lines, XElement paragraph)
    {
        var text = string.Concat(paragraph.Descendants(W + "t").Select(t => t.Value)).Trim();
        if (text.Length > 0)
            lines.Add(text);
    }
}
