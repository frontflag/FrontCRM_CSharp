using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class SalesDirectorSelfApprovalRulesTests
{
    [Fact]
    public void SysAdmin_Allowed()
    {
        Assert.True(SalesDirectorSelfApprovalRules.AllowsOwnCustomerOrSalesOrderDecide(
            new UserPermissionSummaryDto { IsSysAdmin = true, IdentityType = 2 }));
    }

    [Fact]
    public void SalesDirector_Allowed()
    {
        Assert.True(SalesDirectorSelfApprovalRules.AllowsOwnCustomerOrSalesOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 1,
                RoleCodes = new[] { "DEPT_DIRECTOR" }
            }));
    }

    [Fact]
    public void SalesManager_Denied()
    {
        Assert.False(SalesDirectorSelfApprovalRules.AllowsOwnCustomerOrSalesOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 1,
                RoleCodes = new[] { "DEPT_MANAGER" }
            }));
    }

    [Fact]
    public void SalesEmployee_Denied()
    {
        Assert.False(SalesDirectorSelfApprovalRules.AllowsOwnCustomerOrSalesOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 1,
                RoleCodes = new[] { "DEPT_EMPLOYEE" }
            }));
    }

    [Fact]
    public void PurchaseDirector_Denied()
    {
        Assert.False(SalesDirectorSelfApprovalRules.AllowsOwnCustomerOrSalesOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 2,
                RoleCodes = new[] { "DEPT_DIRECTOR" }
            }));
    }

    [Fact]
    public void Null_Denied()
    {
        Assert.False(SalesDirectorSelfApprovalRules.AllowsOwnCustomerOrSalesOrderDecide(null));
    }
}
