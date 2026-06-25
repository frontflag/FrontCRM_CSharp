using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CRM.Infrastructure.Ai;

internal static class AiJsonHelper
{
    public static List<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();
        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            return arr?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList()
                   ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static Dictionary<string, string?> FilterInput(
        IReadOnlyDictionary<string, string?> input,
        IReadOnlyList<string> allowedFields)
    {
        if (allowedFields.Count == 0)
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in allowedFields)
        {
            if (string.IsNullOrWhiteSpace(field))
                continue;
            var key = field.Trim();
            if (input.TryGetValue(key, out var val))
                result[key] = val?.Trim();
            else
            {
                var match = input.FirstOrDefault(kv =>
                    string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key))
                    result[key] = match.Value?.Trim();
            }
        }

        return result;
    }

    public static string CanonicalFingerprintJson(IReadOnlyDictionary<string, string?> filtered, IReadOnlyList<string> keyFields)
    {
        var node = new JsonObject();
        foreach (var field in keyFields.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            filtered.TryGetValue(field, out var val);
            node[field] = val ?? string.Empty;
        }

        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static string ComputeSha256Hex(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string RenderTemplate(string template, IReadOnlyDictionary<string, string?> values)
    {
        var result = template ?? string.Empty;
        foreach (var kv in values)
        {
            var placeholder = "{{" + kv.Key + "}}";
            result = result.Replace(placeholder, kv.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    public static object? TryParseJsonObject(string content)
    {
        foreach (var candidate in EnumerateJsonCandidates(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(candidate);
                if (doc.RootElement.ValueKind is JsonValueKind.Object)
                    return doc.RootElement.Clone();
            }
            catch
            {
                // try next candidate
            }
        }

        return null;
    }

    /// <summary>从 LLM 原始文本中提取可写入 PostgreSQL jsonb 的 JSON 对象字符串。</summary>
    public static string? ExtractJsonObjectText(string? content)
    {
        foreach (var candidate in EnumerateJsonCandidates(content))
        {
            if (TryNormalizeJsonObject(candidate, out var normalized))
                return normalized;
        }

        return null;
    }

    /// <summary>确保字符串为合法 JSON 对象，供 jsonb 列写入；失败时返回 null。</summary>
    public static string? CoerceJsonObjectForJsonb(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        return TryNormalizeJsonObject(json.Trim(), out var normalized) ? normalized : null;
    }

    private static bool TryNormalizeJsonObject(string candidate, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(candidate);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            normalized = doc.RootElement.GetRawText();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateJsonCandidates(string? content)
    {
        var trimmed = (content ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            yield break;

        var fenced = Regex.Match(
            trimmed,
            @"^```(?:json|JSON)?\s*\r?\n([\s\S]*?)\r?\n?```\s*$",
            RegexOptions.Singleline);
        if (fenced.Success)
        {
            var inner = fenced.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(inner))
                yield return inner;
        }

        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineEnd = trimmed.IndexOf('\n');
            if (firstLineEnd >= 0)
            {
                var body = trimmed[(firstLineEnd + 1)..].TrimEnd();
                while (body.EndsWith('`'))
                    body = body[..^1].TrimEnd();
                if (!string.IsNullOrEmpty(body))
                    yield return body;
            }
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            yield return trimmed[start..(end + 1)];

        if (trimmed.StartsWith('{'))
            yield return trimmed;
    }
}
