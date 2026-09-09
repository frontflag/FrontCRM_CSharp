using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionSummaryRosterTests
{
    [Fact]
    public void Active_users_appear_even_with_zero()
    {
        var roster = new[]
        {
            new CommissionRosterPerson("a", "Alice", 2),
            new CommissionRosterPerson("b", "Bob", 1)
        };
        var merged = CommissionSummaryRoster.Merge([], roster, null);
        Assert.Equal(2, merged.Count);
        Assert.All(merged, x => Assert.Equal(0m, x.CommissionUsd));
    }

    [Fact]
    public void Inactive_only_if_commission_positive()
    {
        var lines = new[]
        {
            new CommissionSummaryDto { UserId = "gone", UserName = "Old", UserLevel = 3, LineCount = 1, PeriodGpUsd = 10, CommissionUsd = 1.5m },
            new CommissionSummaryDto { UserId = "zero", UserName = "ZeroLeft", UserLevel = 1, LineCount = 1, PeriodGpUsd = 0, CommissionUsd = 0m }
        };
        var merged = CommissionSummaryRoster.Merge(lines, [], null);
        Assert.Single(merged);
        Assert.Equal("gone", merged[0].UserId);
    }

    [Fact]
    public void Keyword_filters_zero_roster_rows()
    {
        var roster = new[]
        {
            new CommissionRosterPerson("a", "Alice", 1),
            new CommissionRosterPerson("b", "Bob", 1)
        };
        var merged = CommissionSummaryRoster.Merge([], roster, "li");
        Assert.Single(merged);
        Assert.Equal("a", merged[0].UserId);
    }
}
