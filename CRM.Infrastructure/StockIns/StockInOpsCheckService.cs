using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Services;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.StockIns;

public sealed class StockInOpsCheckService : IStockInOpsCheckService
{
    public const int MaxFindings = 2000;

    private readonly ApplicationDbContext _db;

    public StockInOpsCheckService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<StockInOpsCheckResultDto> RunAsync(CancellationToken cancellationToken = default)
    {
        var findings = new List<StockInOpsCheckFindingDto>();

        var notices = await _db.StockInNotifies.AsNoTracking()
            .Where(n => !n.IsDeleted)
            .Select(n => new
            {
                n.Id,
                n.NoticeCode,
                n.Status,
                n.StockInType,
                n.VendorId,
                n.PurchaseOrderId,
                n.PurchaseOrderCode
            })
            .ToListAsync(cancellationToken);

        var purchaseNotices = notices.Where(n => IsPurchaseType(n.StockInType)).ToList();

        var qcs = await _db.QCInfos.AsNoTracking()
            .Where(q => !q.IsDeleted)
            .Select(q => new
            {
                q.Id,
                q.QcCode,
                q.StockInNotifyId,
                q.StockInId,
                q.StockInStatus,
                q.StockInType
            })
            .ToListAsync(cancellationToken);

        var stockIns = await _db.StockIns.AsNoTracking()
            .Where(si => !si.IsDeleted && si.StockInType != StockInTypeCode.Transfer)
            .Select(si => new
            {
                si.Id,
                si.StockInCode,
                si.Status,
                si.StockInType,
                si.SourceId,
                si.SourceCode,
                si.QcId,
                si.VendorId
            })
            .ToListAsync(cancellationToken);

        var purchaseStockIns = stockIns.Where(si => IsPurchaseType(si.StockInType)).ToList();

        var siItems = await _db.StockInItems.AsNoTracking()
            .Where(i => !i.IsDeleted)
            .Select(i => new
            {
                i.Id,
                i.StockInId,
                i.StockInItemCode,
                i.Quantity,
                i.Price,
                i.Amount
            })
            .ToListAsync(cancellationToken);

        var purchaseSiIds = purchaseStockIns.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var purchaseItems = siItems.Where(i => purchaseSiIds.Contains(i.StockInId)).ToList();
        var purchaseItemIds = purchaseItems.Select(x => x.Id).ToList();

        var extends = purchaseItemIds.Count == 0
            ? new List<(string Id, string? PurchaseOrderItemId)>()
            : (await _db.StockInItemExtends.AsNoTracking()
                .Where(e => !e.IsDeleted && purchaseItemIds.Contains(e.Id))
                .Select(e => new { e.Id, e.PurchaseOrderItemId })
                .ToListAsync(cancellationToken))
            .Select(e => (e.Id, e.PurchaseOrderItemId))
            .ToList();

        var stockItems = await _db.StockItems.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => new { s.Id, s.StockItemCode, s.StockInItemId, s.StockInId })
            .ToListAsync(cancellationToken);

        var writeOffs = await _db.FinancePurchaseInvoiceWriteOffs.AsNoTracking()
            .Select(w => new { w.Id, w.FinancePurchaseInvoiceId, w.StockInId, w.StockInItemId })
            .ToListAsync(cancellationToken);

        var invoiceIds = writeOffs
            .Select(w => w.FinancePurchaseInvoiceId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var invoices = invoiceIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.FinancePurchaseInvoices.IgnoreQueryFilters().AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .Select(i => new { i.Id, i.InvoiceCode, i.InvoiceNo })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                x => x.Id,
                x => FirstCode(x.InvoiceCode, x.InvoiceNo, x.Id),
                StringComparer.OrdinalIgnoreCase);

