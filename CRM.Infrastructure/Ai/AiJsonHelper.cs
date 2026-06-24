using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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
        var trimmed = (content ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(trimmed);
        }
        catch
        {
            return null;
        }
    }
}
