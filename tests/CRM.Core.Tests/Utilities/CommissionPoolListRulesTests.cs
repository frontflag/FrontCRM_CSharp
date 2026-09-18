using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionPoolListRulesTests
{
    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-1, -5, 1, 20)]
    [InlineData(2, 50, 2, 50)]
    [InlineData(1, 500, 1, 200)]
    public void NormalizePage(int page, int pageSize, int expectedPage, int expectedSize)
    {
        var (p, s) = CommissionPoolListRules.NormalizePage(page, pageSize);
        Assert.Equal(expectedPage, p);
        Assert.Equal(expectedSize, s);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData((short)0, true)]
    [InlineData((short)1, true)]
    [InlineData((short)2, false)]
    public void SalesStatus(short? value, bool ok) =>
        Assert.Equal(ok, CommissionPoolListRules.IsSalesStatus(value));

    [Theory]
    [InlineData(null, true)]
    [InlineData((short)0, true)]
    [InlineData((short)1, true)]
    [InlineData((short)2, true)]
    [InlineData((short)3, false)]
    public void PurchaseStatus(short? value, bool ok) =>
        Assert.Equal(ok, CommissionPoolListRules.IsPurchaseStatus(value));

    [Theory]
    [InlineData(null, true)]
    [InlineData((short)0, true)]
    [InlineData((short)1, true)]
    [InlineData((short)2, true)]
    [InlineData((short)3, false)]
    public void ReceiptStatus(short? value, bool ok) =>
        Assert.Equal(ok, CommissionPoolListRules.IsReceiptStatus(value));

    [Fact]
    public void NormalizeKeyword_and_display_name()
    {
        Assert.Null(CommissionPoolListRules.NormalizeKeyword("  "));
        Assert.Equal("SO1", CommissionPoolListRules.NormalizeKeyword(" SO1 "));
        Assert.Equal("u1", CommissionPoolListRules.NormalizeUserId(" u1 "));
        Assert.Null(CommissionPoolListRules.NormalizeUserId("  "));
        Assert.Equal("zhang", CommissionPoolListRules.DisplayAccount("  zhang  ", "u1"));
        Assert.Equal("u1", CommissionPoolListRules.DisplayAccount(" ", "u1"));
        Assert.Equal("u1", CommissionPoolListRules.DisplayAccount(null, "u1"));
        Assert.Equal("so-name", CommissionPoolListRules.FirstNonEmpty(null, "  so-name  ", "other"));
        Assert.Null(CommissionPoolListRules.FirstNonEmpty(" ", null));
        Assert.Equal("STO1-2", CommissionPoolListRules.DisplayDocCode(" STO1-2 ", "STO1"));
        Assert.Equal("STO1", CommissionPoolListRules.DisplayDocCode(" ", "STO1"));
        Assert.Equal("", CommissionPoolListRules.DisplayDocCode(null, " "));
        Assert.True(CommissionPoolListRules.ContainsKeyword("sto1-2", "STO1", "STO1-2"));
        Assert.False(CommissionPoolListRules.ContainsKeyword("PO9-1", "STO1", "SO1-1"));
    }

    [Fact]
    public void ResolveLineFacts_prefers_stock_out_then_sell_fallback()
    {
        var facts = CommissionPoolListRules.ResolveLineFacts(
            "PN-SO",
            "Brand-SO",
            hasItem: true,
            itemQty: 3,
            hasExtend: true,
            extQty: 5,
            extPurchasePrice: 10m,
            extPurchaseCurrency: 1,
            extPurchasePriceUsd: 1.4m,
            extSalesPrice: 20m,
            extSalesCurrency: 2,
            extSalesPriceUsd: 20m,
            sellPn: "PN-SELL",
            sellBrand: "Brand-SELL",
            sellPrice: 99m,
            sellCurrency: 1,
            sellConvertPrice: 88m);
        Assert.Equal("PN-SO", facts.PurchasePn);
        Assert.Equal("Brand-SO", facts.PurchaseBrand);
        Assert.Equal(10m, facts.PurchasePrice);
        Assert.Equal((short)1, facts.PurchaseCurrency);
        Assert.Equal(1.4m, facts.PurchasePriceUsd);
        Assert.Equal(20m, facts.SalesPrice);
        Assert.Equal((short)2, facts.SalesCurrency);
        Assert.Equal(20m, facts.SalesPriceUsd);
        Assert.Equal(5, facts.QtyStockOut);
    }

    [Fact]
    public void ResolveLineFacts_qty_and_sales_fallback_when_extend_zero_or_null()
    {
        var facts = CommissionPoolListRules.ResolveLineFacts(
            null,
            null,
            hasItem: true,
            itemQty: 8,
            hasExtend: true,
            extQty: 0,
            extPurchasePrice: 0m,
            extPurchaseCurrency: 1,
            extPurchasePriceUsd: 0m,
            extSalesPrice: null,
            extSalesCurrency: null,
            extSalesPriceUsd: null,
            sellPn: "PN-SELL",
            sellBrand: "Brand-SELL",
            sellPrice: 12.5m,
            sellCurrency: 1,
            sellConvertPrice: 1.8m);
        Assert.Equal("PN-SELL", facts.PurchasePn);
        Assert.Equal("Brand-SELL", facts.PurchaseBrand);
        Assert.Equal(0m, facts.PurchasePrice);
        Assert.Equal(12.5m, facts.SalesPrice);
        Assert.Equal((short)1, facts.SalesCurrency);
        Assert.Equal(1.8m, facts.SalesPriceUsd);
        Assert.Equal(8, facts.QtyStockOut);
    }

    [Fact]
    public void ResolveLineFacts_no_extend_hides_purchase_prices()
    {
        var facts = CommissionPoolListRules.ResolveLineFacts(
            "PN",
            "BR",
            hasItem: true,
            itemQty: 2,
            hasExtend: false,
            extQty: 0,
            extPurchasePrice: 0m,
            extPurchaseCurrency: 1,
            extPurchasePriceUsd: 0m,
            extSalesPrice: null,
            extSalesCurrency: null,
            extSalesPriceUsd: null,
            sellPn: null,
            sellBrand: null,
            sellPrice: 9m,
            sellCurrency: 2,
            sellConvertPrice: 9m);
        Assert.Null(facts.PurchasePrice);
        Assert.Null(facts.PurchaseCurrency);
        Assert.Null(facts.PurchasePriceUsd);
        Assert.Equal(9m, facts.SalesPrice);
        Assert.Equal(2, facts.QtyStockOut);
    }
}
