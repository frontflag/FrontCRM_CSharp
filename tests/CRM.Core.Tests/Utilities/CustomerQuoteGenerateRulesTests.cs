using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

/// <summary>对照 QA：CQ-DRAFT-LST-006 / CQ-DRAFT-LST-009。</summary>
public class CustomerQuoteGenerateRulesTests
{
    [Fact]
    public void SameCustomerAndSales_AllowsGenerate()
    {
        CustomerQuoteGenerateRules.AssertDraftsSameCustomerAndSalesUser(
            new[] { "c1", "c1" },
            new[] { "s1", "s1" });
    }

    [Fact]
    public void EmptyIds_DoNotConflict()
    {
        CustomerQuoteGenerateRules.AssertDraftsSameCustomerAndSalesUser(
            new[] { "c1", null, "" },
            new[] { null, "s1", "  " });
        Assert.Equal("c1", CustomerQuoteGenerateRules.FirstNonEmpty(new[] { null, "c1" }));
        Assert.Equal("s1", CustomerQuoteGenerateRules.FirstNonEmpty(new[] { "", "s1" }));
    }

    [Fact]
    public void DifferentCustomer_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteGenerateRules.AssertDraftsSameCustomerAndSalesUser(
                new[] { "c1", "c2" },
                new[] { "s1", "s1" }));
        Assert.Equal(CustomerQuoteGenerateRules.DifferentCustomerMessage, ex.Message);
    }

    [Fact]
    public void DifferentSalesUser_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteGenerateRules.AssertDraftsSameCustomerAndSalesUser(
                new[] { "c1", "c1" },
                new[] { "s1", "s2" }));
        Assert.Equal(CustomerQuoteGenerateRules.DifferentSalesUserMessage, ex.Message);
    }

    [Fact]
    public void LineMatchesHeader_CompatibleWhenEitherEmpty()
    {
        CustomerQuoteGenerateRules.AssertLineMatchesHeader("c1", "s1", null, "s1");
        CustomerQuoteGenerateRules.AssertLineMatchesHeader("c1", "s1", "c1", null);
    }

    [Fact]
    public void LineDifferentCustomer_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteGenerateRules.AssertLineMatchesHeader("c1", "s1", "c2", "s1"));
        Assert.Equal(CustomerQuoteGenerateRules.LineDifferentCustomerMessage, ex.Message);
    }

    [Fact]
    public void LineDifferentSalesUser_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteGenerateRules.AssertLineMatchesHeader("c1", "s1", "c1", "s2"));
        Assert.Equal(CustomerQuoteGenerateRules.LineDifferentSalesUserMessage, ex.Message);
    }

    [Theory]
    [InlineData("CQ00001", 1, "CQ00001")]
    [InlineData("CQ00001", 0, "CQ00001")]
    [InlineData("CQ00001", 2, "CQ00001-2")]
    [InlineData(" CQ00001 ", 3, "CQ00001-3")]
    [InlineData("", 2, "")]
    public void FormatDisplayCode_VersionOneHasNoSuffix(string code, int version, string expected)
    {
        Assert.Equal(expected, CustomerQuoteGenerateRules.FormatDisplayCode(code, version));
    }
}
