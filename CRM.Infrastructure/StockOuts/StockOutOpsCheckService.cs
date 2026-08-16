using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockOuts;

public sealed class StockOutOpsCheckService : IStockOutOpsCheckService
{
    public const int MaxFindings = 2000;

    private readonly ApplicationDbContext _db;

    public StockOutOpsCheckService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StockOutOpsCheckResultDto> RunAsync(CancellationToken cancellationToken = default)
    {
        var findings = new List<StockOutOpsCheckFindingDto>();

        var packings = await _db.Packings.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.Id, p.Code, p.Status, p.CustomerId, p.StockOutType })
            .ToListAsync(cancellationToken);

        var packingItems = await _db.PackingItems.AsNoTracking()
            .Where(pi => !pi.IsDeleted)
            .Select(pi => new
            {
                pi.Id,
                pi.PackingId,
                pi.ItemCode,
                pi.Qty,
                pi.SellOrderItemId,
                pi.StockOutNotifyId
            })
            .ToListAsync(cancellationToken);

        var pickItems = await _db.PickingTaskItems.AsNoTracking()
            .Where(pti => !pti.IsDeleted && pti.PackingItemId != null)
            .Select(pti => new { pti.Id, pti.PackingItemId })
            .ToListAsync(cancellationToken);

        var stockOuts = await _db.StockOuts.AsNoTracking()
            .Where(so => !so.IsDeleted && so.StockOutType != StockOutTypeCode.Transfer)
            .Select(so => new
            {
                so.Id,
                so.StockOutCode,
                so.Status,
                so.StockOutType,
                so.SourceId,
                so.CustomerId,
                so.SellOrderItemId
            })
            .ToListAsync(cancellationToken);

        var soItems = await _db.StockOutItems.AsNoTracking()
            .Where(soi => !soi.IsDeleted)
            .Select(soi => new
            {
                soi.Id,
                soi.StockOutId,
                soi.PackingId,
                soi.PickingTaskItemId,
                soi.StockOutItemCode,
                Qty = soi.ActualQty > 0 ? soi.ActualQty : soi.Quantity
            })
            .ToListAsync(cancellationToken);

        var soItemIds = soItems.Select(x => x.Id).ToList();
        var extends = soItemIds.Count == 0
            ? new List<(string Id, string? SellOrderItemId, int QtyStockOut)>()
            : (await _db.StockOutItemExtends.AsNoTracking()
                .Where(e => !e.IsDeleted && soItemIds.Contains(e.Id))
                .Select(e => new { e.Id, e.SellOrderItemId, e.QtyStockOut })
                .ToListAsync(cancellationToken))
            .Select(e => (e.Id, e.SellOrderItemId, e.QtyStockOut))
            .ToList();

        var receivables = await _db.FinanceReceivables.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.ReceivableCode,
                r.StockOutId,
                r.StockOutCode,
                r.SellOrderId,
                r.SellOrderItemId,
                r.CustomerId,
                r.CustomerName,
                r.OutboundQty,
                r.UnitPrice,
                r.Currency,
                r.Amount,
                r.VerifiedDone,
                r.VerificationStatus,
                r.SellOrderCode
            })
            .ToListAsync(cancellationToken);

        var receiptCodesByArId = await LoadReceiptCodesByReceivableAsync(
            receivables.Select(r => r.Id).ToList(), cancellationToken);

        var sellLineIds = packingItems
            .Select(x => x.SellOrderItemId)
            .Concat(extends.Select(x => x.SellOrderItemId))
            .Concat(receivables.Select(x => x.SellOrderItemId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sellItems = sellLineIds.Count == 0
            ? new Dictionary<string, (string SellOrderId, decimal Price)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.SellOrderItems.AsNoTracking()
                .Where(si => sellLineIds.Contains(si.Id))
                .Select(si => new { si.Id, si.SellOrderId, si.Price })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => (x.SellOrderId, x.Price), StringComparer.OrdinalIgnoreCase);

        var sellOrderIds = sellItems.Values.Select(v => v.SellOrderId)
            .Concat(receivables.Select(r => r.SellOrderId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sellOrders = sellOrderIds.Count == 0
            ? new Dictionary<string, (string? CustomerId, short Currency, string? Code)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.SellOrders.AsNoTracking()
                .Where(so => sellOrderIds.Contains(so.Id))
                .Select(so => new { so.Id, so.CustomerId, so.Currency, so.SellOrderCode })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                x => x.Id,
                x => (CustomerId: (string?)x.CustomerId, Currency: x.Currency, Code: (string?)x.SellOrderCode),
                StringComparer.OrdinalIgnoreCase);

        var customerIds = packings.Select(p => p.CustomerId)
            .Concat(stockOuts.Select(s => s.CustomerId))
            .Concat(receivables.Select(r => r.CustomerId))
            .Concat(sellOrders.Values.Select(v => v.CustomerId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var customers = customerIds.Count == 0
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : (await _db.Customers.AsNoTracking()
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => new { c.Id, c.OfficialName, c.NickName })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                c => c.Id,
                c => string.IsNullOrWhiteSpace(c.OfficialName) ? c.NickName : c.OfficialName,
                StringComparer.OrdinalIgnoreCase);

        var notifyIds = packingItems
            .Select(x => x.StockOutNotifyId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var notifies = notifyIds.Count == 0
            ? new Dictionary<string, (string? RequestCode, short Status)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.StockOutRequests.AsNoTracking()
                .Where(n => notifyIds.Contains(n.Id))
                .Select(n => new { n.Id, n.RequestCode, n.Status })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                n => n.Id,
                n => ((string?)n.RequestCode, n.Status),
                StringComparer.OrdinalIgnoreCase);

        var soById = stockOuts.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var packingById = packings.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var itemsByPacking = packingItems
            .GroupBy(x => x.PackingId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var pickByPackingItem = pickItems
            .GroupBy(x => x.PackingItemId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList(), StringComparer.OrdinalIgnoreCase);
        var soItemsById = soItems.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var extByItemId = extends.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var soItemsByStockOut = soItems
            .GroupBy(x => x.StockOutId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var arByStockOut = receivables
            .GroupBy(x => x.StockOutId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var soIdsWithAr = arByStockOut.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allSoIdsIncludingDeleted = await _db.StockOuts.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(so => soIdsWithAr.Contains(so.Id))
            .Select(so => new { so.Id, so.IsDeleted, so.StockOutCode })
            .ToListAsync(cancellationToken);
        var soDeleteById = allSoIdsIncludingDeleted
            .ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

        bool IsEffective(short status) => status is 2 or 4;
        bool IsSales(short type) => StockOutTypeCode.IsSalesStockOut(type);

        StockOutOpsCheckSuggestions.StockOutHint ToSoHint(string soId)
        {
            var so = soById[soId];
            arByStockOut.TryGetValue(soId, out var ars);
            var recs = new List<StockOutOpsCheckSuggestions.ReceivableHint>();
            if (ars != null)
            {
                foreach (var ar in ars)
                {
                    receiptCodesByArId.TryGetValue(ar.Id, out var codes);
                    recs.Add(new StockOutOpsCheckSuggestions.ReceivableHint(
                        string.IsNullOrWhiteSpace(ar.ReceivableCode) ? ar.Id : ar.ReceivableCode,
                        ar.VerifiedDone,
                        codes ?? new List<string>()));
                }
            }
            return new StockOutOpsCheckSuggestions.StockOutHint(
                string.IsNullOrWhiteSpace(so.StockOutCode) ? soId : so.StockOutCode,
                so.Status,
                recs);
        }

        List<string> OrderSoIds(IEnumerable<string> ids) =>
            ids.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    id => soById.TryGetValue(id, out var so) ? so.StockOutCode ?? "" : "",
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        string FormatArBits(IEnumerable<string> soIds)
        {
            var parts = new List<string>();
            foreach (var id in soIds)
            {
                if (!arByStockOut.TryGetValue(id, out var ars))
                    continue;
                foreach (var x in ars)
                {
                    var wo = x.VerifiedDone > 0
                        ? (x.VerificationStatus == 2 ? "已核销完成" : "部分核销")
                        : "未核销";
                    parts.Add($"{x.ReceivableCode}（{wo}）");
                }
            }

            return string.Join("、", parts);
        }

        StockOutOpsCheckSuggestions.ReceivableHint ToArHint(
            string arId,
            string? arCode,
            decimal verifiedDone)
        {
            receiptCodesByArId.TryGetValue(arId, out var codes);
            return new StockOutOpsCheckSuggestions.ReceivableHint(
                string.IsNullOrWhiteSpace(arCode) ? arId : arCode,
                verifiedDone,
                codes ?? new List<string>());
        }

        var effectiveByPacking = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var so in stockOuts)
        {
            if (!IsEffective(so.Status))
                continue;
            if (!string.IsNullOrWhiteSpace(so.SourceId)
                && packingById.ContainsKey(so.SourceId.Trim()))
            {
                Add(effectiveByPacking, so.SourceId.Trim(), so.Id);
            }
        }

        foreach (var soi in soItems)
        {
            if (string.IsNullOrWhiteSpace(soi.PackingId) || !soById.TryGetValue(soi.StockOutId, out var so))
                continue;
            if (!IsEffective(so.Status))
                continue;
            Add(effectiveByPacking, soi.PackingId.Trim(), so.Id);
        }

        var linkedByPackingItem = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pi in packingItems)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (pickByPackingItem.TryGetValue(pi.Id, out var pids))
            {
                foreach (var soi in soItems)
                {
                    if (soi.PickingTaskItemId != null && pids.Contains(soi.PickingTaskItemId)
                        && soById.TryGetValue(soi.StockOutId, out var so)
                        && so.Status != 3)
                        set.Add(soi.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(pi.SellOrderItemId))
            {
                foreach (var soi in soItems)
                {
                    if (!string.Equals(soi.PackingId, pi.PackingId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!extByItemId.TryGetValue(soi.Id, out var ext))
                        continue;
                    if (!string.Equals(ext.SellOrderItemId, pi.SellOrderItemId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!soById.TryGetValue(soi.StockOutId, out var so) || so.Status == 3)
                        continue;
                    set.Add(soi.Id);
                }
            }

            if (set.Count > 0)
                linkedByPackingItem[pi.Id] = set;
        }

        foreach (var p in packings)
        {
            effectiveByPacking.TryGetValue(p.Id, out var soIds);
            soIds ??= new List<string>();
            var distinctSo = OrderSoIds(soIds);

            if (p.Status == PackingStatusCode.StockOutFinished && distinctSo.Count == 0)
            {
                AddFinding(findings, "error", "chain", "packing", p.Id, p.Code, "PackingDetail",
                    Params(p.Id), null, null, null, null, null, null,
                    "装箱单状态为出库完成，但没有未删除、未取消的有效出库单。",
                    StockOutOpsCheckSuggestions.PackingFinishedNoStockOut(p.Code ?? p.Id));
            }

            if (distinctSo.Count > 0 && p.Status != PackingStatusCode.StockOutFinished)
            {
                var codes = string.Join("、", distinctSo.Select(id => soById[id].StockOutCode));
                AddFinding(findings, "error", "status", "packing", p.Id, p.Code, "PackingDetail",
                    Params(p.Id), null, "stockOut", distinctSo[0], soById[distinctSo[0]].StockOutCode,
                    "StockOutDetail", Params(distinctSo[0]),
                    $"已有有效出库（{codes}），装箱单状态却不是出库完成（当前 {p.Status}）。",
                    StockOutOpsCheckSuggestions.PackingHasStockOutNotFinished(p.Code ?? p.Id));
            }

            if (distinctSo.Count >= 2)
            {
                var codes = string.Join("、", distinctSo.Select(id => soById[id].StockOutCode));
                var arText = FormatArBits(distinctSo);
                var extras = distinctSo.Skip(1).Select(ToSoHint).ToList();
                AddFinding(findings, "error", "duplicate", "packing", p.Id, p.Code, "PackingDetail",
                    Params(p.Id), null, "stockOut", distinctSo[0], soById[distinctSo[0]].StockOutCode,
                    "StockOutDetail", Params(distinctSo[0]),
                    $"同一装箱单存在 {distinctSo.Count} 张有效出库单：{codes}。"
                    + (string.IsNullOrEmpty(arText) ? "" : $" 应收：{arText}"),
                    StockOutOpsCheckSuggestions.DuplicateKeepSmallest(p.Code ?? p.Id, thisRowIsPacking: true, extras));
            }

            if (p.Status == PackingStatusCode.StockOutFinished
                && itemsByPacking.TryGetValue(p.Id, out var lines))
            {
                foreach (var pi in lines)
                {
                    if (linkedByPackingItem.ContainsKey(pi.Id))
                        continue;
                    AddFinding(findings, "error", "chain", "packingItem", pi.Id, pi.ItemCode,
                        "PackingItemList", null, Highlight(pi.ItemCode),
                        "packing", p.Id, p.Code, "PackingDetail", Params(p.Id),
                        "装箱单已出库完成，本行对不上出库明细。",
                        StockOutOpsCheckSuggestions.PackingItemUnlinked(
                            p.Code ?? p.Id,
                            pi.ItemCode ?? pi.Id,
                            distinctSo.Select(ToSoHint).ToList()));
                }
            }
        }

        foreach (var kv in linkedByPackingItem)
        {
            var soIds = kv.Value
                .Select(id => soItemsById.TryGetValue(id, out var soi) ? soi.StockOutId : null)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (soIds.Count < 2)
                continue;
            var pi = packingItems.First(x => string.Equals(x.Id, kv.Key, StringComparison.OrdinalIgnoreCase));
            var ordered = OrderSoIds(soIds);
            var codes = string.Join("、", ordered.Select(id => soById[id].StockOutCode));
            var firstSo = ordered[0];
            var arText = FormatArBits(ordered);
            packingById.TryGetValue(pi.PackingId, out var packing);
            var extras = ordered.Skip(1).Select(ToSoHint).ToList();
            AddFinding(findings, "error", "duplicate", "packingItem", pi.Id, pi.ItemCode,
                "PackingItemList", null, Highlight(pi.ItemCode),
                "stockOut", firstSo, soById[firstSo].StockOutCode, "StockOutDetail", Params(firstSo),
                $"装箱行重复出库 {ordered.Count} 次：{codes}。"
                + (string.IsNullOrEmpty(arText) ? "" : $" 应收：{arText}"),
                StockOutOpsCheckSuggestions.DuplicateKeepSmallest(
                    packing?.Code ?? pi.PackingId,
                    thisRowIsPacking: false,
                    extras));
        }

        foreach (var so in stockOuts)
        {
            if (so.Status != 4 || !IsSales(so.StockOutType))
                continue;
            if (arByStockOut.ContainsKey(so.Id))
                continue;
            AddFinding(findings, "error", "chain", "stockOut", so.Id, so.StockOutCode, "StockOutDetail",
                Params(so.Id), null, null, null, null, null, null,
                "销售出库已完成，没有有效应收款。",
                StockOutOpsCheckSuggestions.SalesDoneNoReceivable(so.StockOutCode ?? so.Id));
        }

        foreach (var ar in receivables)
        {
            if (!soDeleteById.TryGetValue(ar.StockOutId.Trim(), out var soRow))
            {
                AddFinding(findings, "error", "chain", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", ar.StockOutId, ar.StockOutCode, "StockOutDetail", Params(ar.StockOutId),
                    "应收挂着的出库单不存在。",
                    StockOutOpsCheckSuggestions.VoidReceivableChain(ToArHint(ar.Id, ar.ReceivableCode, ar.VerifiedDone)));
                continue;
            }

            if (soRow.IsDeleted)
            {
                AddFinding(findings, "error", "chain", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", soRow.Id, soRow.StockOutCode, "StockOutDetail", Params(soRow.Id),
                    "出库单已删除，应收仍有效。",
                    StockOutOpsCheckSuggestions.VoidReceivableChain(ToArHint(ar.Id, ar.ReceivableCode, ar.VerifiedDone)));
                continue;
            }

            if (!soById.TryGetValue(ar.StockOutId, out var soLive))
                continue;

            soItemsByStockOut.TryGetValue(soLive.Id, out var lines);
            lines ??= new();
            var qtySum = 0m;
            var amountSum = 0m;
            short? sellCurrency = null;
            string? sellCustomerId = null;
            foreach (var line in lines)
            {
                var qty = line.Qty;
                string? sellLineId = null;
                if (extByItemId.TryGetValue(line.Id, out var ext))
                {
                    if (ext.QtyStockOut > 0)
                        qty = ext.QtyStockOut;
                    sellLineId = ext.SellOrderItemId;
                }
                qtySum += qty;
                if (!string.IsNullOrWhiteSpace(sellLineId)
                    && sellItems.TryGetValue(sellLineId, out var si))
                {
                    amountSum += qty * si.Price;
                    if (sellOrders.TryGetValue(si.SellOrderId, out var soHdr))
                    {
                        sellCurrency ??= soHdr.Currency;
                        sellCustomerId ??= soHdr.CustomerId;
                    }
                }
            }

            if (qtySum > 0 && !StockOutOpsCheckAmounts.QuantitiesMatch(qtySum, ar.OutboundQty))
            {
                AddFinding(findings, "error", "amount", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", soLive.Id, soLive.StockOutCode, "StockOutDetail", Params(soLive.Id),
                    $"应收数量 {ar.OutboundQty} 与出库明细合计 {qtySum} 不一致。",
                    StockOutOpsCheckSuggestions.VoidThenRebuild(
                        ToArHint(ar.Id, ar.ReceivableCode, ar.VerifiedDone),
                        soLive.StockOutCode ?? soLive.Id));
            }

            if (amountSum > 0 && !StockOutOpsCheckAmounts.AmountsMatch(amountSum, ar.Amount))
            {
                AddFinding(findings, "error", "amount", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", soLive.Id, soLive.StockOutCode, "StockOutDetail", Params(soLive.Id),
                    $"应收金额 {ar.Amount} 与「出库数量×销售行单价」{StockOutOpsCheckAmounts.RoundAmount(amountSum)} 不一致。",
                    StockOutOpsCheckSuggestions.VoidThenRebuild(
                        ToArHint(ar.Id, ar.ReceivableCode, ar.VerifiedDone),
                        soLive.StockOutCode ?? soLive.Id));
            }

            if (sellCurrency is short cur && cur != ar.Currency)
            {
                AddFinding(findings, "error", "amount", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", soLive.Id, soLive.StockOutCode, "StockOutDetail", Params(soLive.Id),
                    $"应收币别 {ar.Currency} 与销售订单币别 {cur} 不一致。",
                    StockOutOpsCheckSuggestions.VoidThenRebuild(
                        ToArHint(ar.Id, ar.ReceivableCode, ar.VerifiedDone),
                        soLive.StockOutCode ?? soLive.Id));
            }

            var packingCustomer = soItemsByStockOut.TryGetValue(soLive.Id, out var soLines)
                ? soLines
                    .Select(l => l.PackingId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => packingById.TryGetValue(x!, out var pk) ? pk.CustomerId : null)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                : null;
            if (string.IsNullOrWhiteSpace(packingCustomer)
                && !string.IsNullOrWhiteSpace(soLive.SourceId)
                && packingById.TryGetValue(soLive.SourceId.Trim(), out var pkBySrc))
                packingCustomer = pkBySrc.CustomerId;

            var sellOrderCode = ar.SellOrderCode;
            if (string.IsNullOrWhiteSpace(sellOrderCode)
                && !string.IsNullOrWhiteSpace(ar.SellOrderId)
                && sellOrders.TryGetValue(ar.SellOrderId, out var sellHdr))
                sellOrderCode = sellHdr.Code;

            CheckCustomer(findings, ar.Id, ar.ReceivableCode, soLive.Id, soLive.StockOutCode,
                packingCustomer, soLive.CustomerId, ar.CustomerId, sellCustomerId, customers, ar.CustomerName,
                sellOrderCode, ToArHint(ar.Id, ar.ReceivableCode, ar.VerifiedDone));
        }

        foreach (var pi in packingItems)
        {
            if (string.IsNullOrWhiteSpace(pi.StockOutNotifyId))
                continue;
            if (!notifies.TryGetValue(pi.StockOutNotifyId, out var n))
                continue;
            if (n.Status is StockOutRequestStatusCode.StockedOut or StockOutRequestStatusCode.Cancelled)
                continue;
            if (!effectiveByPacking.TryGetValue(pi.PackingId, out var soIds) || soIds.Count == 0)
                continue;
            packingById.TryGetValue(pi.PackingId, out var packing);
            var ordered = OrderSoIds(soIds);
            AddFinding(findings, "warning", "status", "packingItem", pi.Id, pi.ItemCode,
                "PackingItemList", null, Highlight(pi.ItemCode),
                "packing", pi.PackingId, packing?.Code, "PackingDetail", Params(pi.PackingId),
                $"已有有效出库，出库通知 {n.Item1} 状态仍为 {n.Status}（应为已出库）。",
                StockOutOpsCheckSuggestions.NotifyNotStockedOut(
                    n.Item1 ?? pi.StockOutNotifyId,
                    ordered.Select(ToSoHint).ToList()));
        }

        var truncated = findings.Count > MaxFindings;
        if (truncated)
            findings = findings.Take(MaxFindings).ToList();

        return new StockOutOpsCheckResultDto
        {
            RanAtUtc = DateTime.UtcNow,
            ErrorCount = findings.Count(x => x.Severity == "error"),
            WarningCount = findings.Count(x => x.Severity == "warning"),
            FindingCount = findings.Count,
            Truncated = truncated,
            Findings = findings
        };
    }

    private static void CheckCustomer(
        List<StockOutOpsCheckFindingDto> findings,
        string arId,
        string? arCode,
        string soId,
        string? soCode,
        string? packingCustomerId,
        string? soCustomerId,
        string? arCustomerId,
        string? sellCustomerId,
        Dictionary<string, string?> customers,
        string? arCustomerName,
        string? sellOrderCode,
        StockOutOpsCheckSuggestions.ReceivableHint arHint)
    {
        var ids = new[] { packingCustomerId, soCustomerId, arCustomerId, sellCustomerId }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count > 1)
        {
            AddFinding(findings, "error", "customer", "receivable", arId, arCode,
                "FinanceReceivableDetail", Params(arId), null,
                "stockOut", soId, soCode, "StockOutDetail", Params(soId),
                $"客户主键不一致：装箱 {Norm(packingCustomerId)} / 出库 {Norm(soCustomerId)} / 应收 {Norm(arCustomerId)} / 销售订单 {Norm(sellCustomerId)}。",
                StockOutOpsCheckSuggestions.CustomerRefresh(sellOrderCode, arHint));
            return;
        }

        if (ids.Count == 1
            && customers.TryGetValue(ids[0], out var masterName)
            && !string.IsNullOrWhiteSpace(masterName)
            && !string.IsNullOrWhiteSpace(arCustomerName)
            && !string.Equals(masterName.Trim(), arCustomerName.Trim(), StringComparison.Ordinal))
        {
            AddFinding(findings, "warning", "customer", "receivable", arId, arCode,
                "FinanceReceivableDetail", Params(arId), null,
                "stockOut", soId, soCode, "StockOutDetail", Params(soId),
                $"应收客户名「{arCustomerName}」与客户主数据「{masterName}」不一致。",
                StockOutOpsCheckSuggestions.CustomerRefresh(sellOrderCode, ar: null));
        }
    }

    private static string Norm(string? id) =>
        string.IsNullOrWhiteSpace(id) ? "空" : id.Trim();

    private static void Add(Dictionary<string, List<string>> map, string key, string value)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = new List<string>();
            map[key] = list;
        }

        if (!list.Contains(value, StringComparer.OrdinalIgnoreCase))
            list.Add(value);
    }

    private static Dictionary<string, string> Params(string id) =>
        new() { ["id"] = id };

    private static Dictionary<string, string>? Highlight(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : new Dictionary<string, string> { ["highlight"] = code.Trim() };

    private async Task<Dictionary<string, List<string>>> LoadReceiptCodesByReceivableAsync(
        List<string> receivableIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (receivableIds.Count == 0)
            return result;

        var writeOffs = await _db.FinanceReceivableWriteOffs.AsNoTracking()
            .Where(w => receivableIds.Contains(w.FinanceReceivableId))
            .Select(w => new
            {
                w.FinanceReceivableId,
                w.FinanceReceiptId,
                w.FinanceCustomerAdvanceLedgerId,
                w.WriteOffSource
            })
            .ToListAsync(cancellationToken);
        if (writeOffs.Count == 0)
            return result;

        var receiptIds = writeOffs
            .Select(w => w.FinanceReceiptId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ledgerIds = writeOffs
            .Where(w => w.WriteOffSource == FinanceReceivableWriteOffSourceCode.AdvancePool
                        && !string.IsNullOrWhiteSpace(w.FinanceCustomerAdvanceLedgerId))
            .Select(w => w.FinanceCustomerAdvanceLedgerId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ledgerReceiptById = ledgerIds.Count == 0
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : (await _db.FinanceCustomerAdvanceLedgers.AsNoTracking()
                .Where(l => ledgerIds.Contains(l.Id))
                .Select(l => new { l.Id, l.FinanceReceiptId })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => x.FinanceReceiptId, StringComparer.OrdinalIgnoreCase);

        foreach (var rid in ledgerReceiptById.Values)
        {
            if (!string.IsNullOrWhiteSpace(rid))
                receiptIds.Add(rid.Trim());
        }

        var receiptCodes = receiptIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.FinanceReceipts.IgnoreQueryFilters().AsNoTracking()
                .Where(r => receiptIds.Contains(r.Id))
                .Select(r => new { r.Id, r.FinanceReceiptCode })
                .ToListAsync(cancellationToken))
            .Where(r => !string.IsNullOrWhiteSpace(r.FinanceReceiptCode))
            .ToDictionary(r => r.Id, r => r.FinanceReceiptCode.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var wo in writeOffs)
        {
            var receiptId = wo.FinanceReceiptId;
            if (string.IsNullOrWhiteSpace(receiptId)
                && !string.IsNullOrWhiteSpace(wo.FinanceCustomerAdvanceLedgerId)
                && ledgerReceiptById.TryGetValue(wo.FinanceCustomerAdvanceLedgerId, out var fromLedger))
                receiptId = fromLedger;
            if (string.IsNullOrWhiteSpace(receiptId)
                || !receiptCodes.TryGetValue(receiptId, out var code))
                continue;
            if (!result.TryGetValue(wo.FinanceReceivableId, out var list))
            {
                list = new List<string>();
                result[wo.FinanceReceivableId] = list;
            }

            if (!list.Contains(code, StringComparer.OrdinalIgnoreCase))
                list.Add(code);
        }

        return result;
    }

    private static void AddFinding(
        List<StockOutOpsCheckFindingDto> findings,
        string severity,
        string category,
        string docType,
        string? docId,
        string? docCode,
        string? routeName,
        Dictionary<string, string>? routeParams,
        Dictionary<string, string>? routeQuery,
        string? relatedDocType,
        string? relatedDocId,
        string? relatedDocCode,
        string? relatedRouteName,
        Dictionary<string, string>? relatedRouteParams,
        string reason,
        string suggestion)
    {
        findings.Add(new StockOutOpsCheckFindingDto
        {
            Severity = severity,
            Category = category,
            DocType = docType,
            DocId = docId,
            DocCode = docCode,
            RouteName = routeName,
            RouteParams = routeParams,
            RouteQuery = routeQuery,
            RelatedDocType = relatedDocType,
            RelatedDocId = relatedDocId,
            RelatedDocCode = relatedDocCode,
            RelatedRouteName = relatedRouteName,
            RelatedRouteParams = relatedRouteParams,
            Reason = reason,
            Suggestion = suggestion
        });
    }
}
