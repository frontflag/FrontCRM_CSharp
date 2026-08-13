using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class FinancePurchaseInvoiceWriteOffService : IFinancePurchaseInvoiceWriteOffService
{
    private const short StockInCompleted = 2;

    private readonly IRepository<FinancePurchaseInvoice> _invoiceRepo;
    private readonly IRepository<FinancePurchaseInvoiceWriteOff> _writeOffRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockInItemExtend> _stockInItemExtendRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<VendorInfo> _vendorRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IDataPermissionService _dataPermission;
    private readonly IPurchaseOrderItemExtendSyncService _poItemExtendSync;
    private readonly IStockInExtendLineSeqService _stockInExtendLineSeq;
    private readonly IFinancePurchaseInvoicePaymentSyncService _invoicePaymentSync;
    private readonly IUnitOfWork? _unitOfWork;

    public FinancePurchaseInvoiceWriteOffService(
        IRepository<FinancePurchaseInvoice> invoiceRepo,
        IRepository<FinancePurchaseInvoiceWriteOff> writeOffRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockInItemExtend> stockInItemExtendRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<PurchaseOrder> poRepo,
        IRepository<VendorInfo> vendorRepo,
        IRepository<User> userRepo,
        IDataPermissionService dataPermission,
        IPurchaseOrderItemExtendSyncService poItemExtendSync,
        IStockInExtendLineSeqService stockInExtendLineSeq,
        IFinancePurchaseInvoicePaymentSyncService invoicePaymentSync,
        IUnitOfWork? unitOfWork = null)
    {
        _invoiceRepo = invoiceRepo;
        _writeOffRepo = writeOffRepo;
        _stockInRepo = stockInRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockInItemExtendRepo = stockInItemExtendRepo;
        _poItemRepo = poItemRepo;
        _poRepo = poRepo;
        _vendorRepo = vendorRepo;
        _userRepo = userRepo;
        _dataPermission = dataPermission;
        _poItemExtendSync = poItemExtendSync;
        _stockInExtendLineSeq = stockInExtendLineSeq;
        _invoicePaymentSync = invoicePaymentSync;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<FinancePurchaseInvoiceWriteOffVendorSummary>> GetVendorSummariesAsync(
        string? keyword, string? currentUserId, CancellationToken cancellationToken = default)
    {
        var all = (await _invoiceRepo.GetAllAsync()).ToList();
        if (!string.IsNullOrWhiteSpace(currentUserId))
            all = (await _dataPermission.FilterFinancePurchaseInvoicesAsync(currentUserId, all)).ToList();

        var pendingInvoices = all.Where(i => i.RedInvoiceStatus != 1 && i.VerifiedToBe > 0).ToList();
        var openStockByVendorCurrency = await LoadOpenStockInVendorCurrencyKeysAsync(cancellationToken);

        var groups = pendingInvoices
            .GroupBy(i => $"{i.VendorId.Trim()}::{i.Currency}", StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                var key = (first.VendorId.Trim(), first.Currency);
                return new FinancePurchaseInvoiceWriteOffVendorSummary
                {
                    VendorId = first.VendorId,
                    VendorName = first.VendorName,
                    Currency = first.Currency,
                    PendingWriteOffTotal = g.Sum(x => x.VerifiedToBe),
                    PendingInvoiceCount = g.Count(),
                    EarliestInvoiceDate = g.Min(x => x.InvoiceDate),
                    LatestInvoiceDate = g.Max(x => x.InvoiceDate),
                    HasOpenStockIn = openStockByVendorCurrency.Contains(key)
                };
            })
            .Where(x => x.HasOpenStockIn)
            .ToList();

        await EnrichVendorNamesAsync(groups, cancellationToken);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            groups = groups.Where(x =>
                (x.VendorName?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.VendorEnglishName?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.VendorId.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return groups
            .OrderBy(x => x.EarliestInvoiceDate ?? DateTime.MaxValue)
            .ThenBy(x => x.VendorName)
            .ToList();
    }

    public async Task<FinancePurchaseInvoiceWriteOffCandidates> GetCandidatesAsync(
        string vendorId, byte currency, string? currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vendorId))
            throw new ArgumentException("供应商ID不能为空", nameof(vendorId));
        if (currency is < 1 or > 3) currency = 1;

        var vid = vendorId.Trim();
        var all = (await _invoiceRepo.GetAllAsync()).ToList();
        if (!string.IsNullOrWhiteSpace(currentUserId))
            all = (await _dataPermission.FilterFinancePurchaseInvoicesAsync(currentUserId, all)).ToList();

        var invoices = all
            .Where(i => string.Equals(i.VendorId?.Trim(), vid, StringComparison.OrdinalIgnoreCase)
                        && i.Currency == currency
                        && i.RedInvoiceStatus != 1
                        && i.VerifiedToBe > 0)
            .OrderBy(i => i.InvoiceDate)
            .ThenBy(i => i.InvoiceCode)
            .ToList();

        var vendor = await _vendorRepo.GetByIdAsync(vid);
        var result = new FinancePurchaseInvoiceWriteOffCandidates
        {
            VendorId = vid,
            VendorName = invoices.FirstOrDefault()?.VendorName ?? vendor?.OfficialName,
            VendorEnglishName = vendor?.EnglishOfficialName,
            Currency = currency,
            Invoices = invoices.Select(i => new FinancePurchaseInvoiceWriteOffInvoiceRow
            {
                Id = i.Id,
                InvoiceCode = i.InvoiceCode,
                InvoiceNo = i.InvoiceNo,
                InvoiceDate = i.InvoiceDate,
                InvoiceAmount = i.InvoiceAmount,
                VerifiedDone = i.VerifiedDone,
                VerifiedToBe = i.VerifiedToBe,
                VerificationStatus = i.VerificationStatus,
                Currency = i.Currency,
                ConfirmStatus = i.ConfirmStatus,
                RedInvoiceStatus = i.RedInvoiceStatus
            }).ToList()
        };

        result.StockIns = await BuildPendingStockInRowsAsync(vid, currency, cancellationToken);
        return result;
    }

    public async Task<FinancePurchaseInvoiceWriteOffResult> ApplyAsync(
        FinancePurchaseInvoiceWriteOffRequest request, string? actingUserId, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FinancePurchaseInvoiceId))
            throw new ArgumentException("进项发票ID不能为空");
        var allocations = (request.Allocations ?? new List<FinancePurchaseInvoiceWriteOffAllocation>())
            .Where(a => !string.IsNullOrWhiteSpace(a.StockInItemId) && a.Amount > 0)
            .ToList();
        if (allocations.Count == 0)
            throw new ArgumentException("请至少填写一笔本次核销金额");

        var invoice = await _invoiceRepo.GetByIdAsync(request.FinancePurchaseInvoiceId.Trim())
            ?? throw new InvalidOperationException("进项发票不存在");
        if (invoice.RedInvoiceStatus == 1)
            throw new InvalidOperationException("已冲红的进项发票不允许核销");
        if (invoice.VerifiedToBe <= 0)
            throw new InvalidOperationException("该进项发票已无待核销金额");

        var applyTotal = Math.Round(allocations.Sum(a => a.Amount), 2, MidpointRounding.AwayFromZero);
        if (applyTotal > invoice.VerifiedToBe + 0.0001m)
            throw new ArgumentException("本次核销合计超过发票待核销金额");

        var itemIds = allocations.Select(a => a.StockInItemId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var items = (await _stockInItemRepo.FindAsync(i => itemIds.Contains(i.Id))).ToList();
        if (items.Count != itemIds.Count)
            throw new ArgumentException("存在无效的入库明细");

        var stockInIds = items.Select(i => i.StockInId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var stockIns = (await _stockInRepo.FindAsync(s => stockInIds.Contains(s.Id))).ToList();

        foreach (var si in stockIns)
        {
            if (si.Status != StockInCompleted)
                throw new InvalidOperationException($"入库单 {si.StockInCode} 未入库完成，不能核销");
            if (!string.Equals(si.VendorId?.Trim(), invoice.VendorId.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("入库单供应商与发票不一致");
        }

        var extends = (await _stockInItemExtendRepo.FindAsync(e => itemIds.Contains(e.Id))).ToList();
        var extById = extends.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var alloc in allocations)
        {
            var itemId = alloc.StockInItemId.Trim();
            var item = items.First(i => string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase));
            if (!extById.TryGetValue(itemId, out var ext))
                throw new InvalidOperationException($"入库明细 {itemId} 缺少扩展行");
            var lineCurrency = item.Currency is > 0 and <= 3 ? (byte)item.Currency.Value : invoice.Currency;
            if (lineCurrency != invoice.Currency)
                throw new ArgumentException($"入库明细币别与发票币别不一致：{item.StockInItemCode}");
            var amt = Math.Round(alloc.Amount, 2, MidpointRounding.AwayFromZero);
            if (amt > ext.InvoiceMatchToBe + 0.0001m)
                throw new ArgumentException($"本次核销超过明细待匹配金额：{item.StockInItemCode}");
        }

        var operatorId = ActingUserIdNormalizer.Normalize(actingUserId);
        var poItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alloc in allocations)
        {
            var itemId = alloc.StockInItemId.Trim();
            var item = items.First(i => string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase));
            var ext = extById[itemId];
            var amt = Math.Round(alloc.Amount, 2, MidpointRounding.AwayFromZero);

            await _writeOffRepo.AddAsync(new FinancePurchaseInvoiceWriteOff
            {
                Id = Guid.NewGuid().ToString(),
                FinancePurchaseInvoiceId = invoice.Id,
                FinancePurchaseInvoiceItemId = null,
                StockInItemId = itemId,
                StockInId = item.StockInId,
                PurchaseOrderItemId = string.IsNullOrWhiteSpace(ext.PurchaseOrderItemId) ? null : ext.PurchaseOrderItemId.Trim(),
                Amount = amt,
                Currency = invoice.Currency,
                OperatorUserId = operatorId,
                CreateTime = DateTime.UtcNow
            });

            ext.InvoiceMatchDone = Math.Round(ext.InvoiceMatchDone + amt, 2, MidpointRounding.AwayFromZero);
            ext.InvoiceMatchToBe = Math.Max(0m, Math.Round(item.Amount - ext.InvoiceMatchDone, 2, MidpointRounding.AwayFromZero));
            ext.InvoiceMatchStatus = ResolveMatchStatus(ext.InvoiceMatchDone, item.Amount);
            ext.InvoiceMatchCurrency = invoice.Currency;
            await _stockInItemExtendRepo.UpdateAsync(ext);

            if (!string.IsNullOrWhiteSpace(ext.PurchaseOrderItemId))
                poItemIds.Add(ext.PurchaseOrderItemId.Trim());
        }

        invoice.VerifiedDone = Math.Round(invoice.VerifiedDone + applyTotal, 2, MidpointRounding.AwayFromZero);
        invoice.VerifiedToBe = Math.Max(0m, Math.Round(invoice.InvoiceAmount - invoice.VerifiedDone, 2, MidpointRounding.AwayFromZero));
        invoice.VerificationStatus = ResolveMatchStatus(invoice.VerifiedDone, invoice.InvoiceAmount);
        invoice.ModifyTime = DateTime.UtcNow;
        invoice.ModifyByUserId = operatorId;
        await _invoiceRepo.UpdateAsync(invoice);

        foreach (var sid in stockInIds)
            await RefreshStockInHeaderMatchAsync(sid, cancellationToken);

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        foreach (var pid in poItemIds)
            await _poItemExtendSync.RecalculateAsync(pid, cancellationToken);

        await _invoicePaymentSync.RecalculateForInvoiceAsync(invoice.Id, cancellationToken);

        return new FinancePurchaseInvoiceWriteOffResult
        {
            FinancePurchaseInvoiceId = invoice.Id,
            AppliedTotal = applyTotal,
            AllocationCount = allocations.Count
        };
    }

    private async Task<HashSet<(string VendorId, byte Currency)>> LoadOpenStockInVendorCurrencyKeysAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var completed = (await _stockInRepo.FindAsync(s => s.Status == StockInCompleted)).ToList();
        if (completed.Count == 0) return new HashSet<(string, byte)>();

        var siIds = completed.Select(s => s.Id).ToList();
        var extends = (await _stockInItemExtendRepo.FindAsync(e => siIds.Contains(e.StockInId) && e.InvoiceMatchToBe > 0)).ToList();
        if (extends.Count == 0) return new HashSet<(string, byte)>();

        var itemIds = extends.Select(e => e.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var items = (await _stockInItemRepo.FindAsync(i => itemIds.Contains(i.Id))).ToList();
        var itemById = items.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
        var siById = completed.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);

        var set = new HashSet<(string, byte)>();
        foreach (var e in extends)
        {
            if (!itemById.TryGetValue(e.Id, out var item)) continue;
            if (!siById.TryGetValue(e.StockInId, out var si) || string.IsNullOrWhiteSpace(si.VendorId)) continue;
            var cur = e.InvoiceMatchCurrency
                      ?? (item.Currency is > 0 and <= 3 ? (byte)item.Currency.Value : (byte)1);
            set.Add((si.VendorId.Trim(), cur));
        }

        return set;
    }

    private async Task<List<FinancePurchaseInvoiceWriteOffStockInRow>> BuildPendingStockInRowsAsync(
        string vendorId, byte currency, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stockIns = (await _stockInRepo.FindAsync(s =>
                s.VendorId == vendorId && s.Status == StockInCompleted))
            .OrderByDescending(s => s.StockInDate)
            .ToList();
        if (stockIns.Count == 0) return new List<FinancePurchaseInvoiceWriteOffStockInRow>();

        var siIds = stockIns.Select(s => s.Id).ToList();
        var allItems = (await _stockInItemRepo.FindAsync(i => siIds.Contains(i.StockInId))).ToList();
        var allExt = (await _stockInItemExtendRepo.FindAsync(e => siIds.Contains(e.StockInId))).ToList();
        var extByItem = allExt.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
        var itemsBySi = allItems.GroupBy(i => i.StockInId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var poItemIds = allExt
            .Select(e => e.PurchaseOrderItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var poItems = poItemIds.Count == 0
            ? new List<PurchaseOrderItem>()
            : (await _poItemRepo.FindAsync(p => poItemIds.Contains(p.Id))).ToList();
        var poItemById = poItems.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var poIds = poItems.Select(p => p.PurchaseOrderId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var pos = poIds.Count == 0
            ? new List<PurchaseOrder>()
            : (await _poRepo.FindAsync(p => poIds.Contains(p.Id))).ToList();
        var poById = pos.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        var userIds = pos.Select(p => p.PurchaseUserId).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : (await _userRepo.FindAsync(u => userIds.Contains(u.Id))).ToList();
        var userNameById = users.ToDictionary(
            u => u.Id,
            u => EntityLookupService.FormatUserLoginName(u) ?? u.Id,
            StringComparer.OrdinalIgnoreCase);

        var vendor = await _vendorRepo.GetByIdAsync(vendorId);
        var rows = new List<FinancePurchaseInvoiceWriteOffStockInRow>();

        foreach (var si in stockIns)
        {
            if (!itemsBySi.TryGetValue(si.Id, out var lines)) continue;
            var pendingItems = new List<FinancePurchaseInvoiceWriteOffStockInItemRow>();
            foreach (var line in lines)
            {
                if (!extByItem.TryGetValue(line.Id, out var ext) || ext.InvoiceMatchToBe <= 0) continue;
                byte? fromLine = line.Currency is > 0 and <= 3 ? (byte)line.Currency.Value : null;
                var lineCur = ext.InvoiceMatchCurrency ?? fromLine ?? currency;
                if (lineCur != currency) continue;

                string? poCode = null;
                string? purchaseUserId = null;
                string? purchaseUserName = null;
                string? freight = null;
                if (!string.IsNullOrWhiteSpace(ext.PurchaseOrderItemId) &&
                    poItemById.TryGetValue(ext.PurchaseOrderItemId.Trim(), out var poi) &&
                    poById.TryGetValue(poi.PurchaseOrderId, out var po))
                {
                    poCode = po.PurchaseOrderCode;
                    purchaseUserId = po.PurchaseUserId;
                    if (!string.IsNullOrWhiteSpace(purchaseUserId) &&
                        userNameById.TryGetValue(purchaseUserId.Trim(), out var un))
                        purchaseUserName = un;
                    freight = FreightForwarderOrderNoLookup.FromPurchaseOrderItemId(
                        ext.PurchaseOrderItemId, poItemById, poById);
                }

                pendingItems.Add(new FinancePurchaseInvoiceWriteOffStockInItemRow
                {
                    StockInItemId = line.Id,
                    StockInItemCode = line.StockInItemCode,
                    Amount = line.Amount,
                    InvoiceMatchDone = ext.InvoiceMatchDone,
                    InvoiceMatchToBe = ext.InvoiceMatchToBe,
                    InvoiceMatchStatus = ext.InvoiceMatchStatus,
                    Currency = lineCur,
                    PurchaseOrderItemId = ext.PurchaseOrderItemId,
                    PurchaseOrderItemCode = ext.PurchaseOrderItemCode,
                    PurchaseOrderCode = poCode,
                    PurchaseUserId = purchaseUserId,
                    PurchaseUserName = purchaseUserName,
                    FreightForwarderOrderNo = freight
                });
            }

            if (pendingItems.Count == 0) continue;

            rows.Add(new FinancePurchaseInvoiceWriteOffStockInRow
            {
                StockInId = si.Id,
                StockInCode = si.StockInCode,
                StockInDate = si.StockInDate,
                Currency = currency,
                TotalAmount = pendingItems.Sum(x => x.Amount),
                InvoiceMatchDone = pendingItems.Sum(x => x.InvoiceMatchDone),
                InvoiceMatchToBe = pendingItems.Sum(x => x.InvoiceMatchToBe),
                InvoiceMatchStatus = ResolveMatchStatus(
                    pendingItems.Sum(x => x.InvoiceMatchDone),
                    pendingItems.Sum(x => x.Amount)),
                TotalQuantity = si.TotalQuantity,
                FreightForwarderOrderNo = FreightForwarderOrderNoDisplay.JoinDistinct(
                    pendingItems.Select(x => x.FreightForwarderOrderNo)),
                PurchaseOrderCodes = string.Join(", ",
                    pendingItems.Select(x => x.PurchaseOrderCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)),
                PurchaseUserId = pendingItems.Select(x => x.PurchaseUserId).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                PurchaseUserName = pendingItems.Select(x => x.PurchaseUserName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                VendorName = vendor?.OfficialName ?? si.VendorId,
                VendorEnglishName = vendor?.EnglishOfficialName,
                Items = pendingItems
            });
        }

        return rows;
    }

    private async Task RefreshStockInHeaderMatchAsync(string stockInId, CancellationToken cancellationToken)
    {
        var extends = (await _stockInItemExtendRepo.FindAsync(e => e.StockInId == stockInId)).ToList();
        var done = extends.Sum(e => e.InvoiceMatchDone);
        var toBe = extends.Sum(e => e.InvoiceMatchToBe);
        await _stockInExtendLineSeq.UpsertInvoiceMatchCacheAsync(
            stockInId,
            done,
            toBe,
            ResolveMatchStatus(done, done + toBe),
            extends.Select(e => e.InvoiceMatchCurrency).FirstOrDefault(c => c != null),
            cancellationToken);
    }

    private async Task EnrichVendorNamesAsync(
        List<FinancePurchaseInvoiceWriteOffVendorSummary> rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = rows.Select(r => r.VendorId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count == 0) return;
        var vendors = (await _vendorRepo.FindAsync(v => ids.Contains(v.Id))).ToList();
        var map = vendors.ToDictionary(v => v.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (!map.TryGetValue(row.VendorId, out var v)) continue;
            row.VendorName ??= v.OfficialName;
            row.VendorEnglishName = v.EnglishOfficialName;
        }
    }

    private static short ResolveMatchStatus(decimal done, decimal total)
    {
        if (done <= 0m) return 0;
        if (total > 0m && done + 0.0001m >= total) return 2;
        return 1;
    }
}
