using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Auth;

/// <summary>
/// 微信绑定请求表（用户自助绑定）
/// </summary>
[Table("wechat_bind_request")]
public class WechatBindRequest
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 要绑定的用户ID
    /// </summary>
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 状态：pending/scanned/success/expired
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 绑定用户的OpenId（成功后填充）
    /// </summary>
    [MaxLength(100)]
    public string? OpenId { get; set; }

    /// <summary>
    /// 绑定用户的UnionId（成功后填充）
    /// </summary>
    [MaxLength(100)]
    public string? UnionId { get; set; }

    /// <summary>
    /// 微信昵称
    /// </summary>
    [MaxLength(100)]
    public string? Nickname { get; set; }

    /// <summary>
    /// 微信头像URL
    /// </summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpireTime { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompleteTime { get; set; }
}
