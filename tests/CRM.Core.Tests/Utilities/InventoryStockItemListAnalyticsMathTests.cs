using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class InventoryStockItemTurnoverTests
{
    [Fact]
    public void Days_ZeroOnHand_ReturnsNull()
    {
        Assert.Null(InventoryStockItemTurnover.Days(0, 10));
    }

    [Fact]
    public void Days_ZeroOutbound_ReturnsNull()
    {
        Assert.Null(InventoryStockItemTurnover.Days(100, 0));
    }

    [Fact]
    public void Days_NegativeOutbound_ReturnsNull()
    {
        Assert.Null(InventoryStockItemTurnover.Days(100, -1));
    }

    [Fact]
    public void Days_Normal_RoundsToOneDecimal()
    {
        Assert.Equal(60.0m, InventoryStockItemTurnover.Days(100, 50));
        Assert.Equal(45.0m, InventoryStockItemTurnover.Days(15, 10));
    }
}

public class InventoryAnalyticsAgeBucketTests
{
    [Theory]
    [InlineData(0, InventoryAnalyticsAgeBucket.D0To30)]
    [InlineData(30, InventoryAnalyticsAgeBucket.D0To30)]
    [InlineData(31, InventoryAnalyticsAgeBucket.D31To90)]
    [InlineData(90, InventoryAnalyticsAgeBucket.D31To90)]
    [InlineData(91, InventoryAnalyticsAgeBucket.D91To180)]
    [InlineData(180, InventoryAnalyticsAgeBucket.D91To180)]
    [InlineData(181, InventoryAnalyticsAgeBucket.D181To365)]
    [InlineData(365, InventoryAnalyticsAgeBucket.D181To365)]
    [InlineData(366, InventoryAnalyticsAgeBucket.D365Plus)]
    public void Classify_MatchesLogisticsBuckets(int ageDays, string expected)
    {
        Assert.Equal(expected, InventoryAnalyticsAgeBucket.Classify(ageDays));
    }
}

public class InventoryAnalyticsTrendWindowTests
{
    [Fact]
    public void TryResolveRange_DefaultDay_Has30Points()
    {
        var today = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(InventoryAnalyticsTrendWindow.TryResolveRange(
            today, "day", null, null, out var from, out var to));
        var keys = InventoryAnalyticsTrendWindow.BuildPeriodKeys(from, to, "day");
        Assert.Equal(30, keys.Count);
        Assert.Equal(today, to);
    }

    [Fact]
    public void TryResolveRange_InboundInPast_UsesWindowEndingAtInboundTo()
    {
        var today = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(InventoryAnalyticsTrendWindow.TryResolveRange(
            today,
            "day",
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31),
            out var from,
            out var to));
        Assert.Equal(new DateTime(2024, 1, 2), from);
        Assert.Equal(new DateTime(2024, 1, 31), to);
        Assert.Equal(30, InventoryAnalyticsTrendWindow.BuildPeriodKeys(from, to, "day").Count);
    }

    [Fact]
    public void TryResolveRange_InboundIntersect_ClipsToInboundTo()
    {
        var today = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);
        Assert.True(InventoryAnalyticsTrendWindow.TryResolveRange(
            today,
            "day",
            today.AddDays(-10),
            today.AddDays(-2),
            out var from,
            out var to));
        Assert.Equal(today.AddDays(-10), from);
        Assert.Equal(today.AddDays(-2), to);
        Assert.Equal(9, InventoryAnalyticsTrendWindow.BuildPeriodKeys(from, to, "day").Count);
    }
}
