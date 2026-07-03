using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class SellOrderItemLinkRulesTests
{
    [Fact]
    public void IsLinkedQuoteId_RejectsEmptyAndSentinel()
    {
        Assert.False(SellOrderItemLinkRules.IsLinkedQuoteId(null));
        Assert.False(SellOrderItemLinkRules.IsLinkedQuoteId(""));
        Assert.False(SellOrderItemLinkRules.IsLinkedQuoteId(SellOrderItemLinkRules.EmptyQuoteSentinel));
        Assert.True(SellOrderItemLinkRules.IsLinkedQuoteId("quote-1"));
    }

    [Fact]
    public void ShouldAllowManualAddItem_Customer_Disallows()
    {
        Assert.False(SellOrderItemLinkRules.ShouldAllowManualAddItem(1));
        Assert.True(SellOrderItemLinkRules.ShouldAllowManualAddItem(2));
        Assert.True(SellOrderItemLinkRules.ShouldAllowManualAddItem(3));
    }

    [Fact]
    public void ValidateCustomerOrderItems_MixedLines_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            SellOrderItemLinkRules.ValidateCustomerOrderItems(
                1,
                new[] { (string?)"q-1", null }));
    }

    [Fact]
    public void ValidateCustomerOrderItems_AllLinked_Passes()
    {
        SellOrderItemLinkRules.ValidateCustomerOrderItems(1, new[] { (string?)"q-1", "q-2" });
    }

    [Fact]
    public void ValidateCustomerOrderItems_StockingWithoutQuote_Passes()
    {
        SellOrderItemLinkRules.ValidateCustomerOrderItems(2, new[] { (string?)null });
    }

    [Fact]
    public void ValidatePurchaseRequisitionAllowed_CustomerNoQuote_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SellOrderItemLinkRules.ValidatePurchaseRequisitionAllowed(1, null));
    }

    [Fact]
    public void ValidatePurchaseRequisitionAllowed_StockingNoQuote_Passes()
    {
        SellOrderItemLinkRules.ValidatePurchaseRequisitionAllowed(2, null);
    }
}
