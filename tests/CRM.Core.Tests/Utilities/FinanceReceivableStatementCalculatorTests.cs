using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class FinanceReceivableStatementCalculatorTests
{
    [Fact]
    public void DesignExample_AugustPeriod()
    {
        var events = new[]
        {
            Increase(new DateOnly(2026, 7, 20), 100m, "AR1"),
            Increase(new DateOnly(2026, 8, 5), 50m, "AR2"),
            Receipt(new DateOnly(2026, 8, 12), 40m, "R1"),
            Receipt(new DateOnly(2026, 9, 3), 30m, "R2")
        };

        var header = FinanceReceivableStatementCalculator.Compute(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 7),
            new DateOnly(2026, 9, 7),
            1,
            events,
            out var lines);

        Assert.Equal(100m, header.Opening);
        Assert.Equal(50m, header.PeriodIncrease);
        Assert.Equal(40m, header.PeriodReceived);
        Assert.Equal(110m, header.Ending);
        Assert.Equal(3, lines.Count);
        Assert.Equal(FinanceReceivableStatementLineTypes.Opening, lines[0].LineType);
        Assert.Equal(100m, lines[0].Balance);
        Assert.Equal(150m, lines[1].Balance);
        Assert.Equal(110m, lines[2].Balance);
        Assert.Equal(header.Ending, lines[^1].Balance);
        Assert.Equal(header.PeriodIncrease, lines.Sum(l => l.IncreaseAmount ?? 0m));
        Assert.Equal(header.PeriodReceived, lines.Sum(l => l.ReceivedAmount ?? 0m));
    }

    [Fact]
    public void DeletedWriteOffEquivalent_NotInEvents_Ignored()
    {
        var events = new[]
        {
            Increase(new DateOnly(2026, 8, 1), 80m, "AR1"),
            Receipt(new DateOnly(2026, 8, 10), 20m, "keep")
        };

        var header = FinanceReceivableStatementCalculator.Compute(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 1),
            1,
            events,
            out _);

        Assert.Equal(0m, header.Opening);
        Assert.Equal(80m, header.PeriodIncrease);
        Assert.Equal(20m, header.PeriodReceived);
        Assert.Equal(60m, header.Ending);
    }

    [Fact]
    public void CurrencyIsolation_CallerMustFilterEvents()
    {
        var usdOnly = new[]
        {
            Increase(new DateOnly(2026, 8, 2), 10m, "USD-AR")
        };

        var header = FinanceReceivableStatementCalculator.Compute(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 1),
            2,
            usdOnly,
            out var lines);

        Assert.Equal(2, header.Currency);
        Assert.Equal(10m, header.PeriodIncrease);
        Assert.Equal(10m, header.Ending);
        Assert.DoesNotContain(lines, l => l.DocNo == "RMB-AR");
    }

    [Fact]
    public void ClosedInterval_IncludesBoundaryDates()
    {
        var events = new[]
        {
            Increase(new DateOnly(2026, 8, 1), 5m, "from"),
            Increase(new DateOnly(2026, 8, 31), 7m, "to"),
            Receipt(new DateOnly(2026, 8, 1), 1m, "r-from"),
            Receipt(new DateOnly(2026, 8, 31), 2m, "r-to")
        };

        var header = FinanceReceivableStatementCalculator.Compute(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 1),
            1,
            events,
            out var lines);

        Assert.Equal(0m, header.Opening);
        Assert.Equal(12m, header.PeriodIncrease);
        Assert.Equal(3m, header.PeriodReceived);
        Assert.Equal(9m, header.Ending);
        Assert.Equal(5, lines.Count);
    }

    [Fact]
    public void SameDay_IncreaseBeforeReceipt()
    {
        var events = new[]
        {
            Receipt(new DateOnly(2026, 8, 10), 3m, "R"),
            Increase(new DateOnly(2026, 8, 10), 10m, "AR")
        };

        FinanceReceivableStatementCalculator.Compute(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 1),
            1,
            events,
            out var lines);

        Assert.Equal(FinanceReceivableStatementLineTypes.Increase, lines[1].LineType);
        Assert.Equal(10m, lines[1].Balance);
        Assert.Equal(FinanceReceivableStatementLineTypes.Receipt, lines[2].LineType);
        Assert.Equal(7m, lines[2].Balance);
    }

    [Fact]
    public void InvalidPeriod_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FinanceReceivableStatementCalculator.Compute(
                new DateOnly(2026, 8, 31),
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 1),
                1,
                [],
                out _));
        Assert.Contains("起日", ex.Message);
    }

    [Fact]
    public void OpeningZero_StillEmitsOpeningLine()
    {
        FinanceReceivableStatementCalculator.Compute(
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 1),
            1,
            [],
            out var lines);

        Assert.Single(lines);
        Assert.Equal(FinanceReceivableStatementLineTypes.Opening, lines[0].LineType);
        Assert.Equal(0m, lines[0].Balance);
    }

    private static FinanceReceivableStatementEvent Increase(DateOnly date, decimal amount, string doc) =>
        new()
        {
            LineType = FinanceReceivableStatementLineTypes.Increase,
            BusinessDate = date,
            Amount = amount,
            DocNo = doc,
            Summary = doc
        };

    private static FinanceReceivableStatementEvent Receipt(DateOnly date, decimal amount, string doc) =>
        new()
        {
            LineType = FinanceReceivableStatementLineTypes.Receipt,
            BusinessDate = date,
            Amount = amount,
            DocNo = doc,
            Summary = doc
        };
}
