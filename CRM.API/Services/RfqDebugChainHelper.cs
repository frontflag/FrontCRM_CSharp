using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;
using CRM.Core.Models.RFQ;
using CRM.Core.Models.Sales;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.API.Services;

/// <summary>
/// Debug：按 RFQ 需求单号收集下游单据并支持级联删除（与 simulate-business-chain 造数链路一致）。
/// </summary>
public static class RfqDebugChainHelper
{
    private static string ShortId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;
        return id.Length <= 8 ? id : id[..8] + "…";
    }

    public sealed class ChainNodeDto
    {
        public string Node { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
    }

    public sealed class ChainSnapshot
    {
        public RFQ Rfq { get; init; } = null!;
        public List<RFQItem> RfqItems { get; init; } = new();
        public List<Quote> Quotes { get; init; } = new();
        public List<QuoteItem> QuoteItems { get; init; } = new();
        public List<SellOrder> SellOrders { get; init; } = new();
        public List<SellOrderItem> SellOrderItems { get; init; } = new();
        public List<PurchaseRequisition> PurchaseRequisitions { get; init; } = new();
        public List<PurchaseOrder> PurchaseOrders { get; init; } = new();
        public List<PurchaseOrderItem> PurchaseOrderItems { get; init; } = new();
        public List<StockInNotify> StockInNotifies { get; init; } = new();
        public List<QCInfo> QCInfos { get; init; } = new();
        public List<QCItem> QCItems { get; init; } = new();
        public List<StockIn> StockIns { get; init; } = new();
        public List<StockInExtend> StockInExtends { get; init; } = new();
        public List<StockInBatch> StockInBatches { get; init; } = new();
        public List<StockInItem> StockInItems { get; init; } = new();
        public List<StockInItemExtend> StockInItemExtends { get; init; } = new();
        public List<StockItem> StockItems { get; init; } = new();
        public List<StockInfo> StockAggregates { get; init; } = new();
        public List<StockExtend> StockExtends { get; init; } = new();
        public List<StockOutRequest> StockOutRequests { get; init; } = new();
        public List<PickingTask> PickingTasks { get; init; } = new();
        public List<PickingTaskItem> PickingTaskItems { get; init; } = new();
        public List<StockOut> StockOuts { get; init; } = new();
        public List<StockOutItem> StockOutItems { get; init; } = new();
        public List<StockOutItemExtend> StockOutItemExtends { get; init; } = new();
        public List<SellOrderItemExtend> SellOrderItemExtends { get; init; } = new();
        public List<SellOrderExtend> SellOrderExtends { get; init; } = new();

        public List<ChainNodeDto> ToDisplayNodes()
        {
            var list = new List<ChainNodeDto>();
            void Add(string node, string code, string id)
            {
                if (string.IsNullOrWhiteSpace(id)) return;
                list.Add(new ChainNodeDto { Node = node, Code = code, Id = id });
            }

            Add("RFQ", Rfq.RfqCode, Rfq.Id);
            foreach (var it in RfqItems.OrderBy(x => x.LineNo))
                Add("RFQItem", $"Line{it.LineNo}", it.Id);
            foreach (var q in Quotes)
                Add("Quote", q.QuoteCode, q.Id);
            foreach (var qi in QuoteItems)
                Add("QuoteItem", ShortId(qi.Id), qi.Id);
            foreach (var so in SellOrders)
                Add("SellOrder", so.SellOrderCode, so.Id);
            foreach (var li in SellOrderItems)
                Add("SellOrderItem", li.SellOrderItemCode, li.Id);
            foreach (var pr in PurchaseRequisitions)
                Add("PurchaseRequisition", pr.BillCode, pr.Id);
            foreach (var po in PurchaseOrders)
                Add("PurchaseOrder", po.PurchaseOrderCode, po.Id);
            foreach (var pi in PurchaseOrderItems)
                Add("PurchaseOrderItem", pi.PurchaseOrderItemCode ?? pi.Id, pi.Id);
            foreach (var n in StockInNotifies)
                Add("StockInNotify", n.NoticeCode, n.Id);
            foreach (var qc in QCInfos)
                Add("QC", qc.QcCode, qc.Id);
            foreach (var qci in QCItems)
                Add("QCItem", ShortId(qci.Id), qci.Id);
            foreach (var si in StockIns)
                Add("StockIn", si.StockInCode, si.Id);
            foreach (var sie in StockInExtends)
                Add("StockInExtend", ShortId(sie.StockInId), sie.StockInId);
            foreach (var b in StockInBatches)
                Add("StockInBatch", ShortId(b.Id), b.Id);
            foreach (var sii in StockInItems)
                Add("StockInItem", sii.StockInItemCode ?? sii.Id, sii.Id);
            foreach (var ext in StockInItemExtends)
                Add("StockInItemExtend", ShortId(ext.Id), ext.Id);
            foreach (var st in StockItems)
                Add("StockItem", st.StockItemCode ?? st.Id, st.Id);
            foreach (var agg in StockAggregates)
                Add("Stock", agg.StockCode ?? agg.Id, agg.Id);
            foreach (var se in StockExtends)
                Add("StockExtend", ShortId(se.StockId), se.StockId);
            foreach (var sor in StockOutRequests)
                Add("StockOutRequest", sor.RequestCode, sor.Id);
            foreach (var pt in PickingTasks)
                Add("PickingTask", pt.TaskCode, pt.Id);
            foreach (var pti in PickingTaskItems)
                Add("PickingTaskItem", ShortId(pti.Id), pti.Id);
            foreach (var so in StockOuts)
                Add("StockOut", so.StockOutCode, so.Id);
            foreach (var soi in StockOutItems)
                Add("StockOutItem", ShortId(soi.Id), soi.Id);
            foreach (var soe in StockOutItemExtends)
                Add("StockOutItemExtend", ShortId(soe.Id), soe.Id);
            foreach (var e in SellOrderItemExtends)
                Add("SellOrderItemExtend", ShortId(e.Id), e.Id);
            foreach (var e in SellOrderExtends)
                Add("SellOrderExtend", ShortId(e.SellOrderId), e.SellOrderId);
            return list;
        }
    }

    public static async Task<ChainSnapshot?> LoadChainAsync(ApplicationDbContext db, string rfqCode, CancellationToken ct = default)
    {
        var code = (rfqCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
            return null;

        var rfq = await db.RFQs.AsNoTracking().FirstOrDefaultAsync(r => r.RfqCode == code, ct);
        if (rfq == null)
            return null;

        var rfqItems = await db.RFQItems.AsNoTracking().Where(i => i.RfqId == rfq.Id).ToListAsync(ct);
        var rfqItemIds = rfqItems.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var quotes = await db.Quotes.AsNoTracking()
            .Where(q => q.RFQId == rfq.Id || (q.RFQItemId != null && rfqItemIds.Contains(q.RFQItemId)))
            .ToListAsync(ct);
        var quoteIds = quotes.Select(q => q.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var quoteItems = quoteIds.Count == 0
            ? new List<QuoteItem>()
            : await db.QuoteItems.AsNoTracking().Where(qi => quoteIds.Contains(qi.QuoteId)).ToListAsync(ct);

        var sellOrderItems = await db.SellOrderItems.AsNoTracking()
            .Where(li => li.QuoteId != null && quoteIds.Contains(li.QuoteId))
            .ToListAsync(ct);
        var soItemIds = sellOrderItems.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var soIds = sellOrderItems.Select(x => x.SellOrderId).Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sellOrders = soIds.Count == 0
            ? new List<SellOrder>()
            : await db.SellOrders.AsNoTracking().Where(s => soIds.Contains(s.Id)).ToListAsync(ct);

        var prs = soItemIds.Count == 0
            ? new List<PurchaseRequisition>()
            : await db.PurchaseRequisitions.AsNoTracking().Where(p => soItemIds.Contains(p.SellOrderItemId)).ToListAsync(ct);

        var poItems = soItemIds.Count == 0
            ? new List<PurchaseOrderItem>()
            : await db.PurchaseOrderItems.AsNoTracking()
                .Where(p => p.SellOrderItemId != null && soItemIds.Contains(p.SellOrderItemId))
                .ToListAsync(ct);
        var poIds = poItems.Select(p => p.PurchaseOrderId).Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pos = poIds.Count == 0
            ? new List<PurchaseOrder>()
            : await db.PurchaseOrders.AsNoTracking().Where(p => poIds.Contains(p.Id)).ToListAsync(ct);

        var poItemIds = poItems.Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var notifies = await db.StockInNotifies.AsNoTracking()
            .Where(n => poIds.Contains(n.PurchaseOrderId) || poItemIds.Contains(n.PurchaseOrderItemId))
            .ToListAsync(ct);
        var notifyIds = notifies.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var qcs = notifyIds.Count == 0
            ? new List<QCInfo>()
            : await db.QCInfos.AsNoTracking().Where(q => notifyIds.Contains(q.StockInNotifyId)).ToListAsync(ct);
        var qcIds = qcs.Select(q => q.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var qcItems = qcIds.Count == 0
            ? new List<QCItem>()
            : await db.QCItems.AsNoTracking().Where(x => qcIds.Contains(x.QcInfoId)).ToListAsync(ct);

        var stockIns = await db.StockIns.AsNoTracking()
            .Where(s =>
                (s.SourceId != null && notifyIds.Contains(s.SourceId))
                || (s.QcId != null && qcIds.Contains(s.QcId)))
            .ToListAsync(ct);
        var stockInIds = stockIns.Select(s => s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stockInExtends = stockInIds.Count == 0
            ? new List<StockInExtend>()
            : await db.StockInExtends.AsNoTracking().Where(e => stockInIds.Contains(e.StockInId)).ToListAsync(ct);

        var stockInItems = stockInIds.Count == 0
            ? new List<StockInItem>()
            : await db.StockInItems.AsNoTracking().Where(i => stockInIds.Contains(i.StockInId)).ToListAsync(ct);
        var stockInItemIds = stockInItems.Select(i => i.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var batches = stockInItemIds.Count == 0
            ? new List<StockInBatch>()
            : await db.StockInBatches.AsNoTracking().Where(b => stockInItemIds.Contains(b.StockInItemId)).ToListAsync(ct);

        var siExtends = stockInItemIds.Count == 0
            ? new List<StockInItemExtend>()
            : await db.StockInItemExtends.AsNoTracking().Where(e => stockInItemIds.Contains(e.Id)).ToListAsync(ct);

        var stockItems = stockInItemIds.Count == 0
            ? new List<StockItem>()
            : await db.StockItems.AsNoTracking().Where(si => stockInItemIds.Contains(si.StockInItemId)).ToListAsync(ct);
        var aggIds = stockItems.Select(s => s.StockAggregateId).Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var aggs = aggIds.Count == 0
            ? new List<StockInfo>()
            : await db.Stocks.AsNoTracking().Where(s => aggIds.Contains(s.Id)).ToListAsync(ct);

        var stockExtends = aggIds.Count == 0
            ? new List<StockExtend>()
            : await db.StockExtends.AsNoTracking().Where(e => aggIds.Contains(e.StockId)).ToListAsync(ct);

        var sors = soItemIds.Count == 0
            ? new List<StockOutRequest>()
            : await db.StockOutRequests.AsNoTracking().Where(r => soItemIds.Contains(r.SalesOrderItemId)).ToListAsync(ct);
        var sorIds = sors.Select(s => s.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pickingTasks = sorIds.Count == 0
            ? new List<PickingTask>()
            : await db.PickingTasks.AsNoTracking().Where(t => sorIds.Contains(t.StockOutRequestId)).ToListAsync(ct);
        var pickingIds = pickingTasks.Select(t => t.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pickingItems = pickingIds.Count == 0
            ? new List<PickingTaskItem>()
            : await db.PickingTaskItems.AsNoTracking().Where(i => pickingIds.Contains(i.PickingTaskId)).ToListAsync(ct);

        var stockOuts = await db.StockOuts.AsNoTracking()
            .Where(o =>
                (o.SourceId != null && sorIds.Contains(o.SourceId))
                || (o.SellOrderItemId != null && soItemIds.Contains(o.SellOrderItemId)))
            .ToListAsync(ct);
        var stockOutIds = stockOuts.Select(o => o.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var stockOutItems = stockOutIds.Count == 0
            ? new List<StockOutItem>()
            : await db.StockOutItems.AsNoTracking().Where(i => stockOutIds.Contains(i.StockOutId)).ToListAsync(ct);
        var stockOutItemIds = stockOutItems.Select(i => i.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var soItemExtends = soItemIds.Count == 0
            ? new List<SellOrderItemExtend>()
            : await db.SellOrderItemExtends.AsNoTracking().Where(e => soItemIds.Contains(e.Id)).ToListAsync(ct);

        var soExtends = soIds.Count == 0
            ? new List<SellOrderExtend>()
            : await db.SellOrderExtends.AsNoTracking().Where(e => soIds.Contains(e.SellOrderId)).ToListAsync(ct);

        var outItemExtends = stockOutItemIds.Count == 0
            ? new List<StockOutItemExtend>()
            : await db.StockOutItemExtends.AsNoTracking().Where(e => stockOutItemIds.Contains(e.Id)).ToListAsync(ct);

        return new ChainSnapshot
        {
            Rfq = rfq,
            RfqItems = rfqItems,
            Quotes = quotes,
            QuoteItems = quoteItems,
            SellOrders = sellOrders,
            SellOrderItems = sellOrderItems,
            PurchaseRequisitions = prs,
            PurchaseOrders = pos,
            PurchaseOrderItems = poItems,
            StockInNotifies = notifies,
            QCInfos = qcs,
            QCItems = qcItems,
            StockIns = stockIns,
            StockInExtends = stockInExtends,
            StockInBatches = batches,
            StockInItems = stockInItems,
            StockInItemExtends = siExtends,
            StockItems = stockItems,
            StockAggregates = aggs,
            StockExtends = stockExtends,
            StockOutRequests = sors,
            PickingTasks = pickingTasks,
            PickingTaskItems = pickingItems,
            StockOuts = stockOuts,
            StockOutItems = stockOutItems,
            StockOutItemExtends = outItemExtends,
            SellOrderItemExtends = soItemExtends,
            SellOrderExtends = soExtends
        };
    }

    /// <summary>
    /// 按外键逆序删除；调用方需自行开启事务。
    /// </summary>
    public static async Task DeleteChainAsync(ApplicationDbContext db, ChainSnapshot snap, CancellationToken ct = default)
    {
        var sorIds = snap.StockOutRequests.Select(s => s.Id).ToList();
        if (sorIds.Count > 0)
        {
            var ptIds = await db.PickingTasks.AsNoTracking()
                .Where(t => sorIds.Contains(t.StockOutRequestId))
                .Select(t => t.Id)
                .ToListAsync(ct);
            if (ptIds.Count > 0)
            {
                await db.PickingTaskItems.Where(i => ptIds.Contains(i.PickingTaskId)).ExecuteDeleteAsync(ct);
                await db.PickingTasks.Where(t => ptIds.Contains(t.Id)).ExecuteDeleteAsync(ct);
            }
        }

        var stockOutIds = snap.StockOuts.Select(s => s.Id).ToList();
        if (stockOutIds.Count > 0)
        {
            var soiIds = await db.StockOutItems.AsNoTracking()
                .Where(i => stockOutIds.Contains(i.StockOutId))
                .Select(i => i.Id)
                .ToListAsync(ct);
            if (soiIds.Count > 0)
            {
                await db.StockOutItemExtends.Where(e => soiIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
                await db.StockOutItems.Where(i => soiIds.Contains(i.Id)).ExecuteDeleteAsync(ct);
            }

            await db.StockOuts.Where(s => stockOutIds.Contains(s.Id)).ExecuteDeleteAsync(ct);
        }

        if (sorIds.Count > 0)
            await db.StockOutRequests.Where(r => sorIds.Contains(r.Id)).ExecuteDeleteAsync(ct);

        var stIds = snap.StockItems.Select(s => s.Id).ToList();
        if (stIds.Count > 0)
            await db.StockItems.Where(si => stIds.Contains(si.Id)).ExecuteDeleteAsync(ct);

        foreach (var agg in snap.StockAggregates)
        {
            var left = await db.StockItems.CountAsync(si => si.StockAggregateId == agg.Id, ct);
            if (left == 0)
            {
                await db.StockExtends.Where(e => e.StockId == agg.Id).ExecuteDeleteAsync(ct);
                await db.Stocks.Where(s => s.Id == agg.Id).ExecuteDeleteAsync(ct);
            }
        }

        var stockInItemIds = snap.StockInItems.Select(i => i.Id).ToList();
        if (stockInItemIds.Count > 0)
        {
            await db.StockInBatches.Where(b => stockInItemIds.Contains(b.StockInItemId)).ExecuteDeleteAsync(ct);
            await db.StockInItemExtends.Where(e => stockInItemIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
            await db.StockInItems.Where(i => stockInItemIds.Contains(i.Id)).ExecuteDeleteAsync(ct);
        }

        var stockInIds = snap.StockIns.Select(s => s.Id).ToList();
        if (stockInIds.Count > 0)
        {
            await db.StockInExtends.Where(e => stockInIds.Contains(e.StockInId)).ExecuteDeleteAsync(ct);
            await db.StockIns.Where(s => stockInIds.Contains(s.Id)).ExecuteDeleteAsync(ct);
        }

        var qcIds = snap.QCInfos.Select(q => q.Id).ToList();
        if (qcIds.Count > 0)
            await db.QCItems.Where(x => qcIds.Contains(x.QcInfoId)).ExecuteDeleteAsync(ct);
        if (qcIds.Count > 0)
            await db.QCInfos.Where(q => qcIds.Contains(q.Id)).ExecuteDeleteAsync(ct);

        var notifyIds = snap.StockInNotifies.Select(n => n.Id).ToList();
        if (notifyIds.Count > 0)
            await db.StockInNotifies.Where(n => notifyIds.Contains(n.Id)).ExecuteDeleteAsync(ct);

        var poItemIds = snap.PurchaseOrderItems.Select(p => p.Id).ToList();
        if (poItemIds.Count > 0)
            await db.PurchaseOrderItems.Where(p => poItemIds.Contains(p.Id)).ExecuteDeleteAsync(ct);

        foreach (var po in snap.PurchaseOrders)
        {
            var cnt = await db.PurchaseOrderItems.CountAsync(i => i.PurchaseOrderId == po.Id, ct);
            if (cnt == 0)
                await db.PurchaseOrders.Where(p => p.Id == po.Id).ExecuteDeleteAsync(ct);
        }

        var prIds = snap.PurchaseRequisitions.Select(p => p.Id).ToList();
        if (prIds.Count > 0)
            await db.PurchaseRequisitions.Where(p => prIds.Contains(p.Id)).ExecuteDeleteAsync(ct);

        var soLineIds = snap.SellOrderItems.Select(i => i.Id).ToList();
        if (soLineIds.Count > 0)
            await db.SellOrderItemExtends.Where(e => soLineIds.Contains(e.Id)).ExecuteDeleteAsync(ct);
        if (soLineIds.Count > 0)
            await db.SellOrderItems.Where(i => soLineIds.Contains(i.Id)).ExecuteDeleteAsync(ct);

        foreach (var so in snap.SellOrders)
        {
            var cnt = await db.SellOrderItems.CountAsync(i => i.SellOrderId == so.Id, ct);
            if (cnt == 0)
            {
                await db.SellOrderExtends.Where(e => e.SellOrderId == so.Id).ExecuteDeleteAsync(ct);
                await db.SellOrders.Where(s => s.Id == so.Id).ExecuteDeleteAsync(ct);
            }
        }

        var qiIds = snap.QuoteItems.Select(q => q.Id).ToList();
        if (qiIds.Count > 0)
            await db.QuoteItems.Where(q => qiIds.Contains(q.Id)).ExecuteDeleteAsync(ct);
        var qIds = snap.Quotes.Select(q => q.Id).ToList();
        if (qIds.Count > 0)
            await db.Quotes.Where(q => qIds.Contains(q.Id)).ExecuteDeleteAsync(ct);

        var riIds = snap.RfqItems.Select(i => i.Id).ToList();
        if (riIds.Count > 0)
            await db.RFQItems.Where(i => riIds.Contains(i.Id)).ExecuteDeleteAsync(ct);
        await db.RFQs.Where(r => r.Id == snap.Rfq.Id).ExecuteDeleteAsync(ct);
    }
}
