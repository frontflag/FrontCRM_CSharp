using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class ApprovalDecideAccessRulesTests
{
    [Fact]
    public void SysAdmin_Approves_Without_Write_Code()
    {
        Assert.True(ApprovalDecideAccessRules.HasApprovePermission(
            new UserPermissionSummaryDto { IsSysAdmin = true },
            "purchase-order.write"));
    }

    [Fact]
    public void SysManager_Approves_Without_Write_Code()
    {
        Assert.True(ApprovalDecideAccessRules.HasApprovePermission(
            new UserPermissionSummaryDto { IsSysManager = true, IdentityType = 6 },
            "purchase-order.write"));
        Assert.True(ApprovalDecideAccessRules.HasApprovePermission(
            new UserPermissionSummaryDto { IsSysManager = true },
            "vendor.write"));
        Assert.True(ApprovalDecideAccessRules.HasApprovePermission(
            new UserPermissionSummaryDto { IsSysManager = true },
            "finance-payment.write"));
    }

    [Fact]
    public void Ordinary_User_Needs_Write_Code()
    {
        Assert.False(ApprovalDecideAccessRules.HasApprovePermission(
            new UserPermissionSummaryDto
            {
                IsSysManager = false,
                PermissionCodes = new[] { "purchase-order.read" }
            },
            "purchase-order.write"));
        Assert.True(ApprovalDecideAccessRules.HasApprovePermission(
            new UserPermissionSummaryDto
            {
                PermissionCodes = new[] { "purchase-order.write" }
            },
            "purchase-order.write"));
    }

    [Fact]
    public void Null_Denied()
    {
        Assert.False(ApprovalDecideAccessRules.HasApprovePermission(null, "vendor.write"));
    }
}
