using CRM.Core.Constants;
using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomsInvoiceReportPriceRulesTests
{
    [Fact]
    public void IsCustomsPacking_OnlyType20()
    {
        Assert.True(CustomsInvoiceReportPriceRules.IsCustomsPacking(StockOutTypeCode.Customs));
        Assert.False(CustomsInvoiceReportPriceRules.IsCustomsPacking(StockOutTypeCode.Sales));
    }

    [Fact]
    public void Ready_DoesNotThrow()
    {
        CustomsInvoiceReportPriceRules.EnsureDeclarationReadyForInvoice(
            declarationExists: true,
            feesCalculatedAt: DateTime.UtcNow,
            costUsdByLine: new[] { 1.2m, 3.4m });
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void NotReady_ThrowsFeesRequired(bool exists, bool feesSet, bool hasPositiveCost)
    {
        DateTime? fees = feesSet ? DateTime.UtcNow : null;
        decimal[] costs = hasPositiveCost ? new[] { 1.2m } : Array.Empty<decimal>();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomsInvoiceReportPriceRules.EnsureDeclarationReadyForInvoice(exists, fees, costs));
        Assert.Equal(CustomsInvoiceReportPriceRules.FeesRequiredForInvoiceMessage, ex.Message);
    }

    [Fact]
    public void ZeroCostUsd_ThrowsFeesRequired()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomsInvoiceReportPriceRules.EnsureDeclarationReadyForInvoice(
                true, DateTime.UtcNow, new[] { 2m, 0m }));
        Assert.Equal(CustomsInvoiceReportPriceRules.FeesRequiredForInvoiceMessage, ex.Message);
    }
}
