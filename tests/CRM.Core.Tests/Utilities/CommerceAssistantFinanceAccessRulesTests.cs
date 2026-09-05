using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommerceAssistantFinanceAccessRulesTests
{
    [Fact]
    public void CanAccessReceiptSideMenus_commerce_assistant_true()
    {
        var summary = new UserPermissionSummaryDto
        {
            IdentityType = 4,
            HasBizDataBypass = false,
            FinanceDataScope = 4
        };

        Assert.True(CommerceAssistantFinanceAccessRules.CanAccessReceiptSideMenus(summary));
        Assert.True(CommerceAssistantFinanceAccessRules.ShouldBypassFinanceDataScopeDenial(summary));
    }

    [Fact]
    public void CanAccessReceiptSideMenus_bypass_false()
    {
        var summary = new UserPermissionSummaryDto
        {
            IdentityType = 4,
            HasBizDataBypass = true
        };

        Assert.False(CommerceAssistantFinanceAccessRules.CanAccessReceiptSideMenus(summary));
    }

    [Fact]
    public void CanAccessReceiptSideMenus_sales_identity_false()
    {
        var summary = new UserPermissionSummaryDto
        {
            IdentityType = 1,
            HasBizDataBypass = false
        };

        Assert.False(CommerceAssistantFinanceAccessRules.CanAccessReceiptSideMenus(summary));
    }
}
