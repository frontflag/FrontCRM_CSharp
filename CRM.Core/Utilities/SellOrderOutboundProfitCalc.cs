namespace CRM.Core.Utilities;

/// <summary>单条出库扩展行的批次成本（真实采购价 × 出库数量）。</summary>
public sealed class SellOrderOutboundCostLine
{
    public string? PurchaseOrderItemId { get; init; }
    public string? PurchaseOrderItemCode { get; init; }
    public decimal PurchasePriceUsd { get; init; }
    public int Qty { get; init; }
    public decimal ProfitOutBizUsd { get; init; }
}

/// <summary>销售明细出库利润汇总（实际批次成本优先，无快照时回退 PO 加权均价）。</summary>
public sealed class SellOrderOutboundProfitSnapshot
{
    public bool UseActualBatchCost { get; init; }
    public decimal OutboundCostUsd { get; init; }
    public decimal ProfitOutBizUsd { get; init; }
    public decimal ProfitOutRateBiz { get; init; }
    /// <summary>展示用：实际成本 ÷ 出库数量，或回退 PO 加权均价。</summary>
    public decimal EffectiveAvgCostUsd { get; init; }
    public IReadOnlyList<SellOrderOutboundCostLine> CostLines { get; init; } = Array.Empty<SellOrderOutboundCostLine>();
}

public static class SellOrderOutboundProfitCalc
{
    public static bool HasUsableActualBatchCost(IReadOnlyList<SellOrderOutboundCostLine> lines)
    {
        if (lines == null || lines.Count == 0)
            return false;
        if (lines.Sum(l => l.Qty) <= 0)
            return false;
        return lines.Any(l => l.PurchasePriceUsd > 0m || l.ProfitOutBizUsd != 0m);
    }

    public static SellOrderOutboundProfitSnapshot Compute(
        decimal outboundRevenueUsd,
        decimal qtyStockOutActual,
        IReadOnlyList<SellOrderOutboundCostLine> actualLines,
        decimal poWeightedAvgCostUsd)
    {
        if (qtyStockOutActual <= 0m)
        {
            return new SellOrderOutboundProfitSnapshot
            {
                UseActualBatchCost = false,
                OutboundCostUsd = 0m,
                ProfitOutBizUsd = 0m,
                ProfitOutRateBiz = 0m,
                EffectiveAvgCostUsd = poWeightedAvgCostUsd,
                CostLines = actualLines ?? Array.Empty<SellOrderOutboundCostLine>()
            };
        }

        var lines = actualLines ?? Array.Empty<SellOrderOutboundCostLine>();
        var useActual = HasUsableActualBatchCost(lines);
        decimal costUsd;
        decimal profitUsd;
        decimal effectiveAvg;

        if (useActual)
        {
            costUsd = Math.Round(lines.Sum(l => l.Qty * l.PurchasePriceUsd), 2, MidpointRounding.AwayFromZero);
            profitUsd = Math.Round(lines.Sum(l => l.ProfitOutBizUsd), 2, MidpointRounding.AwayFromZero);
            if (profitUsd == 0m && costUsd > 0m)
                profitUsd = Math.Round(outboundRevenueUsd - costUsd, 2, MidpointRounding.AwayFromZero);

            var sumQty = lines.Sum(l => l.Qty);
            effectiveAvg = sumQty > 0m
                ? Math.Round(costUsd / sumQty, 6, MidpointRounding.AwayFromZero)
                : 0m;
        }
        else
        {
            effectiveAvg = poWeightedAvgCostUsd;
            costUsd = Math.Round(qtyStockOutActual * poWeightedAvgCostUsd, 2, MidpointRounding.AwayFromZero);
            profitUsd = Math.Round(outboundRevenueUsd - costUsd, 2, MidpointRounding.AwayFromZero);
        }

        var rate = costUsd > 0m
            ? Math.Round(outboundRevenueUsd / costUsd, 6, MidpointRounding.AwayFromZero)
            : 0m;

        return new SellOrderOutboundProfitSnapshot
        {
            UseActualBatchCost = useActual,
            OutboundCostUsd = costUsd,
            ProfitOutBizUsd = profitUsd,
            ProfitOutRateBiz = rate,
            EffectiveAvgCostUsd = effectiveAvg,
            CostLines = GroupCostLinesForDisplay(lines)
        };
    }

    /// <summary>按 PO 明细 + 采购单价分组，供绩效公式展示。</summary>
    public static IReadOnlyList<SellOrderOutboundCostLine> GroupCostLinesForDisplay(
        IReadOnlyList<SellOrderOutboundCostLine> lines)
    {
        if (lines == null || lines.Count == 0)
            return Array.Empty<SellOrderOutboundCostLine>();

        return lines
            .GroupBy(l => new
            {
                PoId = (l.PurchaseOrderItemId ?? string.Empty).Trim(),
                PoCode = (l.PurchaseOrderItemCode ?? string.Empty).Trim(),
                l.PurchasePriceUsd
            })
            .Select(g => new SellOrderOutboundCostLine
            {
                PurchaseOrderItemId = string.IsNullOrEmpty(g.Key.PoId) ? null : g.Key.PoId,
                PurchaseOrderItemCode = string.IsNullOrEmpty(g.Key.PoCode) ? null : g.Key.PoCode,
                PurchasePriceUsd = g.Key.PurchasePriceUsd,
                Qty = g.Sum(x => x.Qty),
                ProfitOutBizUsd = Math.Round(g.Sum(x => x.ProfitOutBizUsd), 2, MidpointRounding.AwayFromZero)
            })
            .OrderBy(l => l.PurchaseOrderItemCode ?? l.PurchaseOrderItemId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.PurchasePriceUsd)
            .ToList();
    }
}
