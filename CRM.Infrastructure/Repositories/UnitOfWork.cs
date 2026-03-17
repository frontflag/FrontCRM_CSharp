using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CRM.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        /// <summary>执行原生 SQL 查询，将结果映射到 T 类型</summary>
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql) where T : class, new()
        {
            var results = new List<T>();
            var conn = _context.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                using var reader = await cmd.ExecuteReaderAsync();
                var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var columns = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.GetName(i).ToLowerInvariant())
                    .ToList();
                while (await reader.ReadAsync())
                {
                    var obj = new T();
                    foreach (var prop in props)
                    {
                        var colName = prop.Name.ToLowerInvariant();
                        var idx = columns.IndexOf(colName);
                        if (idx >= 0 && !reader.IsDBNull(idx))
                        {
                            try
                            {
                                var val = reader.GetValue(idx);
                                if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                                {
                                    if (val is DateTime dt)
                                        prop.SetValue(obj, DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                                }
                                else
                                {
                                    prop.SetValue(obj, Convert.ChangeType(val, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType));
                                }
                            }
                            catch { /* 类型转换失败时跳过 */ }
                        }
                    }
                    results.Add(obj);
                }
            }
            finally
            {
                if (!wasOpen) await conn.CloseAsync();
            }
            return results;
        }

        /// <summary>执行原生 SQL 命令（INSERT/UPDATE/DELETE）</summary>
        public async Task ExecuteAsync(string sql)
        {
            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
