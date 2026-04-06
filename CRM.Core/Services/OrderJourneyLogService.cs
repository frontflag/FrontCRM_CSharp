using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.System;

namespace CRM.Core.Services;

public class OrderJourneyLogService : IOrderJourneyLogService
{
    private readonly IRepository<OrderJourneyLog> _repo;
    private readonly IUnitOfWork _uow;

    public OrderJourneyLogService(IRepository<OrderJourneyLog> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task AppendAsync(OrderJourneyLog entry, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
                entry.Id = Guid.NewGuid().ToString();
            if (entry.EventTime == default)
                entry.EventTime = DateTime.UtcNow;
            await _repo.AddAsync(entry);
            await _uow.SaveChangesAsync();
        }
        catch
        {
            // 旅程日志失败不阻断订单主流程
        }
    }

    public async Task<IReadOnlyList<OrderJourneyLog>> GetBySellOrderIdAsync(string sellOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellOrderId)) return Array.Empty<OrderJourneyLog>();
        var list = await _repo.FindAsync(x =>
            (x.EntityKind == OrderJourneyEntityKinds.SellOrder && x.EntityId == sellOrderId) ||
            (x.ParentEntityKind == OrderJourneyEntityKinds.SellOrder && x.ParentEntityId == sellOrderId));
        return list.OrderBy(x => x.EventTime).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
    }

    public async Task<IReadOnlyList<OrderJourneyLog>> GetByPurchaseOrderIdAsync(string purchaseOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderId)) return Array.Empty<OrderJourneyLog>();
        var list = await _repo.FindAsync(x =>
            (x.EntityKind == OrderJourneyEntityKinds.PurchaseOrder && x.EntityId == purchaseOrderId) ||
            (x.ParentEntityKind == OrderJourneyEntityKinds.PurchaseOrder && x.ParentEntityId == purchaseOrderId));
        return list.OrderBy(x => x.EventTime).ThenBy(x => x.Id, StringComparer.Ordinal).ToList();
    }
}
