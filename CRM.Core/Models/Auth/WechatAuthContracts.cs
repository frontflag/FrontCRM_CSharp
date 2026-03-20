namespace CRM.Core.Models.Auth;

public class WechatApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int ErrorCode { get; set; }

    public static WechatApiResponse<T> Ok(T data, string message = "操作成功")
    {
        return new WechatApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            ErrorCode = 0
        };
    }

    public static WechatApiResponse<T> Fail(string message, int errorCode = 1)
    {
        return new WechatApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

public class WechatQrCodeResponse
{
    public string Ticket { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public int ExpireSeconds { get; set; } = 300;
}

public class WechatLoginAuthData
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsSysAdmin { get; set; }
    public IReadOnlyList<string> RoleCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DepartmentIds { get; set; } = Array.Empty<string>();
}

public class WechatLoginStatusResponse
{
    public short Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public WechatLoginAuthData? AuthData { get; set; }
}

public class WechatBindQrResponse
{
    public string BindId { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public int ExpireSeconds { get; set; } = 300;
}

public class WechatBindStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Nickname { get; set; }
}

public class WechatBindInfo
{
    public bool IsBound { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? BindTime { get; set; }
}

public class WechatUserInfo
{
    public string OpenId { get; set; } = string.Empty;
    public string? UnionId { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public int Sex { get; set; }
    public string? Country { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
}

