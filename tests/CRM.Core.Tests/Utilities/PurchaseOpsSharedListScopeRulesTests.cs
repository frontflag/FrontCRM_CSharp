using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public sealed class PurchaseOpsSharedListScopeRulesTests
{
    [Fact]
    public void UsesSharedListScope_true_for_purchase_ops_operator_when_scope_not_denied()
    {
        var summary = new UserPermissionSummaryDto
        {
            UserId = "ceci",
            IdentityType = 3,
            SaleDataScope = 4,
            PurchaseDataScope = 3,
            RoleCodes = new[] { "DEPT_EMPLOYEE", "purchase_ops_operator", "logistics_operator" }
        };

        Assert.True(PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary));
    }

    [Fact]
    public void UsesSharedListScope_false_for_purchase_buyer_only()
    {
        var summary = new UserPermissionSummaryDto
        {
            UserId = "buyer",
            IdentityType = 2,
            PurchaseDataScope = 1,
            RoleCodes = new[] { "DEPT_EMPLOYEE", "purchase_buyer" }
        };

        Assert.False(PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary));
    }

    [Fact]
    public void UsesSharedListScope_false_when_purchase_scope_denied()
    {
        var summary = new UserPermissionSummaryDto
        {
            UserId = "ops",
            PurchaseDataScope = 4,
            RoleCodes = new[] { "purchase_ops_operator" }
        };

        Assert.False(PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary));
    }

    [Fact]
    public void UsesSharedListScope_true_for_biz_bypass()
    {
        var summary = new UserPermissionSummaryDto
        {
            HasBizDataBypass = true,
            PurchaseDataScope = 4,
            RoleCodes = Array.Empty<string>()
        };

        Assert.True(PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary));
    }

    [Fact]
    public void UsesSharedListScope_true_for_purchase_ops_even_when_finance_scope_denied()
    {
        var summary = new UserPermissionSummaryDto
        {
            UserId = "ceci",
            IdentityType = 3,
            FinanceDataScope = 4,
            PurchaseDataScope = 3,
            RoleCodes = new[] { "purchase_ops_operator" }
        };

        Assert.True(PurchaseOpsSharedListScopeRules.UsesSharedListScope(summary));
    }
}
