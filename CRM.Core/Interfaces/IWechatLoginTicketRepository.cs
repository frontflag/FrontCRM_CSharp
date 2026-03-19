using CRM.Core.Interfaces;
using CRM.Core.Models.Auth;

namespace CRM.Core.Interfaces;

/// <summary>
/// 微信登录票据仓储接口
/// </summary>
public interface IWechatLoginTicketRepository : IRepository<WechatLoginTicket>
{
    /// <summary>
    /// 根据票据获取记录
    /// </summary>
    Task<WechatLoginTicket?> GetByTicketAsync(string ticket);

    /// <summary>
    /// 清理过期票据
    /// </summary>
    Task<int> CleanExpiredAsync(DateTime before);
}
