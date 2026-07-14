using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class FinanceAccumulatedMonthBoundaryTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void MonthRangeUtc_AllMonths_DoNotThrow(int month)
    {
        var (start, end) = FinanceAccumulatedMonthBoundary.MonthRangeUtc(2026, month);
        Assert.True(end > start);
    }

    [Fact]
    public void MonthRangeUtc_December_EndsAtNextYearJanuary()
    {
        var (start, end) = FinanceAccumulatedMonthBoundary.MonthRangeUtc(2026, 12);
        var jan2027 = FinanceAccumulatedMonthBoundary.MonthStartUtc(2027, 1);
        Assert.Equal(jan2027, end);
        Assert.True(start < end);
    }
}
