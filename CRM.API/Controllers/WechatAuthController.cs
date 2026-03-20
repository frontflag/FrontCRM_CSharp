using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CRM.Core.Interfaces;
using CRM.Core.Models.Auth;

namespace CRM.API.Controllers;

/// <summary>
/// 微信认证控制器
/// </summary>
[ApiController]
[Route("api/v1/auth/wechat")]
public class WechatAuthController : ControllerBase
{
    private readonly IWechatAuthService _wechatAuthService;
    private readonly ILogger<WechatAuthController> _logger;

    public WechatAuthController(IWechatAuthService wechatAuthService, ILogger<WechatAuthController> logger)
    {
        _wechatAuthService = wechatAuthService;
        _logger = logger;
    }

    #region 扫码登录

    /// <summary>
    /// 获取微信登录二维码
    /// </summary>
    [HttpPost("qrcode")]
    [AllowAnonymous]
    public async Task<ActionResult<WechatApiResponse<WechatQrCodeResponse>>> GetQrCode([FromBody] CRM.API.Models.DTOs.WechatQrCodeRequest request)
    {
        try
        {
            var result = await _wechatAuthService.GenerateQrCodeAsync(request.DeviceType);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取登录二维码失败");
            return StatusCode(500, WechatApiResponse<WechatQrCodeResponse>.Fail("服务器错误"));
        }
    }

    /// <summary>
    /// 查询登录状态（轮询）
    /// </summary>
    [HttpGet("status/{ticket}")]
    [AllowAnonymous]
    public async Task<ActionResult<WechatApiResponse<WechatLoginStatusResponse>>> GetLoginStatus(string ticket)
    {
        try
        {
            var result = await _wechatAuthService.GetLoginStatusAsync(ticket);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询登录状态失败");
            return StatusCode(500, WechatApiResponse<WechatLoginStatusResponse>.Fail("服务器错误"));
        }
    }

    /// <summary>
    /// 微信登录回调（微信服务器调用）
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            var result = await _wechatAuthService.HandleWechatCallbackAsync(code, state);

            // 重定向到前端结果页面
            if (result.Success)
            {
                return Redirect($"/auth/wechat/result?ticket={state}&status=success");
            }
            return Redirect($"/auth/wechat/result?ticket={state}&status=fail&message={Uri.EscapeDataString(result.Message)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "微信登录回调处理失败");
            return Redirect($"/auth/wechat/result?status=error&message={Uri.EscapeDataString("服务器错误")}");
        }
    }

    #endregion

    #region 微信绑定

    /// <summary>
    /// 生成微信绑定二维码（需登录）
    /// </summary>
    [HttpPost("bind-qrcode")]
    [Authorize]
    public async Task<ActionResult<WechatApiResponse<WechatBindQrResponse>>> GenerateBindQrCode()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(WechatApiResponse<WechatBindQrResponse>.Fail("未登录"));
            }

            var result = await _wechatAuthService.GenerateBindQrCodeAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成绑定二维码失败");
            return StatusCode(500, WechatApiResponse<WechatBindQrResponse>.Fail("服务器错误"));
        }
    }

    /// <summary>
    /// 查询绑定状态（轮询）
    /// </summary>
    [HttpGet("bind-status/{bindId}")]
    [Authorize]
    public async Task<ActionResult<WechatApiResponse<WechatBindStatusResponse>>> GetBindStatus(string bindId)
    {
        try
        {
            var result = await _wechatAuthService.GetBindStatusAsync(bindId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询绑定状态失败");
            return StatusCode(500, WechatApiResponse<WechatBindStatusResponse>.Fail("服务器错误"));
        }
    }

    /// <summary>
    /// 微信绑定回调（微信服务器调用）
    /// </summary>
    [HttpGet("bind-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> BindCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            var result = await _wechatAuthService.HandleBindCallbackAsync(code, state);

            // 重定向到前端绑定结果页面
            if (result.Success)
            {
                return Redirect($"/profile/wechat-bind/result?status=success");
            }
            return Redirect($"/profile/wechat-bind/result?status=fail&message={Uri.EscapeDataString(result.Message)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "微信绑定回调处理失败");
            return Redirect($"/profile/wechat-bind/result?status=error&message={Uri.EscapeDataString("服务器错误")}");
        }
    }

    /// <summary>
    /// 获取当前微信绑定信息
    /// </summary>
    [HttpGet("bind-info")]
    [Authorize]
    public async Task<ActionResult<WechatApiResponse<WechatBindInfo>>> GetBindInfo()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(WechatApiResponse<WechatBindInfo>.Fail("未登录"));
            }

            var result = await _wechatAuthService.GetBindInfoAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取绑定信息失败");
            return StatusCode(500, WechatApiResponse<WechatBindInfo>.Fail("服务器错误"));
        }
    }

    /// <summary>
    /// 解除微信绑定
    /// </summary>
    [HttpPost("unbind")]
    [Authorize]
    public async Task<ActionResult<WechatApiResponse<bool>>> Unbind()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(WechatApiResponse<bool>.Fail("未登录"));
            }

            var result = await _wechatAuthService.UnbindWechatAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除绑定失败");
            return StatusCode(500, WechatApiResponse<bool>.Fail("服务器错误"));
        }
    }

    #endregion
}
