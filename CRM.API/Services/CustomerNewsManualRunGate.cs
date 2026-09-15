using System.Collections.Concurrent;

namespace CRM.API.Services;

/// <summary>同一客户同时只允许一次新闻动态抓取。</summary>
public static class CustomerNewsManualRunGate
{
    static readonly ConcurrentDictionary<string, byte> Running = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryBegin(string customerId)
    {
        var key = (customerId ?? string.Empty).Trim();
        return !string.IsNullOrEmpty(key) && Running.TryAdd(key, 0);
    }

    public static void End(string customerId)
    {
        var key = (customerId ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(key))
            Running.TryRemove(key, out _);
    }

    public static bool IsRunning(string customerId)
    {
        var key = (customerId ?? string.Empty).Trim();
        return !string.IsNullOrEmpty(key) && Running.ContainsKey(key);
    }
}
