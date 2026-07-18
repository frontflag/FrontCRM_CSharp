using System.Globalization;

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

        /// <summary>解析查询字符串中的瞬时时间（ISO-8601 / round-trip），归一为 UTC。</summary>
        public static DateTime? ParseDateTimeUtc(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!DateTime.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var d)
                && !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out d))
                return null;
            return ToUtc(d);
        }
    }
}

