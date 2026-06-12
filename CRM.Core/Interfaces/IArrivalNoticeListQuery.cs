using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>到货通知列表：数据库侧分页。</summary>
public interface IArrivalNoticeListQuery
{
    Task<PagedResult<StockInNotify>> GetPagedAsync(
        short? status,
        string? purchaseOrderCode,
        string? freightForwarderOrderNo,
        DateTime? expectedArrivalDate,
        string? noticeId,
        int page,
        int pageSize,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);
}
