using CRM.API.Authorization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using FluentAssertions;

namespace CRM.API.Tests.Authorization;

public sealed class ManagementAccountPolicyTests
{
    private static UserPermissionSummaryDto Actor(bool sa = false, bool admin = false, bool mgr = false) =>
        new()
        {
            UserId = "u1",
            IsSysAdmin = sa,
            IsSysManager = admin,
            IsBizManager = mgr
        };

    [Fact]
    public void CanResetTargetPassword_SysAdmin_CanResetSuperAdmin()
    {
        ManagementAccountPolicy.CanResetTargetPassword(
            Actor(sa: true),
            new[] { ManagementRoleCodes.SuperAdmin }).Should().BeTrue();
    }

    [Fact]
    public void CanResetTargetPassword_SysManager_CannotResetSuperAdmin()
    {
        ManagementAccountPolicy.CanResetTargetPassword(
            Actor(admin: true),
            new[] { ManagementRoleCodes.SuperAdmin }).Should().BeFalse();
    }

    [Fact]
    public void CanResetTargetPassword_BizManager_CannotResetSuperAdmin()
    {
        ManagementAccountPolicy.CanResetTargetPassword(
            Actor(mgr: true),
            new[] { ManagementRoleCodes.SuperAdmin }).Should().BeFalse();
    }

    [Fact]
    public void CanResetTargetPassword_SysAdmin_CanResetOrdinaryEmployee()
    {
        ManagementAccountPolicy.CanResetTargetPassword(
            Actor(sa: true),
            new[] { "DEPT_EMPLOYEE" }).Should().BeTrue();
    }
}
