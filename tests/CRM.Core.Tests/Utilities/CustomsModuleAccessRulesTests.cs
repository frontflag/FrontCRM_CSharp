using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class CustomsModuleAccessRulesTests
{
    [Fact]
    public void SysManager_CanAccess_EvenWithoutFinanceOrLogisticsIdentity()
    {
        var summary = new UserPermissionSummaryDto
        {
            IsSysManager = true,
            IdentityType = 1
        };

        Assert.True(CustomsModuleAccessRules.CanAccessModule(summary));
        Assert.True(CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(summary));
    }

    [Fact]
    public void SysAdmin_CanAccess()
    {
        var summary = new UserPermissionSummaryDto { IsSysAdmin = true, IdentityType = 1 };

        Assert.True(CustomsModuleAccessRules.CanAccessModule(summary));
        Assert.True(CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(summary));
    }

    [Fact]
    public void FinanceAndLogistics_CanAccess()
    {
        Assert.True(CustomsModuleAccessRules.CanAccessModule(new UserPermissionSummaryDto { IdentityType = 5 }));
        Assert.True(CustomsModuleAccessRules.CanAccessModule(new UserPermissionSummaryDto { IdentityType = 6 }));
        Assert.True(CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(
            new UserPermissionSummaryDto { IdentityType = 5 }));
        Assert.False(CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(
            new UserPermissionSummaryDto { IdentityType = 6 }));
    }

    [Fact]
    public void HasBizDataBypass_CanAccess_EvenWithoutFinanceOrLogisticsIdentity()
    {
        var summary = new UserPermissionSummaryDto
        {
            HasBizDataBypass = true,
            IdentityType = 1
        };

        Assert.True(CustomsModuleAccessRules.CanAccessModule(summary));
        Assert.True(CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(summary));
    }

    [Fact]
    public void SalesIdentity_WithoutAdmin_Denied()
    {
        var summary = new UserPermissionSummaryDto { IdentityType = 1 };

        Assert.False(CustomsModuleAccessRules.CanAccessModule(summary));
        Assert.False(CustomsModuleAccessRules.BypassLogisticsDataScopeForCustomsList(summary));
    }
}
