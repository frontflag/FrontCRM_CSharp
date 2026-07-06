using CRM.Core.Models.Purchase;

namespace CRM.Core.Utilities;

/// <summary>
/// 采购申请与采购订单明细关联：显式 <c>purchase_requisition_id</c> + 历史未回填数据的 FIFO 兜底。
/// </summary>
public static class PurchaseRequisitionPoLinkHelper
{
    public static bool IsActivePoItem(short status) => status != -1 && status != -2;

    public static IReadOnlyList<PurchaseRequisition> OrderPrsOnSellLine(IEnumerable<PurchaseRequisition> prs) =>
        prs.Where(p => !p.IsDeleted)
            .OrderBy(p => p.CreateTime)
            .ThenBy(p => p.Id, StringComparer.Ordinal)
            .ToList();

    /// <summary>将同一销售行上未关联 PR 的 PO 数量按 PR 创建时间 FIFO 分摊。</summary>
    public static Dictionary<string, decimal> AllocateLegacyUnlinkedQtyFifo(
        IReadOnlyList<PurchaseRequisition> prsOrdered,
        decimal unlinkedQty)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (unlinkedQty <= 0 || prsOrdered.Count == 0)
            return result;

        var remaining = unlinkedQty;
        foreach (var pr in prsOrdered)
        {
            if (remaining <= 0)
                break;
            var take = Math.Min(pr.Qty, remaining);
            if (take > 0)
                result[pr.Id] = take;
            remaining -= take;
        }

        return result;
    }

    /// <summary>计算指定 PR 的有效关联 PO 数量（显式关联 + FIFO 兜底）。</summary>
    public static decimal ComputeLinkedQtyForRequisition(
        PurchaseRequisition pr,
        IReadOnlyList<PurchaseRequisition> prsOrdered,
        IEnumerable<PurchaseOrderItem> explicitItemsOnPr,
        IEnumerable<PurchaseOrderItem> unlinkedActiveOnSellLine)
    {
        var explicitQty = explicitItemsOnPr
            .Where(i => string.Equals(i.PurchaseRequisitionId, pr.Id, StringComparison.OrdinalIgnoreCase))
            .Where(i => IsActivePoItem(i.Status))
            .Sum(i => i.Qty);

        var unlinkedTotal = unlinkedActiveOnSellLine
            .Where(i => IsActivePoItem(i.Status))
            .Sum(i => i.Qty);
        if (unlinkedTotal <= 0)
            return explicitQty;

        var legacyAlloc = AllocateLegacyUnlinkedQtyFifo(prsOrdered, unlinkedTotal);
        legacyAlloc.TryGetValue(pr.Id, out var legacyQty);
        return explicitQty + legacyQty;
    }

    /// <summary>历史未回填 PO 行：整行按 FIFO 归属到某一 PR（用于详情展示与删除校验）。</summary>
    public static HashSet<string> GetLegacyPoItemIdsForRequisition(
        string requisitionId,
        IReadOnlyList<PurchaseRequisition> prsOrdered,
        IEnumerable<PurchaseOrderItem> unlinkedItems)
    {
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (prsOrdered.Count == 0)
            return assigned;

        var targetId = requisitionId.Trim();
        var prRemaining = prsOrdered.ToDictionary(p => p.Id, p => p.Qty, StringComparer.OrdinalIgnoreCase);

        foreach (var poItem in unlinkedItems
                     .Where(i => IsActivePoItem(i.Status))
                     .OrderBy(i => i.PurchaseOrderItemCode, StringComparer.Ordinal))
        {
            foreach (var pr in prsOrdered)
            {
                if (!prRemaining.TryGetValue(pr.Id, out var need) || need <= 0)
                    continue;

                prRemaining[pr.Id] = need - poItem.Qty;
                if (string.Equals(pr.Id, targetId, StringComparison.OrdinalIgnoreCase))
                    assigned.Add(poItem.Id);
                break;
            }
        }

        return assigned;
    }
}
