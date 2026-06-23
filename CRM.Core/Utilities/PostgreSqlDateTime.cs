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

        /// <summary>解析查询字符串中的日历日（YYYY-MM-DD），转为 UTC 日界。</summary>
        public static DateTime? ParseDateOnly(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return DateTime.TryParse(text, out var d) ? ToUtc(d.Date) : null;
        }
    }
}
