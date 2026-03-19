using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Auth;

/// <summary>
/// 微信扫码登录临时票据表
/// </summary>
[Table("wechat_login_ticket")]
public class WechatLoginTicket
{
    [Key]
    [MaxLength(64)]
    public string Ticket { get; set; } = string.Empty;

    /// <summary>
    /// 二维码图片URL
    /// </summary>
    [MaxLength(500)]
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// 状态：0=待扫码, 1=已扫码, 2=已确认, 3=已过期, 4=已取消, 5=未绑定
    /// </summary>
    public short Status { get; set; } = 0;

    /// <summary>
    /// 扫码用户的OpenId
    /// </summary>
    [MaxLength(100)]
    public string? OpenId { get; set; }

    /// <summary>
    /// 扫码用户的UnionId
    /// </summary>
    [MaxLength(100)]
    public string? UnionId { get; set; }

    /// <summary>
    /// 登录成功的用户ID
    /// </summary>
    [MaxLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpireTime { get; set; }
}
