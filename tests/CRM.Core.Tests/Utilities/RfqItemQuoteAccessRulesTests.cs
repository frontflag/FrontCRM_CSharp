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
}
