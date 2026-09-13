using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class WorkCalendarDotRulesTests
{
    [Theory]
    [InlineData((short)RfqMainStatus.Assigned, true)]
    [InlineData((short)RfqMainStatus.Quoting, true)]
    [InlineData((short)RfqMainStatus.Closed, true)]
    [InlineData((short)RfqMainStatus.PendingAssign, false)]
    [InlineData((short)RfqMainStatus.Cancelled, false)]
    [InlineData((short)RfqMainStatus.LegacyObsoleteClosed, false)]
    public void Rfq_Dot_Uses_Assigned_And_After(short status, bool expected)
    {
        Assert.Equal(expected, WorkCalendarDotRules.CountsAsRfqAssignedDot(status));
    }

    [Theory]
    [InlineData(SellOrderMainStatus.Approved, true)]
    [InlineData(SellOrderMainStatus.InProgress, true)]
    [InlineData(SellOrderMainStatus.Completed, true)]
    [InlineData(SellOrderMainStatus.New, false)]
    [InlineData(SellOrderMainStatus.PendingAudit, false)]
    [InlineData(SellOrderMainStatus.Cancelled, false)]
    [InlineData(SellOrderMainStatus.AuditFailed, false)]
    public void So_Dot_Uses_Approved_And_After(SellOrderMainStatus status, bool expected)
    {
        Assert.Equal(expected, WorkCalendarDotRules.CountsAsSalesOrderApprovedDot(status));
    }
}

public class WorkTaskStatusRulesTests
{
    [Fact]
    public void Complete_From_Pending_Or_Doing()
    {
        Assert.True(WorkTaskStatusRules.CanComplete(10));
        Assert.True(WorkTaskStatusRules.CanComplete(20));
        Assert.False(WorkTaskStatusRules.CanComplete(30));
        Assert.False(WorkTaskStatusRules.CanComplete(90));
    }

    [Fact]
    public void Orange_Dot_Skips_Deleted_And_Cancelled()
    {
        Assert.True(WorkTaskStatusRules.CountsAsOrangeDot(10, false));
        Assert.True(WorkTaskStatusRules.CountsAsOrangeDot(30, false));
        Assert.False(WorkTaskStatusRules.CountsAsOrangeDot(90, false));
        Assert.False(WorkTaskStatusRules.CountsAsOrangeDot(10, true));
    }
}

public class CompanyCalendarDateTests
{
    [Fact]
    public void Utc_Evening_Stays_Same_Shanghai_Day()
    {
        var utc = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 9, 12), CompanyCalendarDate.ToCompanyDate(utc));
    }

    [Fact]
    public void Utc_Late_Maps_Next_Shanghai_Day()
    {
        var utc = new DateTime(2026, 9, 12, 16, 30, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 9, 13), CompanyCalendarDate.ToCompanyDate(utc));
    }
}
