using CRM.Core.Constants;
using FluentAssertions;

namespace CRM.Core.Tests.Constants;

public sealed class SystemPermissionCodesReportParamsTests
{
    [Fact]
    public void ReportGlobalRead_IsPageSubPermission()
    {
        SystemPermissionCodes.IsParamsPageSubPermission(SystemPermissionCodes.ParamsReportGlobalRead)
            .Should().BeTrue();
        SystemPermissionCodes.IsParamsModuleMenuPermission(SystemPermissionCodes.ParamsReportRead)
            .Should().BeTrue();
        SystemPermissionCodes.AllSystemPermissions.Should().Contain(SystemPermissionCodes.ParamsReportRead);
        SystemPermissionCodes.DefaultAdminPermissions.Should().Contain(SystemPermissionCodes.ParamsReportGlobalWrite);
    }

    [Fact]
    public void CommissionSalesRead_IsPageSubPermission()
    {
        SystemPermissionCodes.IsParamsPageSubPermission(SystemPermissionCodes.ParamsCommissionSalesRead)
            .Should().BeTrue();
        SystemPermissionCodes.IsParamsModuleMenuPermission(SystemPermissionCodes.ParamsCommissionRead)
            .Should().BeTrue();
        SystemPermissionCodes.AllSystemPermissions.Should().Contain(SystemPermissionCodes.ParamsCommissionRead);
        SystemPermissionCodes.DefaultAdminPermissions.Should().Contain(SystemPermissionCodes.ParamsCommissionPurchaseWrite);
    }

    [Fact]
    public void RiskAlert_IsMenuPermission_And_AdminOnly()
    {
        SystemPermissionCodes.IsParamsModuleMenuPermission(SystemPermissionCodes.ParamsRiskAlertRead)
            .Should().BeFalse();
        SystemPermissionCodes.AllSystemPermissions.Should().Contain(SystemPermissionCodes.ParamsRiskAlertRead);
        SystemPermissionCodes.AllSystemPermissions.Should().Contain(SystemPermissionCodes.ParamsRiskAlertWrite);
        SystemPermissionCodes.DefaultAdminPermissions.Should().Contain(SystemPermissionCodes.ParamsRiskAlertWrite);
        SystemPermissionCodes.DefaultManagerPermissions.Should().NotContain(SystemPermissionCodes.ParamsRiskAlertRead);
    }
}
