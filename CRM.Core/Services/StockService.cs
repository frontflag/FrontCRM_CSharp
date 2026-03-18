using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services
{
    /// <summary>
    /// 库存查询服务实现
    /// </summary>
    public class StockService : IStockService
    {
        private readonly IRepository<StockInfo> _stockRepository;

        public StockService(IRepository<StockInfo> stockRepository)
        {
            _stockRepository = stockRepository;
        }

        public async Task<IEnumerable<StockInfo>> GetAllAsync()
        {
            return await _stockRepository.GetAllAsync();
        }

        public async Task<IEnumerable<StockInfo>> GetByWarehouseIdAsync(string warehouseId)
        {
            if (string.IsNullOrWhiteSpace(warehouseId))
                return await _stockRepository.GetAllAsync();

            var all = await _stockRepository.GetAllAsync();
            return all.Where(s => s.WarehouseId == warehouseId);
        }
    }
}
