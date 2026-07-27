using Microsoft.Extensions.Primitives;

namespace CRM.API.Utilities;

/// <summary>
/// 从 Query 解析 short 多选：支持 <c>?x=1&amp;x=2</c> 与 <c>?x=1,2</c>。
/// 避免模型绑定对重复键偶发只取末值导致无法 OR。
/// </summary>
public static class QueryShortListParser
{
    public static List<short>? Parse(StringValues values)
    {
        if (values.Count == 0) return null;
        var raw = new List<short>();
        foreach (var s in values)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (short.TryParse(part, out var v))
                    raw.Add(v);
            }
        }
        return raw.Count > 0 ? raw : null;
    }
}
