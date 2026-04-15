using CRM.Core.Models.Inventory;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CRM.Infrastructure.Data
{
    /// <summary>
    /// EF 物化 <see cref="StockItem"/> 后对齐派生列：<see cref="StockItem.StockOutStatus"/>、
    /// <see cref="StockItem.ProfitOutBizUsd"/>（入库快照 × <c>QtyInbound</c>，与出库扩展行利润分离）。
    /// </summary>
    public sealed class StockItemStockOutStatusMaterializationInterceptor : IMaterializationInterceptor
    {
        public static StockItemStockOutStatusMaterializationInterceptor Instance { get; } = new();

        private StockItemStockOutStatusMaterializationInterceptor()
        {
        }

        public object InitializedInstance(MaterializationInterceptionData structuralType, object entity)
        {
            if (entity is StockItem si)
                si.SyncDenormalizedComputedFields();
            return entity;
        }
    }
}
