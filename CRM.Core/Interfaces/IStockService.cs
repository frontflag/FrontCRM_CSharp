using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 库存查询服务接口（库存列表）
    /// </summary>
    public interface IStockService
    {
        Task<IEnumerable<StockInfo>> GetAllAsync();
        Task<IEnumerable<StockInfo>> GetByWarehouseIdAsync(string warehouseId);
    }
}
