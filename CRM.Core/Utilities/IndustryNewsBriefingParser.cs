using System.Globalization;
using System.Text.Json;
using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public sealed class IndustryNewsParsedItem
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string OccurredOn { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int Importance { get; set; } = 1;
    public bool IsBackground { get; set; }
    public bool Unconfirmed { get; set; }
}

public sealed class IndustryNewsParsedBriefing
{
    public List<IndustryNewsParsedItem> Items { get; set; } = new();
    public string Markdown { get; set; } = string.Empty;
}

/// <summary>解析行业新闻场景的 JSON（items + markdown）。</summary>
public static class IndustryNewsBriefingParser
{
    public const int MaxItems = 5;

    public static bool TryParse(string? content, out IndustryNewsParsedBriefing briefing)
    {
        briefing = new IndustryNewsParsedBriefing();
        var text = StripFence((content ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            var root = doc.RootElement;
            if (root.TryGetProperty("markdown", out var mdEl) && mdEl.ValueKind == JsonValueKind.String)
                briefing.Markdown = (mdEl.GetString() ?? string.Empty).Trim();

            if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in itemsEl.EnumerateArray())
                {
                    if (briefing.Items.Count >= MaxItems)
                        break;
                    var item = ReadItem(el);
                    if (item == null)
                        continue;
                    briefing.Items.Add(item);
                }
            }

            return briefing.Items.Count > 0 || !string.IsNullOrWhiteSpace(briefing.Markdown);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static IndustryNewsParsedItem? ReadItem(JsonElement el)
    {
        if (el.ValueKind != JsonValueKind.Object)
            return null;

        var title = ReadString(el, "title");
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var importance = 1;
        if (el.TryGetProperty("importance", out var impEl))
        {
            if (impEl.ValueKind == JsonValueKind.Number && impEl.TryGetInt32(out var n))
                importance = n;
            else if (impEl.ValueKind == JsonValueKind.String
                     && int.TryParse(impEl.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
                importance = s;
        }

        return new IndustryNewsParsedItem
        {
            Category = NormalizeCategory(ReadString(el, "category")),
            Title = title.Trim(),
            OccurredOn = ReadString(el, "occurred_on") ?? ReadString(el, "date") ?? string.Empty,
            Summary = ReadString(el, "summary") ?? string.Empty,
            Importance = Math.Clamp(importance, 1, 3),
            IsBackground = ReadBool(el, "is_background"),
            Unconfirmed = ReadBool(el, "unconfirmed")
        };
    }

    static string NormalizeCategory(string? raw)
    {
        var v = (raw ?? string.Empty).Trim();
        foreach (var known in IndustryNewsCodes.Categories)
        {
            if (string.Equals(known, v, StringComparison.Ordinal))
                return known;
            if (v.Contains(known, StringComparison.Ordinal))
                return known;
        }

        return string.IsNullOrEmpty(v) ? "行业动态" : v;
    }

    static string? ReadString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String)
            return null;
        return p.GetString();
    }

    static bool ReadBool(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return false;
        return p.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(p.GetString(), out var b) && b,
            _ => false
        };
    }

    static string StripFence(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
            return text;
        var firstNl = text.IndexOf('\n');
        if (firstNl < 0)
            return text;
        var body = text[(firstNl + 1)..];
        var end = body.LastIndexOf("```", StringComparison.Ordinal);
        return end >= 0 ? body[..end].Trim() : body.Trim();
    }
}
