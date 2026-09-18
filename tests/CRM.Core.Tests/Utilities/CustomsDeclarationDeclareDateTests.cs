using CRM.Core.Constants;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class CustomsDeclarationDeclareDateTests
{
    [Fact]
    public void Empty_WhenNoStockOut()
    {
        Assert.Null(CustomsDeclarationDeclareDate.Resolve(Array.Empty<(short, DateTime?)>()));
    }

    [Fact]
    public void Empty_WhenReadyNotCompleted()
    {
        var date = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        Assert.Null(CustomsDeclarationDeclareDate.Resolve(
            [(StockOutStatusCode.Ready, date)]));
    }

    [Fact]
    public void Empty_WhenCompletedWithoutStockOutDate()
    {
        Assert.Null(CustomsDeclarationDeclareDate.Resolve(
            [(StockOutStatusCode.Completed, null)]));
    }

    [Fact]
    public void UsesStockOutDate_WhenCompleted()
    {
        var date = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(date, CustomsDeclarationDeclareDate.Resolve(
            [(StockOutStatusCode.Completed, date)]));
    }

    [Fact]
    public void PicksLatest_WhenMultipleCompleted()
    {
        var earlier = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var later = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(later, CustomsDeclarationDeclareDate.Resolve(
        [
            (StockOutStatusCode.Completed, earlier),
            (StockOutStatusCode.Ready, later.AddDays(1)),
            (StockOutStatusCode.Completed, later)
        ]));
    }
}
