using CRM.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CRM.Infrastructure.Data
{
    /// <summary>
    /// 在 SaveChanges 前将实体上所有 <see cref="DateTime"/> / <see cref="DateTime?"/> 规范为 UTC，
    /// 避免 Npgsql 写入 timestamptz 时因 Unspecified 抛错。
    /// </summary>
    public sealed class UtcDateTimePropertiesInterceptor : ISaveChangesInterceptor
    {
        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            NormalizeDateTimes(eventData.Context);
            return result;
        }

        public ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            NormalizeDateTimes(eventData.Context);
            return ValueTask.FromResult(result);
        }

        private static void NormalizeDateTimes(DbContext? context)
        {
            if (context == null) return;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                    continue;

                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsShadowProperty())
                        continue;

                    var clr = prop.Metadata.ClrType;
                    if (clr == typeof(DateTime))
                    {
                        var dt = (DateTime)prop.CurrentValue!;
                        prop.CurrentValue = PostgreSqlDateTime.ToUtc(dt);
                    }
                    else if (clr == typeof(DateTime?))
                    {
                        var dt = (DateTime?)prop.CurrentValue;
                        if (dt.HasValue)
                            prop.CurrentValue = PostgreSqlDateTime.ToUtc(dt.Value);
                    }
                }
            }
        }
    }
}
