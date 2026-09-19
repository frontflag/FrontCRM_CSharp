using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class RfqItemAnalyticsBundleKeyTests
{
    [Fact]
    public void Same_Filter_Same_Key()
    {
        var a = Sample();
        var b = Sample();
        Assert.Equal(RfqItemAnalyticsBundleKey.From(a), RfqItemAnalyticsBundleKey.From(b));
    }

    [Fact]
    public void Date_Or_Scope_Change_Changes_Key()
    {
        var a = Sample();
        var b = Sample();
        b.EndDate = a.EndDate!.Value.AddDays(1);
        Assert.NotEqual(RfqItemAnalyticsBundleKey.From(a), RfqItemAnalyticsBundleKey.From(b));

        var c = Sample();
        c.AnalyticsViewLevel = "personal";
        Assert.NotEqual(RfqItemAnalyticsBundleKey.From(a), RfqItemAnalyticsBundleKey.From(c));
    }

    [Fact]
    public void Page_Does_Not_Affect_Key()
    {
        var a = Sample();
        var b = Sample();
        b.PageIndex = 9;
        b.PageSize = 50;
        Assert.Equal(RfqItemAnalyticsBundleKey.From(a), RfqItemAnalyticsBundleKey.From(b));
    }

    private static RFQItemQueryRequest Sample() => new()
    {
        CurrentUserId = "u1",
        StartDate = new DateTime(2026, 3, 18, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc),
        AnalyticsDataset = "reportScope",
        AnalyticsViewLevel = "company",
        CanViewCustomerInList = true
    };
}
