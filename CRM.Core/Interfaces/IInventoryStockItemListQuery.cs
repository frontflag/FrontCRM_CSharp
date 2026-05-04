using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>全库 <c>stockitem</c> 列表的数据库分页查询（与 <see cref="InventoryStockItemListQuery"/> 筛选语义一致）。</summary>
public interface IInventoryStockItemListQuery
{
    public const int MaxPageSize = 2000;

    Task<PagedResult<InventoryStockItemListRowDto>> GetPagedAsync(
        InventoryStockItemListQuery? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
