using System.Linq.Expressions;
using CRM.Core.Models;

namespace CRM.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        /// <summary>忽略全局查询过滤器（如软删除过滤器），用于回收站等场景</summary>
        Task<IEnumerable<T>> FindIgnoreFiltersAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(string id);
    }
}
