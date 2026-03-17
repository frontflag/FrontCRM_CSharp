namespace CRM.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        Task<IEnumerable<T>> QueryAsync<T>(string sql) where T : class, new();
        Task ExecuteAsync(string sql);
    }
}
