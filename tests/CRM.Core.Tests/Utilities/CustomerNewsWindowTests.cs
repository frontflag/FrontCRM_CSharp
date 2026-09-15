using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomerNewsWindowTests
{
    [Fact]
    public void First_fetch_is_last_three_months()
    {
        var today = new DateOnly(2026, 9, 15);
        var (start, end) = CustomerNewsWindow.ForFetch(today, null);
        Assert.Equal(new DateOnly(2026, 6, 15), start);
        Assert.Equal(today, end);
    }

    [Fact]
    public void Next_fetch_starts_after_last_period_end()
    {
        var today = new DateOnly(2026, 9, 15);
        var (start, end) = CustomerNewsWindow.ForFetch(today, new DateOnly(2026, 9, 1));
        Assert.Equal(new DateOnly(2026, 9, 2), start);
        Assert.Equal(today, end);
    }

    [Fact]
    public void Same_day_refetch_collapses_to_today()
    {
        var today = new DateOnly(2026, 9, 15);
        var (start, end) = CustomerNewsWindow.ForFetch(today, today);
        Assert.Equal(today, start);
        Assert.Equal(today, end);
    }
}
