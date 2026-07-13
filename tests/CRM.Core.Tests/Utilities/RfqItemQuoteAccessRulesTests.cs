using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class RfqItemQuoteAccessRulesTests
{
    private static UserPermissionSummaryDto Summary(
        bool isSysAdmin = false,
        short identityType = 0,
        params string[] roleCodes) =>
        new()
        {
            UserId = "u1",
            IsSysAdmin = isSysAdmin,
            IdentityType = identityType,
            RoleCodes = roleCodes
        };

    [Fact]
    public void CanQuote_SysAdmin_Allows()
    {
        var item = new RFQItem { AssignedPurchaserUserId1 = "other" };
        Assert.True(RfqItemQuoteAccessRules.CanQuote(Summary(isSysAdmin: true), item, "u1"));
    }

    [Fact]
    public void CanQuote_PurchaseDirector_Allows()
    {
        var item = new RFQItem { AssignedPurchaserUserId1 = "other" };
        var summary = Summary(false, 2, "DEPT_DIRECTOR");
        Assert.True(RfqItemQuoteAccessRules.CanQuote(summary, item, "u1"));
    }

    [Fact]
    public void CanQuote_SalesDirector_Denies()
    {
        var item = new RFQItem { AssignedPurchaserUserId1 = "other" };
        var summary = Summary(false, 1, "DEPT_DIRECTOR");
        Assert.False(RfqItemQuoteAccessRules.CanQuote(summary, item, "u1"));
    }

    [Fact]
    public void CanQuote_AssignedQuoter_Allows()
    {
        var item = new RFQItem { AssignedPurchaserUserId2 = "u1" };
        Assert.True(RfqItemQuoteAccessRules.CanQuote(Summary(), item, "u1"));
    }

    [Fact]
    public void CanQuote_UnassignedUser_Denies()
    {
        var item = new RFQItem { AssignedPurchaserUserId1 = "other" };
        Assert.False(RfqItemQuoteAccessRules.CanQuote(Summary(), item, "u1"));
    }

    [Fact]
    public void CanQuote_ProtectionExpired_AllowsPurchaseDeptMember()
    {
        var now = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
        var item = new RFQItem
        {
            AssignedPurchaserUserId1 = "other",
            CreateTime = now.AddMinutes(-31)
        };
        var summary = new UserPermissionSummaryDto
        {
            UserId = "u1",
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 1
        };
        Assert.True(RfqItemQuoteAccessRules.CanQuote(summary, item, "u1", 30, now));
    }

    [Fact]
    public void CanQuote_WithinProtection_DeniesUnassigned()
    {
        var now = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
        var item = new RFQItem
        {
            AssignedPurchaserUserId1 = "other",
            CreateTime = now.AddMinutes(-10)
        };
        var summary = new UserPermissionSummaryDto
        {
            UserId = "u1",
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 1
        };
        Assert.False(RfqItemQuoteAccessRules.CanQuote(summary, item, "u1", 30, now));
    }

    [Fact]
    public void CanQuote_NoProtectionPeriod_AllowsPurchaseDeptMember()
    {
        var now = new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);
        var item = new RFQItem
        {
            AssignedPurchaserUserId1 = "other",
            CreateTime = now
        };
        var summary = new UserPermissionSummaryDto
        {
            UserId = "u1",
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 1
        };
        Assert.True(RfqItemQuoteAccessRules.CanQuote(summary, item, "u1", 0, now));
    }
}
