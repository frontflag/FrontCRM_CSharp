using System.Linq.Expressions;
using System.Reflection;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Tests.Fakes;

/// <summary>
/// 内存版 <see cref="IRepository{T}"/>，用于业务场景测试中的模拟持久化读写。
/// </summary>
public sealed class MemoryRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly List<T> _store = new();
    private readonly object _gate = new();

    public Task<T?> GetByIdAsync(string id)
    {
        lock (_gate)
            return Task.FromResult(_store.FirstOrDefault(e => GetStringId(e) == id));
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        lock (_gate)
            return Task.FromResult<IEnumerable<T>>(_store.ToList());
    }

    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var fn = predicate.Compile();
        lock (_gate)
            return Task.FromResult<IEnumerable<T>>(_store.Where(fn).ToList());
    }

    public Task<IEnumerable<T>> FindIgnoreFiltersAsync(Expression<Func<T, bool>> predicate) =>
        FindAsync(predicate);

    public Task AddAsync(T entity)
    {
        lock (_gate)
            _store.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity)
    {
        var id = GetStringId(entity);
        lock (_gate)
        {
            var idx = _store.FindIndex(e => GetStringId(e) == id);
            if (idx >= 0)
                _store[idx] = entity;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        lock (_gate)
            _store.RemoveAll(e => GetStringId(e) == id);
        return Task.CompletedTask;
    }

    /// <summary>测试断言用快照（副本）。</summary>
    public IReadOnlyList<T> Snapshot()
    {
        lock (_gate)
            return _store.ToList();
    }

    private static string GetStringId(T entity)
    {
        var p = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (p?.GetValue(entity) is string s)
            return s;
        throw new InvalidOperationException($"{typeof(T).Name} 需要可读的 string 类型 Id 属性。");
    }
}
