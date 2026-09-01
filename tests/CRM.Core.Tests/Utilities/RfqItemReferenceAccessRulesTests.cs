using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class RfqItemReferenceAccessRulesTests
{
    private static UserPermissionSummaryDto Summary(
        short identityType = 0,
        bool bypass = false,
        bool sysAdmin = false,
        bool sysManager = false,
        bool bizManager = false,
        bool belongsToPurchase = false,
        params string[] roleCodes) =>
        new()
        {
            UserId = "u1",
            IdentityType = identityType,
            HasBizDataBypass = bypass,
            IsSysAdmin = sysAdmin,
            IsSysManager = sysManager,
            IsBizManager = bizManager,
            BelongsToPurchaseDept = belongsToPurchase,
            RoleCodes = roleCodes
        };

    [Fact]
    public void CanEnterPage_SalesPurchaseAndBypass_Allows()
    {
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(1)));
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(2)));
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(3)));
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(0, belongsToPurchase: true)));
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(6, bypass: true)));
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(6, sysAdmin: true)));
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(6, sysManager: true)));
        Assert.True(RfqItemReferenceAccessRules.CanEnterPage(Summary(6, bizManager: true)));
    }

    [Fact]
    public void CanEnterPage_FinanceLogistics_Denies()
    {
        Assert.False(RfqItemReferenceAccessRules.CanEnterPage(Summary(4)));
        Assert.False(RfqItemReferenceAccessRules.CanEnterPage(Summary(5)));
        Assert.False(RfqItemReferenceAccessRules.CanEnterPage(Summary(6)));
        Assert.False(RfqItemReferenceAccessRules.CanEnterPage(null));
    }

    [Fact]
    public void NeedsSalespersonCustomerMask_OnlySalesWithoutBypass()
    {
        Assert.True(RfqItemReferenceAccessRules.NeedsSalespersonCustomerMask(Summary(1)));
        Assert.False(RfqItemReferenceAccessRules.NeedsSalespersonCustomerMask(Summary(1, bypass: true)));
        Assert.False(RfqItemReferenceAccessRules.NeedsSalespersonCustomerMask(Summary(1, sysAdmin: true)));
        Assert.False(RfqItemReferenceAccessRules.NeedsSalespersonCustomerMask(Summary(2)));
        Assert.False(RfqItemReferenceAccessRules.NeedsSalespersonCustomerMask(Summary(3)));
        Assert.False(RfqItemReferenceAccessRules.NeedsSalespersonCustomerMask(Summary(0, belongsToPurchase: true)));
    }

    [Fact]
    public void CanRevealCustomerOnRow_Frontline_OnlySelf()
    {
        var s = Summary(1);
        Assert.True(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, "u1", null));
        Assert.False(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, "other", null));
        Assert.False(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, null, null));
        Assert.False(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, "  ", null));
    }

    [Fact]
    public void CanRevealCustomerOnRow_SalesManager_UsesRevealSet()
    {
        var s = Summary(1, roleCodes: new[] { "DEPT_MANAGER" });
        var reveal = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "u1", "emp1" };
        Assert.True(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, "emp1", reveal));
        Assert.False(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, "peer-manager", reveal));
        Assert.False(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, "emp1", null));
    }

    [Fact]
    public void CanRevealCustomerOnRow_Purchase_DoesNotMaskBySalesperson()
    {
        var s = Summary(2);
        Assert.True(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, "anyone", null));
        Assert.True(RfqItemReferenceAccessRules.CanRevealCustomerOnRow(s, null, null));
    }
}
