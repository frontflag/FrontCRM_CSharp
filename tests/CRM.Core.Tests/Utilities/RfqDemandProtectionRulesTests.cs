using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class RfqDemandProtectionRulesTests
{
    private static readonly DateTime Now = new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(30, true)]
    public void HasProtectionPeriod_ReflectsMinutes(int minutes, bool expected) =>
        Assert.Equal(expected, RfqDemandProtectionRules.HasProtectionPeriod(minutes));

    [Fact]
    public void IsProtectionExpired_AlwaysTrueWhenNoProtectionPeriod() =>
        Assert.True(RfqDemandProtectionRules.IsProtectionExpired(
            Now, 0, Now));

    [Fact]
    public void IsProtectionExpired_StrictGreaterThanMinutes()
    {
        var createTime = Now.AddMinutes(-30);
        Assert.False(RfqDemandProtectionRules.IsProtectionExpired(createTime, 30, Now));
        Assert.True(RfqDemandProtectionRules.IsProtectionExpired(createTime.AddSeconds(-1), 30, Now));
    }

    [Fact]
    public void CanParticipateInProtectionPool_ExcludesPurchaseScopeNone()
    {
        var summary = new UserPermissionSummaryDto
        {
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 4
        };
        Assert.False(RfqDemandProtectionRules.CanParticipateInProtectionPool(summary));
    }

    [Fact]
    public void CanParticipateInProtectionPool_IncludesPurchaseDeptMember()
    {
        var summary = new UserPermissionSummaryDto
        {
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 1
        };
        Assert.True(RfqDemandProtectionRules.CanParticipateInProtectionPool(summary));
    }

    [Fact]
    public void IsPurchaseSideVisible_ExpiredItemVisibleToOtherPurchaser()
    {
        var item = new RFQItem
        {
            AssignedPurchaserUserId1 = "other",
            CreateTime = Now.AddMinutes(-31)
        };
        var summary = new UserPermissionSummaryDto
        {
            UserId = "u1",
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 1
        };

        Assert.True(RfqDemandProtectionRules.IsPurchaseSideVisible(
            summary, item, "u1", null, 30, Now));
    }

    [Fact]
    public void IsPurchaseSideVisible_NoProtectionPeriod_VisibleToOtherPurchaser()
    {
        var item = new RFQItem
        {
            AssignedPurchaserUserId1 = "other",
            CreateTime = Now
        };
        var summary = new UserPermissionSummaryDto
        {
            UserId = "u1",
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 1
        };

        Assert.True(RfqDemandProtectionRules.IsPurchaseSideVisible(
            summary, item, "u1", null, 0, Now));
    }

    [Fact]
    public void IsPurchaseSideVisible_WithinProtectionOnlyAssigned()
    {
        var item = new RFQItem
        {
            AssignedPurchaserUserId1 = "other",
            CreateTime = Now.AddMinutes(-10)
        };
        var summary = new UserPermissionSummaryDto
        {
            UserId = "u1",
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 1
        };

        Assert.False(RfqDemandProtectionRules.IsPurchaseSideVisible(
            summary, item, "u1", null, 30, Now));
    }
}
