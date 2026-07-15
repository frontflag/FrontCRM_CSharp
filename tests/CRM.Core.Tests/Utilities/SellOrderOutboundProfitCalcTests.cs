using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class SellOrderOutboundProfitCalcTests
{
    [Fact]
    public void Compute_UsesActualBatchCost_WhenExtendLinesExist()
    {
        var lines = new[]
        {
            new SellOrderOutboundCostLine
            {
                PurchaseOrderItemCode = "PO-1",
                PurchasePriceUsd = 0.30m,
                Qty = 100,
                ProfitOutBizUsd = 9m
            },
            new SellOrderOutboundCostLine
            {
                PurchaseOrderItemCode = "PO-2",
                PurchasePriceUsd = 0.35m,
                Qty = 200,
                ProfitOutBizUsd = 10m
            }
        };

        var snapshot = SellOrderOutboundProfitCalc.Compute(
            outboundRevenueUsd: 120m,
            qtyStockOutActual: 300m,
            actualLines: lines,
            poWeightedAvgCostUsd: 0.32m);

        Assert.True(snapshot.UseActualBatchCost);
        Assert.Equal(100m, snapshot.OutboundCostUsd);
        Assert.Equal(19m, snapshot.ProfitOutBizUsd);
        Assert.Equal(1.2m, snapshot.ProfitOutRateBiz);
        Assert.Equal(0.333333m, snapshot.EffectiveAvgCostUsd);
        Assert.Equal(2, snapshot.CostLines.Count);
    }

    [Fact]
    public void Compute_FallsBackToPoWeightedAverage_WhenNoActualLines()
    {
        var snapshot = SellOrderOutboundProfitCalc.Compute(
            outboundRevenueUsd: 120m,
            qtyStockOutActual: 300m,
            actualLines: Array.Empty<SellOrderOutboundCostLine>(),
            poWeightedAvgCostUsd: 0.32m);

        Assert.False(snapshot.UseActualBatchCost);
        Assert.Equal(96m, snapshot.OutboundCostUsd);
        Assert.Equal(24m, snapshot.ProfitOutBizUsd);
        Assert.Equal(1.25m, snapshot.ProfitOutRateBiz);
        Assert.Equal(0.32m, snapshot.EffectiveAvgCostUsd);
    }

    [Fact]
    public void OrderCostDetailsForDisplay_SortsByStockOutThenPoThenPrice()
    {
        var lines = new[]
        {
            new SellOrderOutboundCostDetailLine
            {
                StockOutCode = "CK-B",
                PurchaseOrderItemCode = "PO-2",
                PurchasePriceUsd = 0.35m,
                Qty = 10,
                CostUsd = 3.5m
            },
            new SellOrderOutboundCostDetailLine
            {
                StockOutCode = "CK-A",
                PurchaseOrderItemCode = "PO-2",
                PurchasePriceUsd = 0.30m,
                Qty = 20,
                CostUsd = 6m
            },
            new SellOrderOutboundCostDetailLine
            {
                StockOutCode = "CK-A",
                PurchaseOrderItemCode = "PO-1",
                PurchasePriceUsd = 0.32m,
                Qty = 5,
                CostUsd = 1.6m
            }
        };

        var ordered = SellOrderOutboundProfitCalc.OrderCostDetailsForDisplay(lines);

        Assert.Equal(3, ordered.Count);
        Assert.Equal("CK-A", ordered[0].StockOutCode);
        Assert.Equal("PO-1", ordered[0].PurchaseOrderItemCode);
        Assert.Equal("CK-A", ordered[1].StockOutCode);
        Assert.Equal("PO-2", ordered[1].PurchaseOrderItemCode);
        Assert.Equal(0.30m, ordered[1].PurchasePriceUsd);
        Assert.Equal("CK-B", ordered[2].StockOutCode);
    }
}
