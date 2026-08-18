using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class InventoryOpsCheckAccessRulesTests
{
    [Fact]
    public void SysAdmin_CanAccess()
    {
        Assert.True(InventoryOpsCheckAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IsSysAdmin = true,
            IdentityType = 2
        }));
    }

    [Fact]
    public void SysManager_CanAccess()
    {
        Assert.True(InventoryOpsCheckAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IsSysManager = true
        }));
    }

    [Fact]
    public void FinanceDirector_CanAccess()
    {
        Assert.True(InventoryOpsCheckAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IdentityType = 5,
            RoleCodes = new[] { "DEPT_DIRECTOR" }
        }));
    }

    [Fact]
    public void FinanceEmployee_Denied()
    {
        Assert.False(InventoryOpsCheckAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IdentityType = 5,
            RoleCodes = new[] { "DEPT_EMPLOYEE" }
        }));
    }

    [Fact]
    public void SalesDirector_Denied()
    {
        Assert.False(InventoryOpsCheckAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IdentityType = 1,
            RoleCodes = new[] { "DEPT_DIRECTOR" }
        }));
    }

    [Fact]
    public void Null_Denied()
    {
        Assert.False(InventoryOpsCheckAccessRules.CanAccess(null));
    }
}
