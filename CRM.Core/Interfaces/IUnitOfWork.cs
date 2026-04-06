namespace CRM.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        Task<IEnumerable<T>> QueryAsync<T>(string sql) where T : class, new();
        Task ExecuteAsync(string sql);
        /// <summary>执行非查询 SQL，返回受影响行数（用于需确认落库的场景）</summary>
        Task<int> ExecuteNonQueryAsync(string sql);

        /// <summary>
        /// 按采购明细业务编号（物理列 purchase_order_item_code）解析 purchase_order_id，
        /// 避免 EF 将属性名映射成不存在的 "PurchaseOrderItemCode" 列导致 42703。
        /// </summary>
        Task<string?> GetPurchaseOrderIdByPurchaseOrderItemLineCodeAsync(string purchaseOrderItemCode,
            CancellationToken cancellationToken = default);
    }
}
