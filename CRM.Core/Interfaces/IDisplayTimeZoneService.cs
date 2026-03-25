namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 读取「显示用」时区配置（参数表 System.Display.TimeZoneId）
    /// </summary>
    public interface IDisplayTimeZoneService
    {
        /// <summary>
        /// 返回 IANA 时区 ID；无配置或禁用时返回默认 Asia/Shanghai
        /// </summary>
        Task<string> GetDisplayTimeZoneIdAsync(CancellationToken cancellationToken = default);
    }
}
