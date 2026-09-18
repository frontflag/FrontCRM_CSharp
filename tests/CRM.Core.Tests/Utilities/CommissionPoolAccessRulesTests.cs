using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionPoolAccessRulesTests
{
    static UserPermissionSummaryDto Summary(
        short identityType = 0,
        bool sysAdmin = false,
        bool sysManager = false,
        bool bypass = false,
        bool belongsToPurchase = false,
        string userId = "u1") =>
        new()
        {
            UserId = userId,
            IdentityType = identityType,
            IsSysAdmin = sysAdmin,
            IsSysManager = sysManager,
            HasBizDataBypass = bypass,
            BelongsToPurchaseDept = belongsToPurchase
        };

    [Fact]
    public void CanEnter_AdminManagerBypass_AndSalesPurchase()
    {
        Assert.True(CommissionPoolAccessRules.CanEnter(Summary(sysAdmin: true)));
        Assert.True(CommissionPoolAccessRules.CanEnter(Summary(6, sysManager: true)));
        Assert.True(CommissionPoolAccessRules.CanEnter(Summary(5, bypass: true)));
        Assert.True(CommissionPoolAccessRules.CanEnter(Summary(1)));
        Assert.True(CommissionPoolAccessRules.CanEnter(Summary(2)));
        Assert.True(CommissionPoolAccessRules.CanEnter(Summary(3)));
        Assert.True(CommissionPoolAccessRules.CanEnter(Summary(0, belongsToPurchase: true)));
    }

    [Fact]
    public void CanEnter_FinanceLogisticsCommerce_Denies()
    {
        Assert.False(CommissionPoolAccessRules.CanEnter(null));
        Assert.False(CommissionPoolAccessRules.CanEnter(Summary(0)));
        Assert.False(CommissionPoolAccessRules.CanEnter(Summary(4)));
        Assert.False(CommissionPoolAccessRules.CanEnter(Summary(5)));
        Assert.False(CommissionPoolAccessRules.CanEnter(Summary(6)));
    }

    [Fact]
    public void ResolveRowScope_SeeAll_HasNoRestrict()
    {
        var scope = CommissionPoolAccessRules.ResolveRowScope(Summary(1, sysAdmin: true));
        Assert.Null(scope.RestrictSalesUserId);
        Assert.Null(scope.RestrictPurchaseUserId);
        Assert.False(scope.RestrictEither);
    }

    [Fact]
    public void ResolveRowScope_Salesperson_OnlyOwnSales()
    {
        var scope = CommissionPoolAccessRules.ResolveRowScope(Summary(1, userId: "sales-1"));
        Assert.Equal("sales-1", scope.RestrictSalesUserId);
        Assert.Null(scope.RestrictPurchaseUserId);
        Assert.False(scope.RestrictEither);
    }

    [Fact]
    public void ResolveRowScope_Purchaser_OnlyOwnPurchase()
    {
        var scope = CommissionPoolAccessRules.ResolveRowScope(Summary(2, userId: "pur-1"));
        Assert.Null(scope.RestrictSalesUserId);
        Assert.Equal("pur-1", scope.RestrictPurchaseUserId);
        Assert.False(scope.RestrictEither);
    }

    [Fact]
    public void ResolveRowScope_BothIdentities_EitherSide()
    {
        var scope = CommissionPoolAccessRules.ResolveRowScope(
            Summary(1, belongsToPurchase: true, userId: "both-1"));
        Assert.Equal("both-1", scope.RestrictSalesUserId);
        Assert.Equal("both-1", scope.RestrictPurchaseUserId);
        Assert.True(scope.RestrictEither);
    }
}
