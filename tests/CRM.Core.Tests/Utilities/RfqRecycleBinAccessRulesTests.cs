using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class RfqRecycleBinAccessRulesTests
{
    [Fact]
    public void SysAdmin_CanAccess()
    {
        Assert.True(RfqRecycleBinAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IsSysAdmin = true,
            IdentityType = 2
        }));
    }

    [Fact]
    public void SysManager_CanAccess()
    {
        Assert.True(RfqRecycleBinAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IsSysManager = true
        }));
    }

    [Fact]
    public void BizManager_CanAccess()
    {
        Assert.True(RfqRecycleBinAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IsBizManager = true
        }));
    }

    [Fact]
    public void SalesDirector_CanAccess()
    {
        Assert.True(RfqRecycleBinAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IdentityType = 1,
            RoleCodes = new[] { "DEPT_DIRECTOR" }
        }));
    }

    [Fact]
    public void SalesEmployee_Denied()
    {
        Assert.False(RfqRecycleBinAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IdentityType = 1,
            RoleCodes = new[] { "DEPT_EMPLOYEE" }
        }));
    }

    [Fact]
    public void PurchaseDirector_Denied()
    {
        Assert.False(RfqRecycleBinAccessRules.CanAccess(new UserPermissionSummaryDto
        {
            IdentityType = 2,
            RoleCodes = new[] { "DEPT_DIRECTOR" }
        }));
    }

    [Fact]
    public void Null_Denied()
    {
        Assert.False(RfqRecycleBinAccessRules.CanAccess(null));
    }
}
