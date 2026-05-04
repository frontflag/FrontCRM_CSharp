namespace CRM.Core.Interfaces;

/// <summary>需求明细行列表：数据库侧筛选与分页（与 <see cref="IRFQService.GetPagedItemsAsync"/> 配合）。</summary>
public interface IRfqItemListQuery
{
    Task<PagedResult<RFQItemListItem>> GetPagedAsync(
        RFQItemQueryRequest request,
        CancellationToken cancellationToken = default);
}
