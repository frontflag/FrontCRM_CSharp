using System.Linq.Expressions;
using CRM.Core.Models;

namespace CRM.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        /// <summary>只读查询，不跟踪实体（避免大表全量加载后 SaveChanges 误写库存等）。</summary>
        Task<IEnumerable<T>> FindAsNoTrackingAsync(Expression<Func<T, bool>> predicate);
        /// <summary>忽略全局查询过滤器（如软删除过滤器），用于回收站等场景</summary>
        Task<IEnumerable<T>> FindIgnoreFiltersAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(string id);
    }
}
