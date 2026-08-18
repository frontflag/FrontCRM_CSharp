using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class FinanceReceivableVoidRulesTests
{
    [Fact]
    public void MissingStockOut_IsOrphan()
    {
        Assert.True(FinanceReceivableVoidRules.IsOrphanStockOut(null));
    }

    [Fact]
    public void SoftDeletedStockOut_IsOrphan()
    {
        Assert.True(FinanceReceivableVoidRules.IsOrphanStockOut(new StockOut { IsDeleted = true }));
    }

    [Fact]
    public void LiveStockOut_IsNotOrphan()
    {
        Assert.False(FinanceReceivableVoidRules.IsOrphanStockOut(new StockOut { IsDeleted = false }));
    }

    [Fact]
    public void DetailVoid_LiveStockOut_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            FinanceReceivableVoidRules.AssertOrphanStockOutForDetailVoid(new StockOut { IsDeleted = false }));
        Assert.Equal(FinanceReceivableVoidRules.StockOutStillValidMessage, ex.Message);
    }

    [Fact]
    public void DetailVoid_Orphan_Allows()
    {
        FinanceReceivableVoidRules.AssertOrphanStockOutForDetailVoid(null);
        FinanceReceivableVoidRules.AssertOrphanStockOutForDetailVoid(new StockOut { IsDeleted = true });
    }
}
