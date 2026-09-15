using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomerNewsMonitorAccessRulesTests
{
    [Fact]
    public void SysAdmin_can_fetch()
    {
        var summary = new UserPermissionSummaryDto { IsSysAdmin = true };
        Assert.True(CustomerNewsMonitorAccessRules.CanFetch(summary, false, false));
    }

    [Fact]
    public void SysAdmin_role_code_can_fetch_without_flag()
    {
        var summary = new UserPermissionSummaryDto { RoleCodes = new[] { "SYS_ADMIN" } };
        Assert.True(CustomerNewsMonitorAccessRules.CanFetch(summary, false, false));
    }

    [Fact]
    public void SysManager_can_fetch()
    {
        var summary = new UserPermissionSummaryDto { IsSysManager = true };
        Assert.True(CustomerNewsMonitorAccessRules.CanFetch(summary, false, false));
    }

    [Fact]
    public void Intel_permission_can_fetch()
    {
        var summary = new UserPermissionSummaryDto
        {
            PermissionCodes = new[] { "biz.ai.customer_intel.lookup" }
        };
        Assert.True(CustomerNewsMonitorAccessRules.CanFetch(summary, false, false));
    }

    [Fact]
    public void Sales_owner_can_fetch()
    {
        var summary = new UserPermissionSummaryDto { UserId = "u1" };
        Assert.True(CustomerNewsMonitorAccessRules.CanFetch(summary, true, false));
    }

    [Fact]
    public void Dept_manager_of_salesperson_can_fetch()
    {
        var summary = new UserPermissionSummaryDto
        {
            RoleCodes = new[] { "DEPT_MANAGER" }
        };
        Assert.True(CustomerNewsMonitorAccessRules.CanFetch(summary, false, true));
    }

    [Fact]
    public void Peer_without_intel_cannot_fetch()
    {
        var summary = new UserPermissionSummaryDto
        {
            RoleCodes = new[] { "DEPT_STAFF" }
        };
        Assert.False(CustomerNewsMonitorAccessRules.CanFetch(summary, false, true));
    }
}
