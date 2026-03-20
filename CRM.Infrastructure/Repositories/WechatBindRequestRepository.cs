using CRM.Core.Interfaces;
using CRM.Core.Models.Auth;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Repositories;

/// <summary>
/// 微信绑定请求仓储实现
/// </summary>
public class WechatBindRequestRepository : Repository<WechatBindRequest>, IWechatBindRequestRepository
{
    public WechatBindRequestRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<WechatBindRequest?> GetPendingByUserIdAsync(string userId)
    {
        return await _context.WechatBindRequests
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "pending");
    }

    public async Task<int> CleanExpiredAsync(DateTime before)
    {
        var expired = await _context.WechatBindRequests
            .Where(r => r.ExpireTime < before && r.Status == "pending")
            .ToListAsync();

        _context.WechatBindRequests.RemoveRange(expired);
        return await _context.SaveChangesAsync();
    }
}
