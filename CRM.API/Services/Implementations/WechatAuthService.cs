using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CRM.API.Models.DTOs;
using CRM.API.Services.Interfaces;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Auth;

namespace CRM.API.Services.Implementations;

/// <summary>
/// 微信认证服务实现
/// </summary>
public class WechatAuthService : IWechatAuthService
{
    private readonly IWechatLoginTicketRepository _ticketRepository;
    private readonly IWechatBindRequestRepository _bindRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRbacService _rbacService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WechatAuthService> _logger;

    // 微信配置
    private string AppId => _configuration["Wechat:AppId"] ?? "";
    private string AppSecret => _configuration["Wechat:AppSecret"] ?? "";

    public WechatAuthService(
        IWechatLoginTicketRepository ticketRepository,
        IWechatBindRequestRepository bindRequestRepository,
        IUserRepository userRepository,
        IRbacService rbacService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WechatAuthService> logger)
    {
        _ticketRepository = ticketRepository;
        _bindRequestRepository = bindRequestRepository;
        _userRepository = userRepository;
        _rbacService = rbacService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    #region 扫码登录

    /// <summary>
    /// 生成登录二维码
    /// </summary>
    public async Task<ApiResponse<WechatQrCodeResponse>> GenerateQrCodeAsync(string deviceType)
    {
        try
        {
            var ticket = Guid.NewGuid().ToString("N");
            var expireSeconds = 300; // 5分钟

            // 调用微信API生成带参数二维码
            var qrCodeUrl = await GenerateWechatQrCodeAsync(ticket, expireSeconds);

            // 保存票据
            var ticketEntity = new WechatLoginTicket
            {
                Ticket = ticket,
                QrCodeUrl = qrCodeUrl,
                Status = 0,
                ExpireTime = DateTime.UtcNow.AddSeconds(expireSeconds)
            };
            await _ticketRepository.AddAsync(ticketEntity);

            _logger.LogInformation($"生成登录二维码: Ticket={ticket}");

            return ApiResponse<WechatQrCodeResponse>.Ok(new WechatQrCodeResponse
            {
                Ticket = ticket,
                QrCodeUrl = qrCodeUrl,
                ExpireSeconds = expireSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成登录二维码失败");
            return ApiResponse<WechatQrCodeResponse>.Fail("生成二维码失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 获取登录状态
    /// </summary>
    public async Task<ApiResponse<WechatLoginStatusResponse>> GetLoginStatusAsync(string ticket)
    {
        try
        {
            var ticketEntity = await _ticketRepository.GetByTicketAsync(ticket);
            if (ticketEntity == null)
            {
                return ApiResponse<WechatLoginStatusResponse>.Fail("票据不存在");
            }

            // 检查过期
            if (ticketEntity.ExpireTime < DateTime.UtcNow && ticketEntity.Status == 0)
            {
                ticketEntity.Status = 3;
                await _ticketRepository.UpdateAsync(ticketEntity);
            }

            var response = new WechatLoginStatusResponse
            {
                Status = ticketEntity.Status,
                Message = GetLoginStatusMessage(ticketEntity.Status)
            };

            // 登录成功，返回认证信息
            if (ticketEntity.Status == 2 && !string.IsNullOrEmpty(ticketEntity.UserId))
            {
                response.AuthData = await GenerateAuthResponseAsync(ticketEntity.UserId);
            }

            return ApiResponse<WechatLoginStatusResponse>.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取登录状态失败");
            return ApiResponse<WechatLoginStatusResponse>.Fail("获取状态失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 处理微信登录回调
    /// </summary>
    public async Task<ApiResponse<bool>> HandleWechatCallbackAsync(string code, string state)
    {
        try
        {
            // 1. 用code换取OpenId
            var wechatUserInfo = await GetWechatUserInfoByCodeAsync(code);
            if (wechatUserInfo == null)
            {
                return ApiResponse<bool>.Fail("获取微信用户信息失败");
            }

            // 2. 获取票据
            var ticket = await _ticketRepository.GetByTicketAsync(state);
            if (ticket == null)
            {
                return ApiResponse<bool>.Fail("票据不存在");
            }

            if (ticket.ExpireTime < DateTime.UtcNow)
            {
                ticket.Status = 3;
                await _ticketRepository.UpdateAsync(ticket);
                return ApiResponse<bool>.Fail("二维码已过期");
            }

            // 3. 【核心】查找已绑定该微信的用户
            var user = await _userRepository
                .FirstOrDefaultAsync(u => u.WechatOpenId == wechatUserInfo.OpenId && u.IsActive);

            if (user == null)
            {
                // 未绑定用户
                _logger.LogInformation($"未绑定用户扫码: OpenId={wechatUserInfo.OpenId}");

                ticket.Status = 5; // 5=未绑定
                ticket.OpenId = wechatUserInfo.OpenId;
                await _ticketRepository.UpdateAsync(ticket);

                return ApiResponse<bool>.Ok(true); // 返回成功，让前端轮询到状态5
            }

            // 4. 已绑定用户，允许登录
            ticket.Status = 2; // 已确认
            ticket.UserId = user.Id;
            ticket.OpenId = wechatUserInfo.OpenId;
            ticket.UnionId = wechatUserInfo.UnionId;
            await _ticketRepository.UpdateAsync(ticket);

            _logger.LogInformation($"微信登录成功: User={user.UserName}");
            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理微信回调失败");
            return ApiResponse<bool>.Fail("处理失败: " + ex.Message);
        }
    }

    #endregion

    #region 微信绑定

    /// <summary>
    /// 生成绑定二维码
    /// </summary>
    public async Task<ApiResponse<WechatBindQrResponse>> GenerateBindQrCodeAsync(string userId)
    {
        try
        {
            // 检查用户是否已绑定
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<WechatBindQrResponse>.Fail("用户不存在");
            }

            if (!string.IsNullOrEmpty(user.WechatOpenId))
            {
                return ApiResponse<WechatBindQrResponse>.Fail("您已绑定微信");
            }

            // 清理该用户的待处理请求
            var existingRequest = await _bindRequestRepository.GetPendingByUserIdAsync(userId);
            if (existingRequest != null)
            {
                existingRequest.Status = "expired";
                await _bindRequestRepository.UpdateAsync(existingRequest);
            }

            // 创建新的绑定请求
            var bindId = Guid.NewGuid().ToString("N");
            var expireSeconds = 300;

            var bindRequest = new WechatBindRequest
            {
                Id = bindId,
                UserId = userId,
                Status = "pending",
                ExpireTime = DateTime.UtcNow.AddSeconds(expireSeconds)
            };
            await _bindRequestRepository.AddAsync(bindRequest);

            // 生成二维码URL（使用微信OAuth授权链接）
            var callbackUrl = Uri.EscapeDataString($"{_configuration["Wechat:CallbackDomain"]}/api/v1/auth/wechat/bind-callback");
            var scope = "snsapi_userinfo"; // 需要用户信息
            var qrCodeUrl = $"https://open.weixin.qq.com/connect/qrconnect?appid={AppId}&redirect_uri={callbackUrl}&response_type=code&scope={scope}&state={bindId}#wechat_redirect";

            _logger.LogInformation($"生成绑定二维码: UserId={userId}, BindId={bindId}");

            return ApiResponse<WechatBindQrResponse>.Ok(new WechatBindQrResponse
            {
                BindId = bindId,
                QrCodeUrl = qrCodeUrl,
                ExpireSeconds = expireSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成绑定二维码失败");
            return ApiResponse<WechatBindQrResponse>.Fail("生成失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 获取绑定状态
    /// </summary>
    public async Task<ApiResponse<WechatBindStatusResponse>> GetBindStatusAsync(string bindId)
    {
        try
        {
            var bindRequest = await _bindRequestRepository.GetByIdAsync(bindId);
            if (bindRequest == null)
            {
                return ApiResponse<WechatBindStatusResponse>.Fail("绑定请求不存在");
            }

            // 检查过期
            if (bindRequest.ExpireTime < DateTime.UtcNow && bindRequest.Status == "pending")
            {
                bindRequest.Status = "expired";
                await _bindRequestRepository.UpdateAsync(bindRequest);
            }

            return ApiResponse<WechatBindStatusResponse>.Ok(new WechatBindStatusResponse
            {
                Status = bindRequest.Status,
                Message = GetBindStatusMessage(bindRequest.Status),
                Nickname = bindRequest.Nickname
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取绑定状态失败");
            return ApiResponse<WechatBindStatusResponse>.Fail("获取失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 处理微信绑定回调
    /// </summary>
    public async Task<ApiResponse<bool>> HandleBindCallbackAsync(string code, string bindId)
    {
        try
        {
            // 1. 获取微信用户信息
            var wechatUser = await GetWechatUserInfoByCodeAsync(code);
            if (wechatUser == null)
            {
                return ApiResponse<bool>.Fail("获取微信用户信息失败");
            }

            // 2. 验证绑定请求
            var bindRequest = await _bindRequestRepository.GetByIdAsync(bindId);
            if (bindRequest == null)
            {
                return ApiResponse<bool>.Fail("绑定请求不存在");
            }

            if (bindRequest.ExpireTime < DateTime.UtcNow)
            {
                bindRequest.Status = "expired";
                await _bindRequestRepository.UpdateAsync(bindRequest);
                return ApiResponse<bool>.Fail("绑定链接已过期");
            }

            if (bindRequest.Status != "pending")
            {
                return ApiResponse<bool>.Fail("该链接已被使用");
            }

            // 3. 检查OpenId是否已被其他账号绑定
            var existingUser = await _userRepository
                .FirstOrDefaultAsync(u => u.WechatOpenId == wechatUser.OpenId);
            if (existingUser != null)
            {
                return ApiResponse<bool>.Fail($"该微信已绑定账号：{existingUser.UserName}");
            }

            // 4. 执行绑定
            var user = await _userRepository.GetByIdAsync(bindRequest.UserId);
            user.WechatOpenId = wechatUser.OpenId;
            user.WechatUnionId = wechatUser.UnionId;
            user.WechatNickname = wechatUser.Nickname;
            user.WechatAvatarUrl = wechatUser.AvatarUrl;
            await _userRepository.UpdateAsync(user);

            // 5. 更新绑定请求
            bindRequest.Status = "success";
            bindRequest.OpenId = wechatUser.OpenId;
            bindRequest.UnionId = wechatUser.UnionId;
            bindRequest.Nickname = wechatUser.Nickname;
            bindRequest.AvatarUrl = wechatUser.AvatarUrl;
            bindRequest.CompleteTime = DateTime.UtcNow;
            await _bindRequestRepository.UpdateAsync(bindRequest);

            _logger.LogInformation($"微信绑定成功: User={user.UserName}, Nickname={wechatUser.Nickname}");
            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理绑定回调失败");
            return ApiResponse<bool>.Fail("绑定失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 解除微信绑定
    /// </summary>
    public async Task<ApiResponse<bool>> UnbindWechatAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Fail("用户不存在");
            }

            if (string.IsNullOrEmpty(user.WechatOpenId))
            {
                return ApiResponse<bool>.Fail("您未绑定微信");
            }

            var oldNickname = user.WechatNickname;

            user.WechatOpenId = null;
            user.WechatUnionId = null;
            user.WechatNickname = null;
            user.WechatAvatarUrl = null;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation($"解除微信绑定: User={user.UserName}, 原昵称={oldNickname}");
            return ApiResponse<bool>.Ok(true, "已解除微信绑定");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除微信绑定失败");
            return ApiResponse<bool>.Fail("解除失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 获取绑定信息
    /// </summary>
    public async Task<ApiResponse<WechatBindInfo>> GetBindInfoAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<WechatBindInfo>.Fail("用户不存在");
            }

            var isBound = !string.IsNullOrEmpty(user.WechatOpenId);

            return ApiResponse<WechatBindInfo>.Ok(new WechatBindInfo
            {
                IsBound = isBound,
                Nickname = user.WechatNickname,
                AvatarUrl = user.WechatAvatarUrl,
                BindTime = isBound ? user.UpdateTime : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取绑定信息失败");
            return ApiResponse<WechatBindInfo>.Fail("获取失败: " + ex.Message);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 生成微信带参数二维码
    /// </summary>
    private async Task<string> GenerateWechatQrCodeAsync(string sceneStr, int expireSeconds)
    {
        var httpClient = _httpClientFactory.CreateClient();

        // 1. 获取access_token
        var tokenUrl = $"https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={AppId}&secret={AppSecret}";
        var tokenResponse = await httpClient.GetStringAsync(tokenUrl);
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponse);
        var accessToken = tokenData.GetProperty("access_token").GetString();

        // 2. 创建二维码ticket
        var qrUrl = $"https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token={accessToken}";
        var qrBody = new
        {
            expire_seconds = expireSeconds,
            action_name = "QR_STR_SCENE",
            action_info = new
            {
                scene = new { scene_str = sceneStr }
            }
        };

        var qrJson = JsonSerializer.Serialize(qrBody);
        var qrContent = new StringContent(qrJson, Encoding.UTF8, "application/json");
        var qrResponse = await httpClient.PostAsync(qrUrl, qrContent);
        var qrResult = await qrResponse.Content.ReadAsStringAsync();
        var qrData = JsonSerializer.Deserialize<JsonElement>(qrResult);

        var ticket = qrData.GetProperty("ticket").GetString();
        return $"https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket={Uri.EscapeDataString(ticket!)}";
    }

    /// <summary>
    /// 用code换取微信用户信息
    /// </summary>
    private async Task<WechatUserInfo?> GetWechatUserInfoByCodeAsync(string code)
    {
        var httpClient = _httpClientFactory.CreateClient();

        // 1. 用code换取access_token和openid
        var tokenUrl = $"https://api.weixin.qq.com/sns/oauth2/access_token?appid={AppId}&secret={AppSecret}&code={code}&grant_type=authorization_code";
        var tokenResponse = await httpClient.GetStringAsync(tokenUrl);
        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponse);

        if (tokenData.TryGetProperty("errcode", out _))
        {
            _logger.LogError($"获取access_token失败: {tokenResponse}");
            return null;
        }

        var accessToken = tokenData.GetProperty("access_token").GetString();
        var openId = tokenData.GetProperty("openid").GetString();
        var unionId = tokenData.TryGetProperty("unionid", out var unionIdElement) ? unionIdElement.GetString() : null;

        // 2. 获取用户信息
        var userInfoUrl = $"https://api.weixin.qq.com/sns/userinfo?access_token={accessToken}&openid={openId}&lang=zh_CN";
        var userInfoResponse = await httpClient.GetStringAsync(userInfoUrl);
        var userInfoData = JsonSerializer.Deserialize<JsonElement>(userInfoResponse);

        if (userInfoData.TryGetProperty("errcode", out _))
        {
            _logger.LogError($"获取用户信息失败: {userInfoResponse}");
            return null;
        }

        return new WechatUserInfo
        {
            OpenId = openId!,
            UnionId = unionId,
            Nickname = userInfoData.GetProperty("nickname").GetString(),
            AvatarUrl = userInfoData.GetProperty("headimgurl").GetString(),
            Sex = userInfoData.GetProperty("sex").GetInt32(),
            Country = userInfoData.GetProperty("country").GetString(),
            Province = userInfoData.GetProperty("province").GetString(),
            City = userInfoData.GetProperty("city").GetString()
        };
    }

    /// <summary>
    /// 生成认证响应
    /// </summary>
    private async Task<AuthResponse> GenerateAuthResponseAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId);

        // 生成JWT Token
        var token = GenerateJwtToken(user.Email ?? user.UserName, user.UserName, userId);

        return new AuthResponse
        {
            Token = token,
            UserName = user.UserName,
            Email = user.Email ?? "",
            UserId = userId,
            IsSysAdmin = summary.IsSysAdmin,
            RoleCodes = summary.RoleCodes,
            PermissionCodes = summary.PermissionCodes,
            DepartmentIds = summary.DepartmentIds
        };
    }

    /// <summary>
    /// 生成JWT Token
    /// </summary>
    private string GenerateJwtToken(string email, string userName, string userId)
    {
        var secretKey = _configuration["Jwt:SecretKey"]!;
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"]!);

        var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetLoginStatusMessage(short status) => status switch
    {
        0 => "等待扫码",
        1 => "已扫码，等待确认",
        2 => "登录成功",
        3 => "二维码已过期",
        4 => "用户取消",
        5 => "微信未绑定系统账号",
        _ => "未知状态"
    };

    private string GetBindStatusMessage(string status) => status switch
    {
        "pending" => "等待扫码",
        "scanned" => "已扫码，等待确认",
        "success" => "绑定成功",
        "expired" => "二维码已过期",
        _ => "未知状态"
    };

    #endregion
}
