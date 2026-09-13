using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class IndustryNewsWindowTests
{
    [Fact]
    public void Window_Is_Yesterday_And_Day_Before()
    {
        var briefing = new DateOnly(2026, 9, 14);
        var (start, end) = IndustryNewsWindow.ForBriefingDate(briefing);
        Assert.Equal(new DateOnly(2026, 9, 12), start);
        Assert.Equal(new DateOnly(2026, 9, 13), end);
    }

    [Fact]
    public void Job_Runs_From_Eight_Shanghai()
    {
        Assert.False(IndustryNewsWindow.CanRunAt(new DateTime(2026, 9, 14, 7, 59, 0)));
        Assert.True(IndustryNewsWindow.CanRunAt(new DateTime(2026, 9, 14, 8, 0, 0)));
        Assert.True(IndustryNewsWindow.CanRunAt(new DateTime(2026, 9, 14, 23, 10, 0)));
    }
}
