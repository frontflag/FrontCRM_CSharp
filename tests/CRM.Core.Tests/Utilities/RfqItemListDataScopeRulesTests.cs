using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class RfqItemListDataScopeRulesTests
{
    private static UserPermissionSummaryDto Summary(
        bool bypass = false,
        short sale = 1,
        short purchase = 1,
        bool sysAdmin = false) =>
        new()
        {
            UserId = "admin-1",
            HasBizDataBypass = bypass,
            IsSysAdmin = sysAdmin,
            SaleDataScope = sale,
            PurchaseDataScope = purchase
        };

    [Fact]
    public void SysManager_HasBizDataBypass_DoesNotApplyJobPageScope()
    {
        var summary = Summary(bypass: true, sale: 1, purchase: 1);
        summary.IsSysManager = true;
        Assert.False(RfqItemListDataScopeRules.ShouldApplyJobPageScope(summary));
    }

    [Fact]
    public void SysBizManager_HasBizDataBypass_DoesNotApplyJobPageScope()
    {
        var summary = Summary(bypass: true, sale: 2, purchase: 1);
        summary.IsBizManager = true;
        Assert.False(RfqItemListDataScopeRules.ShouldApplyJobPageScope(summary));
    }

    [Fact]
    public void SysAdmin_HasBizDataBypass_DoesNotApplyJobPageScope()
    {
        Assert.False(RfqItemListDataScopeRules.ShouldApplyJobPageScope(
            Summary(bypass: true, sysAdmin: true)));
    }

    [Fact]
    public void RegularSales_SelfScope_AppliesJobPageScope()
    {
        Assert.True(RfqItemListDataScopeRules.ShouldApplyJobPageScope(Summary()));
    }

    [Fact]
    public void SaleOrPurchaseAll_DoesNotApplyJobPageScope()
    {
        Assert.False(RfqItemListDataScopeRules.ShouldApplyJobPageScope(Summary(sale: 0, purchase: 1)));
        Assert.False(RfqItemListDataScopeRules.ShouldApplyJobPageScope(Summary(sale: 1, purchase: 0)));
    }

    [Fact]
    public void IsSysAdmin_WithoutBypass_StillApplies_AlignsWithMainList()
    {
        // 主表只认 HasBizDataBypass；生产路径上 SYS_ADMIN 必带 bypass。
        Assert.True(RfqItemListDataScopeRules.ShouldApplyJobPageScope(
            Summary(bypass: false, sysAdmin: true)));
    }

    [Fact]
    public void NullSummary_DoesNotApply()
    {
        Assert.False(RfqItemListDataScopeRules.ShouldApplyJobPageScope(null));
    }
}
