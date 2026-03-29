namespace CRM.API.Models.DTOs
{
    /// <summary>管理员模拟登录请求（避免 userId 放在 URL 路径上导致部分代理/网关 404）</summary>
    public class ImpersonateRequest
    {
        public string UserId { get; set; } = string.Empty;
    }
}