        var writeOffSiIds = writeOffs
            .Select(w => w.StockInId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var siIncludingDeleted = writeOffSiIds.Count == 0
            ? new List<(string Id, string StockInCode, short StockInType, bool IsDeleted)>()
            : (await _db.StockIns.IgnoreQueryFilters().AsNoTracking()
                .Where(si => writeOffSiIds.Contains(si.Id))
                .Select(si => new { si.Id, si.StockInCode, si.StockInType, si.IsDeleted })
                .ToListAsync(cancellationToken))
            .Select(x => (x.Id, x.StockInCode, x.StockInType, x.IsDeleted))
            .ToList();

        var stockItemSiIds = stockItems
            .Select(s => s.StockInId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var stockItemSiTypeById = stockItemSiIds.Count == 0
            ? new Dictionary<string, (short StockInType, bool IsDeleted)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.StockIns.IgnoreQueryFilters().AsNoTracking()
                .Where(si => stockItemSiIds.Contains(si.Id))
                .Select(si => new { si.Id, si.StockInType, si.IsDeleted })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => (x.StockInType, x.IsDeleted), StringComparer.OrdinalIgnoreCase);

        var poIds = purchaseNotices
            .Select(n => n.PurchaseOrderId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var poItemIds = extends
            .Select(e => e.PurchaseOrderItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var poItems = poItemIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.PurchaseOrderItems.AsNoTracking()
                .Where(i => poItemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.PurchaseOrderId })
                .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => x.PurchaseOrderId, StringComparer.OrdinalIgnoreCase);

        foreach (var poId in poItems.Values)
        {
            if (!string.IsNullOrWhiteSpace(poId) && !poIds.Contains(poId, StringComparer.OrdinalIgnoreCase))
                poIds.Add(poId.Trim());
        }

        var purchaseOrders = poIds.Count == 0
            ? new Dictionary<string, (string? VendorId, string? Code)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.PurchaseOrders.AsNoTracking()
                .Where(p => poIds.Contains(p.Id))
                .Select(p => new { p.Id, p.VendorId, p.PurchaseOrderCode })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                x => x.Id,
                x => (VendorId: (string?)x.VendorId, Code: (string?)x.PurchaseOrderCode),
                StringComparer.OrdinalIgnoreCase);

