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
}
