namespace CRM.API.Models.DTOs
{
    /// <summary>
    /// 前端展示用系统设置（如日期时间 IANA 时区）
    /// </summary>
    public class DisplaySettingsDto
    {
        /// <summary>
        /// IANA 时区，如 Asia/Shanghai、America/New_York
        /// </summary>
        public string DisplayTimeZoneId { get; set; } = "Asia/Shanghai";
    }
}
