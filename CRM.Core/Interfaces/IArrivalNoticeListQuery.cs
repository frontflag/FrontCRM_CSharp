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
        short? stockInType,
        string? preset,
        string? pn,
        string? vendorName,
        short? purchaseCurrency,
        int page,
        int pageSize,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>按 Id 批量返回到货通知列表行（与 <see cref="GetPagedAsync"/> 列表字段一致）。</summary>
    Task<List<StockInNotify>> GetByIdsAsync(
        IReadOnlyList<string> ids,
        string? currentUserId = null,
        bool applyDataScope = true,
        CancellationToken cancellationToken = default);
}
