using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities
{
    public class PostgreSqlDateTimeTests
    {
        [Fact]
        public void ToUtc_Unspecified_TreatsClockAsUtc()
        {
            var v = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Unspecified);
            var u = PostgreSqlDateTime.ToUtc(v);
            Assert.Equal(DateTimeKind.Utc, u.Kind);
            Assert.Equal(v.Ticks, u.Ticks);
        }

        [Fact]
        public void ToUtc_Local_ConvertsToUtc()
        {
            var local = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Local);
            var u = PostgreSqlDateTime.ToUtc(local);
            Assert.Equal(DateTimeKind.Utc, u.Kind);
            Assert.Equal(local.ToUniversalTime(), u);
        }

        [Fact]
        public void ToUtc_NullableNull_ReturnsNull()
        {
            DateTime? n = null;
            Assert.Null(PostgreSqlDateTime.ToUtc(n));
        }

        [Fact]
        public void ParseDateOnly_ReturnsUtcMidnight()
        {
            var d = PostgreSqlDateTime.ParseDateOnly("2026-06-22");
            Assert.NotNull(d);
            Assert.Equal(DateTimeKind.Utc, d!.Value.Kind);
            Assert.Equal(new DateTime(2026, 6, 22, 0, 0, 0, DateTimeKind.Utc), d.Value);
        }
    }
}
