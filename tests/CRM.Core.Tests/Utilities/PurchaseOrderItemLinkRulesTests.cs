using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class PurchaseOrderItemLinkRulesTests
{
    [Fact]
    public void ResolveHeaderType_AllManual_ReturnsStocking()
    {
        var t = PurchaseOrderItemLinkRules.ResolveHeaderType(1, new[] { (string?)null, "" });
        Assert.Equal(PurchaseOrderItemLinkRules.PurchaseOrderTypeStocking, t);
    }

    [Fact]
    public void ResolveHeaderType_AnyLinked_ReturnsCustomer()
    {
        var t = PurchaseOrderItemLinkRules.ResolveHeaderType(2, new[] { (string?)null, "so-line-1" });
        Assert.Equal(PurchaseOrderItemLinkRules.PurchaseOrderTypeCustomer, t);
    }

    [Fact]
    public void ValidateCustomerOrderItems_MixedLines_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PurchaseOrderItemLinkRules.ValidateCustomerOrderItems(
                1,
                new[] { (string?)"so-1", null }));
    }

    [Fact]
    public void ValidateCustomerOrderItems_AllLinked_Passes()
    {
        PurchaseOrderItemLinkRules.ValidateCustomerOrderItems(1, new[] { (string?)"so-1", "so-2" });
    }

    [Fact]
    public void ResolveInboundStockType_CustomerHeaderNoSell_FallsBackToStocking()
    {
        var t = PurchaseOrderItemLinkRules.ResolveInboundStockType(1, null, null);
        Assert.Equal(PurchaseOrderItemLinkRules.PurchaseOrderTypeStocking, t);
    }

    [Fact]
    public void ResolveInboundStockType_CustomerHeaderWithPoLineSell_StaysCustomer()
    {
        var t = PurchaseOrderItemLinkRules.ResolveInboundStockType(1, null, "so-1");
        Assert.Equal(PurchaseOrderItemLinkRules.PurchaseOrderTypeCustomer, t);
    }
}
