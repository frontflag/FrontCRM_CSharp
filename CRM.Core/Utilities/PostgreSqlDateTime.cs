namespace CRM.Core.Utilities
{
    /// <summary>
    /// PostgreSQL <c>timestamptz</c>（Npgsql）要求写入的 <see cref="DateTime"/> 为 UTC；
    /// JSON/API 反序列化得到的值多为 <see cref="DateTimeKind.Unspecified"/>。
    /// </summary>
    public static class PostgreSqlDateTime
    {
        public static DateTime ToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public static DateTime? ToUtc(DateTime? value)
        {
            if (!value.HasValue) return null;
            return ToUtc(value.Value);
        }
    }
}
