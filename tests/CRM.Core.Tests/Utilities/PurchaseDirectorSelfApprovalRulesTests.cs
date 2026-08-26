using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class PurchaseDirectorSelfApprovalRulesTests
{
    [Fact]
    public void SysAdmin_Allowed()
    {
        Assert.True(PurchaseDirectorSelfApprovalRules.AllowsOwnVendorOrPurchaseOrderDecide(
            new UserPermissionSummaryDto { IsSysAdmin = true, IdentityType = 1 }));
    }

    [Fact]
    public void PurchaseDirector_Allowed()
    {
        Assert.True(PurchaseDirectorSelfApprovalRules.AllowsOwnVendorOrPurchaseOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 2,
                RoleCodes = new[] { "DEPT_DIRECTOR" }
            }));
    }

    [Fact]
    public void PurchaseOpsDirector_Allowed()
    {
        Assert.True(PurchaseDirectorSelfApprovalRules.AllowsOwnVendorOrPurchaseOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 3,
                RoleCodes = new[] { "DEPT_DIRECTOR" }
            }));
    }

    [Fact]
    public void PurchaseManager_Denied()
    {
        Assert.False(PurchaseDirectorSelfApprovalRules.AllowsOwnVendorOrPurchaseOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 2,
                RoleCodes = new[] { "DEPT_MANAGER" }
            }));
    }

    [Fact]
    public void SalesDirector_Denied()
    {
        Assert.False(PurchaseDirectorSelfApprovalRules.AllowsOwnVendorOrPurchaseOrderDecide(
            new UserPermissionSummaryDto
            {
                IdentityType = 1,
                RoleCodes = new[] { "DEPT_DIRECTOR" }
            }));
    }

    [Fact]
    public void Null_Denied()
    {
        Assert.False(PurchaseDirectorSelfApprovalRules.AllowsOwnVendorOrPurchaseOrderDecide(null));
    }
}
