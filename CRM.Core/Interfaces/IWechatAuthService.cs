using CRM.API.Models.DTOs;

namespace CRM.Core.Interfaces;

/// <summary>
/// 微信认证服务接口
/// </summary>
public interface IWechatAuthService
{
    // ==================== 扫码登录 ====================

    /// <summary>
    /// 生成登录二维码
    /// </summary>
    Task<ApiResponse<WechatQrCodeResponse>> GenerateQrCodeAsync(string deviceType);

    /// <summary>
    /// 获取登录状态（供前端轮询）
    /// </summary>
    Task<ApiResponse<WechatLoginStatusResponse>> GetLoginStatusAsync(string ticket);

    /// <summary>
    /// 处理微信登录回调
    /// </summary>
    Task<ApiResponse<bool>> HandleWechatCallbackAsync(string code, string state);

    // ==================== 微信绑定 ====================

    /// <summary>
    /// 生成绑定二维码（已登录用户）
    /// </summary>
    Task<ApiResponse<WechatBindQrResponse>> GenerateBindQrCodeAsync(string userId);

    /// <summary>
    /// 获取绑定状态
    /// </summary>
    Task<ApiResponse<WechatBindStatusResponse>> GetBindStatusAsync(string bindId);

    /// <summary>
    /// 处理微信绑定回调
    /// </summary>
    Task<ApiResponse<bool>> HandleBindCallbackAsync(string code, string bindId);

    /// <summary>
    /// 解除微信绑定
    /// </summary>
    Task<ApiResponse<bool>> UnbindWechatAsync(string userId);

    /// <summary>
    /// 获取用户微信绑定信息
    /// </summary>
    Task<ApiResponse<WechatBindInfo>> GetBindInfoAsync(string userId);
}
