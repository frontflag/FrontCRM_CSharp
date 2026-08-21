using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class StockingPurchaseListAccessTests
{
    [Fact]
    public void SysAdmin_CanEnter()
    {
        Assert.True(StockingPurchaseListAccess.CanEnter(new UserPermissionSummaryDto { IsSysAdmin = true }));
    }

    [Fact]
    public void PurchaseDept_WithPoRead_CanEnter()
    {
        var s = new UserPermissionSummaryDto
        {
            BelongsToPurchaseDept = true,
            PermissionCodes = new[] { "purchase-order.read" }
        };
        Assert.True(StockingPurchaseListAccess.CanEnter(s));
        Assert.True(StockingPurchaseListAccess.CanReadStockingPurchaseOrder(s, 2));
        Assert.False(StockingPurchaseListAccess.CanReadStockingPurchaseOrder(s, 1));
    }

    [Fact]
    public void PurchaseOpsRole_WithPoRead_CanEnter()
    {
        var s = new UserPermissionSummaryDto
        {
            RoleCodes = new[] { "purchase_ops_operator" },
            PermissionCodes = new[] { "purchase-order.read" }
        };
        Assert.True(StockingPurchaseListAccess.CanEnter(s));
    }

    [Fact]
    public void PurchaseAssistantIdentity_WithPoRead_CanEnter()
    {
        var s = new UserPermissionSummaryDto
        {
            IdentityType = 3,
            PermissionCodes = new[] { "purchase-order.read" }
        };
        Assert.True(StockingPurchaseListAccess.CanEnter(s));
    }

    [Fact]
    public void Logistics_WithPoRead_CannotEnter()
    {
        var s = new UserPermissionSummaryDto
        {
            IdentityType = 6,
            PermissionCodes = new[] { "purchase-order.read" }
        };
        Assert.False(StockingPurchaseListAccess.CanEnter(s));
    }

    [Fact]
    public void PurchaseDept_WithoutPoRead_CannotEnter()
    {
        var s = new UserPermissionSummaryDto { BelongsToPurchaseDept = true };
        Assert.False(StockingPurchaseListAccess.CanEnter(s));
    }

    [Fact]
    public void PurchaseScopeForbidden_CannotEnter()
    {
        var s = new UserPermissionSummaryDto
        {
            BelongsToPurchaseDept = true,
            PurchaseDataScope = 4,
            PermissionCodes = new[] { "purchase-order.read" }
        };
        Assert.False(StockingPurchaseListAccess.CanEnter(s));
    }
}
