namespace CRM.Core.Interfaces;

/// <summary>报表参数（sysparam）读写。本期仅样式版本，打印页不消费。</summary>
public interface IReportParamsService
{
    /// <summary>当前报表样式版本；无行或非法值返回 V1。</summary>
    Task<string> GetStyleVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>保存报表样式版本；非法值抛 <see cref="ArgumentException"/>。</summary>
    Task<string> SetStyleVersionAsync(string styleVersion, CancellationToken cancellationToken = default);
}
