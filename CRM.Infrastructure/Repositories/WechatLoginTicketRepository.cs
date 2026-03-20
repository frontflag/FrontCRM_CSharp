using CRM.Core.Interfaces;
using CRM.Core.Models.Auth;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Repositories;

/// <summary>
/// 微信登录票据仓储实现
/// </summary>
public class WechatLoginTicketRepository : Repository<WechatLoginTicket>, IWechatLoginTicketRepository
{
    public WechatLoginTicketRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<WechatLoginTicket?> GetByTicketAsync(string ticket)
    {
        return await _context.WechatLoginTickets
            .FirstOrDefaultAsync(t => t.Ticket == ticket);
    }

    public async Task<int> CleanExpiredAsync(DateTime before)
    {
        var expired = await _context.WechatLoginTickets
            .Where(t => t.ExpireTime < before)
            .ToListAsync();

        _context.WechatLoginTickets.RemoveRange(expired);
        return await _context.SaveChangesAsync();
    }
}
