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
                r.VerificationStatus
            })
            .ToListAsync(cancellationToken);

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
            ? new Dictionary<string, (string? CustomerId, short Currency)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.SellOrders.AsNoTracking()
                .Where(so => sellOrderIds.Contains(so.Id))
                .Select(so => new { so.Id, so.CustomerId, so.Currency })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => (x.CustomerId, x.Currency), StringComparer.OrdinalIgnoreCase);

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
            var distinctSo = soIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (p.Status == PackingStatusCode.StockOutFinished && distinctSo.Count == 0)
            {
                AddFinding(findings, "error", "chain", "packing", p.Id, p.Code, "PackingDetail",
                    Params(p.Id), null, null, null, null, null, null,
                    "装箱单状态为出库完成，但没有未删除、未取消的有效出库单。",
                    "点本行单号打开「装箱单详情」，点击「刷新」。无有效出库时应从出库完成回退。");
            }

            if (distinctSo.Count > 0 && p.Status != PackingStatusCode.StockOutFinished)
            {
                var codes = string.Join("、", distinctSo.Select(id => soById[id].StockOutCode).OrderBy(x => x));
                AddFinding(findings, "error", "status", "packing", p.Id, p.Code, "PackingDetail",
                    Params(p.Id), null, "stockOut", distinctSo[0], soById[distinctSo[0]].StockOutCode,
                    "StockOutDetail", Params(distinctSo[0]),
                    $"已有有效出库（{codes}），装箱单状态却不是出库完成（当前 {p.Status}）。",
                    "点本行单号打开「装箱单详情」，点击「刷新」，把状态升到出库完成。");
            }

            if (distinctSo.Count >= 2)
            {
                var codes = string.Join("、", distinctSo.Select(id => soById[id].StockOutCode).OrderBy(x => x));
                AddFinding(findings, "error", "duplicate", "packing", p.Id, p.Code, "PackingDetail",
                    Params(p.Id), null, "stockOut", distinctSo[0], soById[distinctSo[0]].StockOutCode,
                    "StockOutDetail", Params(distinctSo[0]),
                    $"同一装箱单存在 {distinctSo.Count} 张有效出库单：{codes}。",
                    SuggestDeleteExtraStockOutsThenRefreshPacking());
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
                        "装箱单已出库完成，本行对不上出库明细（拣货行或 packing_id+销售行）。",
                        "点本行关联单号打开「装箱单详情」，核对本行装箱明细。再到「出库单列表」用装箱单号筛选，打开出库详情核对本行是否挂了拣货。没有一键修复：对该出库点「强制删除」后按装箱流程重出。");
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
            var codes = string.Join("、", soIds.Select(id => soById[id].StockOutCode).OrderBy(x => x));
            var firstSo = soIds[0];
            var arBits = soIds
                .Select(id => arByStockOut.TryGetValue(id, out var ars) ? ars.FirstOrDefault() : null)
                .Where(x => x != null)
                .Select(x =>
                {
                    var wo = x!.VerifiedDone > 0
                        ? (x.VerificationStatus == 2 ? "已核销完成" : "部分核销")
                        : "未核销";
                    return $"{x.ReceivableCode}（{wo}）";
                });
            var arText = string.Join("、", arBits);
            AddFinding(findings, "error", "duplicate", "packingItem", pi.Id, pi.ItemCode,
                "PackingItemList", null, Highlight(pi.ItemCode),
                "stockOut", firstSo, soById[firstSo].StockOutCode, "StockOutDetail", Params(firstSo),
                $"装箱行重复出库 {soIds.Count} 次：{codes}。"
                + (string.IsNullOrEmpty(arText) ? "" : $" 应收：{arText}"),
                SuggestDeleteExtraStockOutsThenRefreshPacking());
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
                "点本行单号打开「出库单详情」确认是销售出库且已完成。已完成单在「出库单列表」没有「标记完成」。请在「出库单列表」对该行点「强制删除」（输入出库单号确认），再按装箱重新出库，完成后点「标记完成」以生成应收。");
        }

        foreach (var ar in receivables)
        {
            if (!soDeleteById.TryGetValue(ar.StockOutId.Trim(), out var soRow))
            {
                AddFinding(findings, "error", "chain", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", ar.StockOutId, ar.StockOutCode, "StockOutDetail", Params(ar.StockOutId),
                    "应收挂着的出库单不存在。",
                    SuggestVoidReceivable(ar.VerifiedDone > 0));
                continue;
            }

            if (soRow.IsDeleted)
            {
                AddFinding(findings, "error", "chain", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", soRow.Id, soRow.StockOutCode, "StockOutDetail", Params(soRow.Id),
                    "出库单已删除，应收仍有效。",
                    SuggestVoidReceivable(ar.VerifiedDone > 0));
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
                    SuggestVoidThenRebuildReceivable());
            }

            if (amountSum > 0 && !StockOutOpsCheckAmounts.AmountsMatch(amountSum, ar.Amount))
            {
                AddFinding(findings, "error", "amount", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", soLive.Id, soLive.StockOutCode, "StockOutDetail", Params(soLive.Id),
                    $"应收金额 {ar.Amount} 与「出库数量×销售行单价」{StockOutOpsCheckAmounts.RoundAmount(amountSum)} 不一致。",
                    SuggestVoidThenRebuildReceivable());
            }

            if (sellCurrency is short cur && cur != ar.Currency)
            {
                AddFinding(findings, "error", "amount", "receivable", ar.Id, ar.ReceivableCode,
                    "FinanceReceivableDetail", Params(ar.Id), null,
                    "stockOut", soLive.Id, soLive.StockOutCode, "StockOutDetail", Params(soLive.Id),
                    $"应收币别 {ar.Currency} 与销售订单币别 {cur} 不一致。",
                    SuggestVoidThenRebuildReceivable());
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

            CheckCustomer(findings, ar.Id, ar.ReceivableCode, soLive.Id, soLive.StockOutCode,
                packingCustomer, soLive.CustomerId, ar.CustomerId, sellCustomerId, customers, ar.CustomerName);
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
            AddFinding(findings, "warning", "status", "packingItem", pi.Id, pi.ItemCode,
                "PackingItemList", null, Highlight(pi.ItemCode),
                "packing", pi.PackingId, packing?.Code, "PackingDetail", Params(pi.PackingId),
                $"已有有效出库，出库通知 {n.Item1} 状态仍为 {n.Status}（应为已出库）。",
                "到「出库单列表」用装箱单号筛选找到对应出库。未完成则点「标记完成」（会把出库通知写成已出库）。已完成则打开「出库通知」核对状态；当前没有单独改通知状态的按钮。");
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
        string? arCustomerName)
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
                "以销售订单客户为准：点本行关联出库打开「出库单详情」，再打开对应「销售订单详情」。点击页头「刷新」右侧下拉「刷新客户」并确认。已出库完成或已核销可能被阻断，需先处理出库/收款后再刷新。");
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
                "点本行关联出库打开「出库单详情」，再打开对应「销售订单详情」。点击页头「刷新」右侧下拉「刷新客户」，同步应收上的客户名称。");
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

    private static string SuggestVoidReceivable(bool verified) =>
        verified
            ? "点本行单号打开「应收款详情」，在核销记录记下收款单号；到「财务 → 收款单」找到该收款单，点击「反核销」。回到应收款详情点击「作废应收」，输入应收单号确认。"
            : "点本行单号打开「应收款详情」，确认未核销后点击「作废应收」，输入应收单号确认。";

    private static string SuggestVoidThenRebuildReceivable() =>
        "点本行单号打开「应收款详情」。未核销：点击「作废应收」，输入应收单号确认。已核销：先到「财务 → 收款单」点「反核销」，再作废应收。然后到「出库单列表」对该出库点「强制删除」，按装箱重出后点「标记完成」，按当前出库明细重建应收。";

    private static string SuggestDeleteExtraStockOutsThenRefreshPacking() =>
        "到「出库单列表」用装箱单号筛选。未核销的多余出库：操作列点「强制删除」，输入出库单号确认，只留一张。已核销：先到「财务 → 收款单」点「反核销」，再强制删除。然后点本行装箱单号打开「装箱单详情」，点击「刷新」。";

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
