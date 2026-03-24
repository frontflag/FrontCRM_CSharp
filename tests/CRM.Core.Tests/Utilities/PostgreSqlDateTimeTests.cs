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
    }
}
