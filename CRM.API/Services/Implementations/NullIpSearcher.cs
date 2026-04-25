using System.Net;
using IP2Region.Net.Abstractions;

namespace CRM.API.Services.Implementations;

/// <summary>在缺少 xdb 文件时注册，避免登录流程因地理库失败而中断。</summary>
public sealed class NullIpSearcher : ISearcher
{
    public int IoCount => 0;

    public void Dispose()
    {
    }

    public string? Search(string ipStr) => null;

    public string? Search(IPAddress ipAddress) => null;

    [Obsolete("Deprecated by IP2Region.Net")]
    public string? Search(uint ipAddress) => null;
}
