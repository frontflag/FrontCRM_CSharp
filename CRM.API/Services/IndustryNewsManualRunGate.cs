namespace CRM.API.Services;

/// <summary>同一进程内同时只允许一次手动/排队的行业新闻生成。</summary>
public static class IndustryNewsManualRunGate
{
    static int _running;

    public static bool TryBegin() => Interlocked.CompareExchange(ref _running, 1, 0) == 0;

    public static void End() => Interlocked.Exchange(ref _running, 0);
}