        var allVendorIds = purchaseNotices.Select(n => n.VendorId)
            .Concat(purchaseStockIns.Select(s => s.VendorId))
            .Concat(purchaseOrders.Values.Select(v => v.VendorId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vendorCodes = allVendorIds.Count == 0
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : (await _db.Vendors.AsNoTracking()
                .Where(v => allVendorIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Code })
                .ToListAsync(cancellationToken))
            .ToDictionary(v => v.Id, v => (string?)v.Code, StringComparer.OrdinalIgnoreCase);

        string VendorSlot(string? docCode, string? vendorId) =>
            OpsCheckDocumentCodes.FormatDocPartySlot(documentCode: docCode, vendorId, vendorCodes);

        var siById = purchaseStockIns.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var itemsByStockIn = purchaseItems
            .GroupBy(x => x.StockInId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var liveItemIds = purchaseItems.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var extByItemId = extends.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var qcsByNotice = qcs
            .Where(q => !string.IsNullOrWhiteSpace(q.StockInNotifyId))
            .GroupBy(q => q.StockInNotifyId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var writeOffsBySi = writeOffs
            .GroupBy(w => w.StockInId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var stockItemsByItem = stockItems
            .Where(s => !string.IsNullOrWhiteSpace(s.StockInItemId))
            .GroupBy(s => s.StockInItemId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var siDeleteById = siIncludingDeleted
            .ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);

        StockInOpsCheckSuggestions.StockInHint ToSiHint(string siId)
        {
            siById.TryGetValue(siId, out var si);
            return new StockInOpsCheckSuggestions.StockInHint(
                OpsCheckDocumentCodes.ForSuggestion(si?.StockInCode),
                InvoiceCodesForSi(siId));
        }

        List<string> InvoiceCodesForSi(string siId)
        {
            if (!writeOffsBySi.TryGetValue(siId, out var rows) || rows.Count == 0)
                return new List<string>();
            return rows
                .Select(w => invoices.TryGetValue(w.FinancePurchaseInvoiceId, out var code)
                    ? code
                    : null)
                .Where(OpsCheckDocumentCodes.IsUsableCode)
                .Select(c => c!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        string FormatInvoiceBits(IEnumerable<string> siIds)
        {
            var parts = new List<string>();
            foreach (var id in siIds)
            {
                foreach (var code in InvoiceCodesForSi(id))
                    parts.Add(code);
            }

            return string.Join("、", parts.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        List<string> OrderSiIds(IEnumerable<string> ids) =>
            ids.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    id => siById.TryGetValue(id, out var si) ? si.StockInCode ?? "" : "",
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        var postedByNotice = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var unpostedByNotice = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var notice in purchaseNotices)
        {
            qcsByNotice.TryGetValue(notice.Id, out var noticeQcs);
            noticeQcs ??= new();
            var qcEntities = noticeQcs
                .Select(q => new QCInfo { Id = q.Id, StockInId = q.StockInId })
                .ToList();
            var noticeEntity = new StockInNotify
            {
                Id = notice.Id,
                NoticeCode = notice.NoticeCode,
                StockInType = notice.StockInType
            };

            foreach (var si in purchaseStockIns)
            {
                var siEntity = ToSiEntity(si.Id, si.Status, si.StockInType, si.SourceId, si.SourceCode, si.QcId);
                if (!IsLinkedToNotice(siEntity, noticeEntity, qcEntities))
                    continue;
                if (si.Status == StockInHeaderStatusCode.Posted)
                    Add(postedByNotice, notice.Id, si.Id);
                else if (si.Status is StockInHeaderStatusCode.Draft or StockInHeaderStatusCode.Pending)
                    Add(unpostedByNotice, notice.Id, si.Id);
            }
        }

        foreach (var notice in purchaseNotices)
        {
            postedByNotice.TryGetValue(notice.Id, out var postedIds);
            postedIds ??= new List<string>();
            var distinctPosted = OrderSiIds(postedIds);
            var noticeCode = OpsCheckDocumentCodes.ForSuggestion(notice.NoticeCode);

            if (notice.Status == ArrivalNoticeStatusCalculator.StatusStockedIn && distinctPosted.Count == 0)
            {
                unpostedByNotice.TryGetValue(notice.Id, out var unpostedIds);
                unpostedIds ??= new List<string>();
                var unpostedCodes = OrderSiIds(unpostedIds)
                    .Select(id => OpsCheckDocumentCodes.ForSuggestion(siById[id].StockInCode))
                    .ToList();
                AddFinding(findings, "error", "chain", "arrivalNotice", notice.Id, notice.NoticeCode,
                    "ArrivalNoticeList", null, null,
                    unpostedIds.Count > 0 ? "stockIn" : "debug",
                    unpostedIds.Count > 0 ? unpostedIds[0] : null,
                    unpostedIds.Count > 0 ? siById[unpostedIds[0]].StockInCode : "Debug",
                    unpostedIds.Count > 0 ? "StockInDetail" : "DebugData",
                    unpostedIds.Count > 0 ? Params(unpostedIds[0]) : null,
                    "到货通知状态为已入库，但没有已过账的采购入库单。",
                    StockInOpsCheckSuggestions.Notice100NoPosted(unpostedCodes));
            }

            if (distinctPosted.Count > 0 && notice.Status != ArrivalNoticeStatusCalculator.StatusStockedIn)
            {
                var codes = string.Join("、", distinctPosted.Select(id => siById[id].StockInCode));
                AddFinding(findings, "error", "status", "arrivalNotice", notice.Id, notice.NoticeCode,
                    "ArrivalNoticeList", null, null,
                    "stockIn", distinctPosted[0], siById[distinctPosted[0]].StockInCode,
                    "StockInDetail", Params(distinctPosted[0]),
                    $"已有已过账采购入库（{codes}），到货通知状态却不是已入库（当前 {notice.Status}）。",
                    StockInOpsCheckSuggestions.PostedButNoticeNot100());
            }

            if (distinctPosted.Count >= 2)
            {
                var codes = string.Join("、", distinctPosted.Select(id => siById[id].StockInCode));
                var invText = FormatInvoiceBits(distinctPosted.Skip(1));
                var extras = distinctPosted.Skip(1).Select(ToSiHint).ToList();
                var keeper = distinctPosted[0];
                AddFinding(findings, "error", "duplicate", "arrivalNotice", notice.Id, notice.NoticeCode,
                    "ArrivalNoticeList", null, null,
                    "stockIn", keeper, siById[keeper].StockInCode, "StockInDetail", Params(keeper),
                    $"同一到货通知存在 {distinctPosted.Count} 张已过账采购入库单：{codes}。保留单号最小的 {siById[keeper].StockInCode}。"
                    + (string.IsNullOrEmpty(invText) ? "" : $" 其余单进项发票：{invText}"),
                    StockInOpsCheckSuggestions.DuplicateKeepSmallest(extras));
            }

            var vendorIds = new List<string?> { notice.VendorId };
            string? poVendorId = null;
            string? poCode = notice.PurchaseOrderCode;
            if (!string.IsNullOrWhiteSpace(notice.PurchaseOrderId)
                && purchaseOrders.TryGetValue(notice.PurchaseOrderId.Trim(), out var poHdr))
            {
                poVendorId = poHdr.VendorId;
                if (string.IsNullOrWhiteSpace(poCode))
                    poCode = poHdr.Code;
            }

            vendorIds.Add(poVendorId);
            foreach (var siId in distinctPosted)
                vendorIds.Add(siById[siId].VendorId);

            var distinctVendors = vendorIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (distinctVendors.Count > 1)
            {
                var relatedSi = distinctPosted.Count > 0 ? distinctPosted[0] : null;
                AddFinding(findings, "error", "vendor", "arrivalNotice", notice.Id, notice.NoticeCode,
                    "ArrivalNoticeList", null, null,
                    relatedSi != null ? "stockIn" : "purchaseOrder",
                    relatedSi ?? notice.PurchaseOrderId,
                    relatedSi != null ? siById[relatedSi].StockInCode : poCode,
                    relatedSi != null ? "StockInDetail" : "PurchaseOrderDetail",
                    relatedSi != null ? Params(relatedSi) : (string.IsNullOrWhiteSpace(notice.PurchaseOrderId) ? null : Params(notice.PurchaseOrderId)),
                    $"供应商主键不一致：到货通知 {VendorSlot(notice.NoticeCode, notice.VendorId)} / 入库 {string.Join("、", distinctPosted.Select(id => VendorSlot(siById[id].StockInCode, siById[id].VendorId)))} / 采购订单 {VendorSlot(poCode, poVendorId)}。",
                    StockInOpsCheckSuggestions.VendorMismatch(
                        poCode,
                        relatedSi != null ? siById[relatedSi].StockInCode : null,
                        noticeCode));
            }
        }

        var linkedPostedIds = postedByNotice.Values
            .SelectMany(x => x)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var si in purchaseStockIns)
        {
            if (si.Status != StockInHeaderStatusCode.Posted || linkedPostedIds.Contains(si.Id))
                continue;
            string? poVendorId = null;
            string? poCode = null;
            string? poId = null;
            if (itemsByStockIn.TryGetValue(si.Id, out var linesForPo))
            {
                foreach (var line in linesForPo)
                {
                    if (!extByItemId.TryGetValue(line.Id, out var ext) || string.IsNullOrWhiteSpace(ext.PurchaseOrderItemId))
                        continue;
                    if (!poItems.TryGetValue(ext.PurchaseOrderItemId, out var fromItem) || string.IsNullOrWhiteSpace(fromItem))
                        continue;
                    poId = fromItem;
                    if (purchaseOrders.TryGetValue(fromItem, out var poHdr))
                    {
                        poVendorId = poHdr.VendorId;
                        poCode = poHdr.Code;
                    }

                    break;
                }
            }

            var vendorIds = new[] { si.VendorId, poVendorId }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (vendorIds.Count <= 1)
                continue;
            AddFinding(findings, "error", "vendor", "stockIn", si.Id, si.StockInCode,
                "StockInDetail", Params(si.Id), null,
                string.IsNullOrWhiteSpace(poId) ? null : "purchaseOrder",
                poId, poCode,
                string.IsNullOrWhiteSpace(poId) ? null : "PurchaseOrderDetail",
                string.IsNullOrWhiteSpace(poId) ? null : Params(poId),
                $"供应商主键不一致：入库 {VendorSlot(si.StockInCode, si.VendorId)} / 采购订单 {VendorSlot(poCode, poVendorId)}。",
                StockInOpsCheckSuggestions.VendorMismatch(poCode, si.StockInCode, noticeCode: null));
        }

        foreach (var si in purchaseStockIns)
        {
            if (si.Status != StockInHeaderStatusCode.Posted)
                continue;
            if (!itemsByStockIn.TryGetValue(si.Id, out var lines))
                continue;

            var hint = ToSiHint(si.Id);
            foreach (var line in lines)
            {
                stockItemsByItem.TryGetValue(line.Id, out var sis);
                sis ??= new();
                var itemCode = OpsCheckDocumentCodes.ForSuggestion(line.StockInItemCode);

                if (sis.Count == 0)
                {
                    AddFinding(findings, "error", "chain", "stockInItem", line.Id, itemCode,
                        "StockInDetail", Params(si.Id), null,
                        "stockIn", si.Id, si.StockInCode, "StockInDetail", Params(si.Id),
                        "已过账入库明细没有对应的库存明细。",
                        StockInOpsCheckSuggestions.PostedItemNoStockItem(hint));
                }
                else if (sis.Count >= 2)
                {
                    var ordered = sis
                        .OrderBy(x => x.StockItemCode ?? "", StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var extras = ordered.Skip(1)
                        .Select(x => OpsCheckDocumentCodes.ForSuggestion(x.StockItemCode))
                        .ToList();
                    var keeperCode = OpsCheckDocumentCodes.ForSuggestion(ordered[0].StockItemCode);
                    AddFinding(findings, "error", "duplicate", "stockItem", ordered[0].Id, keeperCode,
                        "InventoryStockItemList", null, Highlight(keeperCode),
                        "stockInItem", line.Id, itemCode, "StockInDetail", Params(si.Id),
                        $"同一入库明细存在 {sis.Count} 条库存明细，保留编号最小的 {keeperCode}。",
                        StockInOpsCheckSuggestions.DuplicateStockItems(extras));
                }

                var expected = line.Quantity * line.Price;
                if (!StockOutOpsCheckAmounts.AmountsMatch(expected, line.Amount))
                {
                    AddFinding(findings, "error", "amount", "stockInItem", line.Id, itemCode,
                        "StockInDetail", Params(si.Id), null,
                        "stockIn", si.Id, si.StockInCode, "StockInDetail", Params(si.Id),
                        $"入库明细金额 {line.Amount} 与「数量×单价」{StockOutOpsCheckAmounts.RoundAmount(expected)} 不一致。",
                        StockInOpsCheckSuggestions.AmountMismatchRebuild(hint));
                }
            }
        }

        foreach (var row in stockItems)
        {
            if (!stockItemSiTypeById.TryGetValue(row.StockInId, out var siMeta))
            {
                var code = OpsCheckDocumentCodes.ForSuggestion(row.StockItemCode);
                AddFinding(findings, "error", "chain", "stockItem", row.Id, code,
                    "InventoryStockItemList", null, Highlight(code),
                    null, null, null, null, null,
                    "库存明细挂着的入库单不存在。",
                    StockInOpsCheckSuggestions.OrphanStockItem(code));
                continue;
            }

            if (!IsPurchaseType(siMeta.StockInType))
                continue;

            if (liveItemIds.Contains(row.StockInItemId) && !siMeta.IsDeleted)
                continue;

            var stockItemCode = OpsCheckDocumentCodes.ForSuggestion(row.StockItemCode);
            var reason = siMeta.IsDeleted
                ? "入库单已删除，库存明细仍有效。"
                : "库存明细对应的入库明细不存在或已删除。";
            AddFinding(findings, "error", "chain", "stockItem", row.Id, stockItemCode,
                "InventoryStockItemList", null, Highlight(stockItemCode),
                "stockIn", row.StockInId, null, "StockInDetail", Params(row.StockInId),
                reason,
                StockInOpsCheckSuggestions.OrphanStockItem(stockItemCode));
        }

        var orphanKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var wo in writeOffs)
        {
            var siId = wo.StockInId.Trim();
            var exists = siDeleteById.TryGetValue(siId, out var siRow);
            var orphan = !exists || siRow.IsDeleted;
            if (!orphan)
                continue;
            if (exists && !IsPurchaseType(siRow.StockInType))
                continue;

            var invoiceCode = invoices.TryGetValue(wo.FinancePurchaseInvoiceId, out var code)
                ? code
                : null;
            invoiceCode = OpsCheckDocumentCodes.ForSuggestion(invoiceCode);
            var key = $"{wo.FinancePurchaseInvoiceId}|{siId}";
            if (!orphanKeys.Add(key))
                continue;

            AddFinding(findings, "error", "chain", "purchaseInvoice", wo.FinancePurchaseInvoiceId, invoiceCode,
                "FinancePurchaseInvoiceDetail", Params(wo.FinancePurchaseInvoiceId), null,
                "stockIn", siId, exists ? siRow.StockInCode : null,
                exists ? "StockInDetail" : null,
                exists ? Params(siId) : null,
                exists ? "入库单已删除，进项核销流水仍有效。" : "进项核销挂着的入库单不存在。",
                StockInOpsCheckSuggestions.OrphanWriteOff(invoiceCode));
        }

        foreach (var qc in qcs)
        {
            if (!IsPurchaseType(qc.StockInType))
                continue;
            if (qc.StockInStatus != 1)
                continue;

            var linkedPosted = purchaseStockIns.Where(si =>
                si.Status == StockInHeaderStatusCode.Posted
                && (
                    (!string.IsNullOrWhiteSpace(si.QcId)
                     && string.Equals(si.QcId.Trim(), qc.Id, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(qc.StockInId)
                        && string.Equals(qc.StockInId.Trim(), si.Id, StringComparison.OrdinalIgnoreCase))
                )).ToList();
            if (linkedPosted.Count == 0)
                continue;

            var first = linkedPosted[0];
            AddFinding(findings, "warning", "status", "qc", qc.Id, qc.QcCode,
                "QcList", null, null,
                "stockIn", first.Id, first.StockInCode, "StockInDetail", Params(first.Id),
                $"质检已关联已过账入库单 {first.StockInCode}，质检入库状态仍为未入库。",
                StockInOpsCheckSuggestions.QcStockInStatusLag(OpsCheckDocumentCodes.ForSuggestion(qc.QcCode)));
        }

        var truncated = findings.Count > MaxFindings;
        if (truncated)
            findings = findings.Take(MaxFindings).ToList();

        return new StockInOpsCheckResultDto
        {
            RanAtUtc = DateTime.UtcNow,
            ErrorCount = findings.Count(x => x.Severity == "error"),
            WarningCount = findings.Count(x => x.Severity == "warning"),
            FindingCount = findings.Count,
            Truncated = truncated,
            Findings = findings
        };
    }

    private static bool IsPurchaseType(short stockInType) =>
        stockInType != StockInTypeCode.Transfer
        && StockInTypeCode.Normalize(stockInType) == StockInTypeCode.Purchase;

    private static bool IsLinkedToNotice(StockIn si, StockInNotify notice, IReadOnlyList<QCInfo> qcRows)
    {
        var posted = si.Status;
        si.Status = StockInHeaderStatusCode.Posted;
        var linked = ArrivalNoticeStatusCalculator.IsPostedStockInLinkedToNotice(si, notice, qcRows);
        si.Status = posted;
        return linked;
    }

    private static StockIn ToSiEntity(
        string id,
        short status,
        short stockInType,
        string? sourceId,
        string? sourceCode,
        string? qcId) =>
        new()
        {
            Id = id,
            Status = status,
            StockInType = stockInType,
            SourceId = sourceId,
            SourceCode = sourceCode,
            QcId = qcId
        };

    private static string FirstCode(string? invoiceCode, string? invoiceNo, string id) =>
        OpsCheckDocumentCodes.ForSuggestion(invoiceCode, invoiceNo);

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

    private static void AddFinding(
        List<StockInOpsCheckFindingDto> findings,
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
        findings.Add(new StockInOpsCheckFindingDto
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
