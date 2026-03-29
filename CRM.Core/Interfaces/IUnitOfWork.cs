namespace CRM.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        Task<IEnumerable<T>> QueryAsync<T>(string sql) where T : class, new();
        Task ExecuteAsync(string sql);
        /// <summary>执行非查询 SQL，返回受影响行数（用于需确认落库的场景）</summary>
        Task<int> ExecuteNonQueryAsync(string sql);
    }
}
