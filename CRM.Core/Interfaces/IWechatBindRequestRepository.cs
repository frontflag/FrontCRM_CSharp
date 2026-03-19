using CRM.Core.Models.Auth;

namespace CRM.Core.Interfaces;

/// <summary>
/// 微信绑定请求仓储接口
/// </summary>
public interface IWechatBindRequestRepository : IRepository<WechatBindRequest>
{
    /// <summary>
    /// 获取用户的待处理绑定请求
    /// </summary>
    Task<WechatBindRequest?> GetPendingByUserIdAsync(string userId);

    /// <summary>
    /// 清理过期请求
    /// </summary>
    Task<int> CleanExpiredAsync(DateTime before);
}
