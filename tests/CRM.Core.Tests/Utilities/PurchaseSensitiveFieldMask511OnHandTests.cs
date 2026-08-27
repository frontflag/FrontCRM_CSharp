using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class PurchaseSensitiveFieldMask511OnHandTests
{
    [Fact]
    public void ApplyInventoryOnHandSummaryRows_ZerosAmountsWhenMasked()
    {
        var rows = new List<InventoryOnHandSummaryRowDto>
        {
            new()
            {
                Amounts = new List<InventoryOnHandAmountDto>
                {
                    new() { Currency = 1, Amount = 12.3m },
                    new() { Currency = 2, Amount = 4m }
                }
            }
        };
        PurchaseSensitiveFieldMask511.ApplyInventoryOnHandSummaryRows(rows, true);
        Assert.All(rows[0].Amounts, a => Assert.Equal(0m, a.Amount));
    }

    [Fact]
    public void ApplyInventoryOnHandSummaryAmounts_ZerosTotalsWhenMasked()
    {
        var totals = new List<InventoryOnHandAmountDto>
        {
            new() { Currency = 1, Amount = 100m },
            new() { Currency = 2, Amount = 8.5m }
        };
        PurchaseSensitiveFieldMask511.ApplyInventoryOnHandSummaryAmounts(totals, true);
        Assert.All(totals, a => Assert.Equal(0m, a.Amount));
    }
}
