namespace CRM.API.Models.DTOs;

// ==================== 扫码登录相关 ====================

/// <summary>
/// 获取微信登录二维码请求
/// </summary>
public class WechatQrCodeRequest
{
    /// <summary>
    /// 设备类型：web=网页端, pc=桌面端
    /// </summary>
    public string DeviceType { get; set; } = "web";
}

/// <summary>
/// 获取微信登录二维码响应
/// </summary>
public class WechatQrCodeResponse
{
    /// <summary>
    /// 唯一票据，用于轮询登录状态
    /// </summary>
    public string Ticket { get; set; } = string.Empty;

    /// <summary>
    /// 二维码图片URL（可直接显示）
    /// </summary>
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpireSeconds { get; set; } = 300;
}

/// <summary>
/// 微信登录状态响应
/// </summary>
public class WechatLoginStatusResponse
{
    /// <summary>
    /// 状态：0=待扫码, 1=已扫码, 2=已确认/登录成功, 3=已过期, 4=已取消, 5=未绑定
    /// </summary>
    public short Status { get; set; }

    /// <summary>
    /// 状态描述
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 登录成功时返回的认证信息
    /// </summary>
    public AuthResponse? AuthData { get; set; }
}

// ==================== 微信绑定相关 ====================

/// <summary>
/// 生成微信绑定二维码响应
/// </summary>
public class WechatBindQrResponse
{
    /// <summary>
    /// 绑定请求ID
    /// </summary>
    public string BindId { get; set; } = string.Empty;

    /// <summary>
    /// 二维码图片URL
    /// </summary>
    public string QrCodeUrl { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpireSeconds { get; set; } = 300;
}

/// <summary>
/// 微信绑定状态响应
/// </summary>
public class WechatBindStatusResponse
{
    /// <summary>
    /// 状态：pending/scanned/success/expired
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 状态描述
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 微信昵称（绑定成功后）
    /// </summary>
    public string? Nickname { get; set; }
}

/// <summary>
/// 微信绑定信息
/// </summary>
public class WechatBindInfo
{
    /// <summary>
    /// 是否已绑定
    /// </summary>
    public bool IsBound { get; set; }

    /// <summary>
    /// 微信昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 微信头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 绑定时间
    /// </summary>
    public DateTime? BindTime { get; set; }
}

// ==================== 微信用户信息 ====================

/// <summary>
/// 微信用户信息
/// </summary>
public class WechatUserInfo
{
    /// <summary>
    /// OpenId
    /// </summary>
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    /// UnionId（可能为空）
    /// </summary>
    public string? UnionId { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 性别：0=未知, 1=男, 2=女
    /// </summary>
    public int Sex { get; set; }

    /// <summary>
    /// 国家
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 省份
    /// </summary>
    public string? Province { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    public string? City { get; set; }
}
