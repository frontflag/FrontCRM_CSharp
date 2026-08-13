using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class FinanceSellInvoiceWriteOffService : IFinanceSellInvoiceWriteOffService
{
    private const short InvoiceStatusVoid = -1;
    private const short InvoiceStatusIssued = 100;
    private const short InvoiceTypeRed = 20;

    private readonly IRepository<FinanceSellInvoice> _invoiceRepo;
    private readonly IRepository<FinanceSellInvoiceWriteOff> _writeOffRepo;
    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepo;
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IDataPermissionService _dataPermission;
    private readonly IFinanceReceivableListQuery _receivableListQuery;
    private readonly IUnitOfWork? _unitOfWork;

    public FinanceSellInvoiceWriteOffService(
        IRepository<FinanceSellInvoice> invoiceRepo,
        IRepository<FinanceSellInvoiceWriteOff> writeOffRepo,
        IRepository<FinanceReceivable> receivableRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOutItemExtend> stockOutItemExtendRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<PurchaseOrderItem> purchaseOrderItemRepo,
        IRepository<PurchaseOrder> purchaseOrderRepo,
        IRepository<CustomerInfo> customerRepo,
        IRepository<User> userRepo,
        IDataPermissionService dataPermission,
        IFinanceReceivableListQuery receivableListQuery,
        IUnitOfWork? unitOfWork = null)
    {
        _invoiceRepo = invoiceRepo;
        _writeOffRepo = writeOffRepo;
        _receivableRepo = receivableRepo;
        _stockOutRepo = stockOutRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutItemExtendRepo = stockOutItemExtendRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockInRepo = stockInRepo;
        _purchaseOrderItemRepo = purchaseOrderItemRepo;
        _purchaseOrderRepo = purchaseOrderRepo;
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _dataPermission = dataPermission;
        _receivableListQuery = receivableListQuery;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<FinanceSellInvoiceWriteOffCustomerSummary>> GetCustomerSummariesAsync(
        string? keyword, string? currentUserId, CancellationToken cancellationToken = default)
    {
        var all = (await _invoiceRepo.GetAllAsync()).ToList();
        if (!string.IsNullOrWhiteSpace(currentUserId))
            all = (await _dataPermission.FilterFinanceSellInvoicesAsync(currentUserId, all)).ToList();

        var pendingInvoices = all.Where(IsMatchablePendingInvoice).ToList();
        var openRecvKeys = await LoadOpenReceivableCustomerCurrencyKeysAsync(cancellationToken);

        var groups = pendingInvoices
            .GroupBy(i => $"{i.CustomerId.Trim()}::{i.Currency}", StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                var key = (first.CustomerId.Trim(), first.Currency);
                return new FinanceSellInvoiceWriteOffCustomerSummary
                {
                    CustomerId = first.CustomerId,
                    CustomerName = first.CustomerName,
                    Currency = first.Currency,
                    PendingWriteOffTotal = g.Sum(x => x.MatchToBe),
                    PendingInvoiceCount = g.Count(),
                    EarliestInvoiceDate = g.Min(x => x.MakeInvoiceDate),
                    LatestInvoiceDate = g.Max(x => x.MakeInvoiceDate),
                    HasOpenReceivable = openRecvKeys.Contains(key)
                };
            })
            .Where(x => x.HasOpenReceivable)
            .ToList();

        await EnrichCustomerSummariesAsync(groups, cancellationToken);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            groups = groups.Where(x =>
                (x.CustomerName?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.CustomerEnglishName?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.SalesUserName?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.CustomerId.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return groups
            .OrderBy(x => x.EarliestInvoiceDate ?? DateTime.MaxValue)
            .ThenBy(x => x.CustomerName)
            .ToList();
    }

    public async Task<FinanceSellInvoiceWriteOffCandidates> GetCandidatesAsync(
        string customerId, byte currency, string? currentUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("客户ID不能为空", nameof(customerId));
        if (currency is < 1 or > 3) currency = 1;

        var cid = customerId.Trim();
        var all = (await _invoiceRepo.GetAllAsync()).ToList();
        if (!string.IsNullOrWhiteSpace(currentUserId))
            all = (await _dataPermission.FilterFinanceSellInvoicesAsync(currentUserId, all)).ToList();

        var invoices = all
            .Where(i => string.Equals(i.CustomerId?.Trim(), cid, StringComparison.OrdinalIgnoreCase)
                        && i.Currency == currency
                        && IsMatchablePendingInvoice(i))
            .OrderBy(i => i.MakeInvoiceDate)
            .ThenBy(i => i.InvoiceCode)
            .ToList();

        var customer = await _customerRepo.GetByIdAsync(cid);
        var result = new FinanceSellInvoiceWriteOffCandidates
        {
            CustomerId = cid,
            CustomerName = invoices.FirstOrDefault()?.CustomerName
                           ?? customer?.OfficialName
                           ?? customer?.CustomerName,
            CustomerEnglishName = customer?.EnglishOfficialName,
            Currency = currency,
            Invoices = invoices.Select(i => new FinanceSellInvoiceWriteOffInvoiceRow
            {
                Id = i.Id,
                InvoiceCode = i.InvoiceCode,
                InvoiceNo = i.InvoiceNo,
                InvoiceDate = i.MakeInvoiceDate,
                InvoiceAmount = i.InvoiceTotal,
                MatchDone = i.MatchDone,
                MatchToBe = i.MatchToBe,
                MatchStatus = i.MatchStatus,
                Currency = i.Currency,
                Type = i.Type,
                InvoiceStatus = i.InvoiceStatus,
                SellInvoiceType = i.SellInvoiceType
            }).ToList()
        };

        result.Receivables = await BuildPendingReceivableRowsAsync(cid, currency, currentUserId, cancellationToken);
        return result;
    }

    public async Task<FinanceSellInvoiceWriteOffResult> ApplyAsync(
        FinanceSellInvoiceWriteOffRequest request, string? actingUserId, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FinanceSellInvoiceId))
            throw new ArgumentException("销项发票ID不能为空");
        var allocations = (request.Allocations ?? new List<FinanceSellInvoiceWriteOffAllocation>())
            .Where(a => !string.IsNullOrWhiteSpace(a.FinanceReceivableId) && a.Amount > 0)
            .ToList();
        if (allocations.Count == 0)
            throw new ArgumentException("请至少填写一笔本次核销金额");

        var invoice = await _invoiceRepo.GetByIdAsync(request.FinanceSellInvoiceId.Trim())
            ?? throw new InvalidOperationException("销项发票不存在");
        if (!IsMatchablePendingInvoice(invoice))
            throw new InvalidOperationException("该销项发票不可匹配或已无待匹配金额");

        var applyTotal = Math.Round(allocations.Sum(a => a.Amount), 2, MidpointRounding.AwayFromZero);
        if (applyTotal > invoice.MatchToBe + 0.0001m)
            throw new ArgumentException("本次核销合计超过发票待匹配金额");

        var recvIds = allocations.Select(a => a.FinanceReceivableId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var receivables = (await _receivableRepo.FindAsync(r => recvIds.Contains(r.Id))).ToList();
        if (receivables.Count != recvIds.Count)
            throw new ArgumentException("存在无效的应收款");

        var recvById = receivables.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var alloc in allocations)
        {
            var recv = recvById[alloc.FinanceReceivableId.Trim()];
            if (recv.IsDeleted)
                throw new InvalidOperationException($"应收款 {recv.ReceivableCode ?? recv.Id} 已失效");
            if (!string.Equals(recv.CustomerId?.Trim(), invoice.CustomerId.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("应收款客户与发票不一致");
            var recvCur = (byte)(recv.Currency is >= 1 and <= 3 ? recv.Currency : 1);
            if (recvCur != invoice.Currency)
                throw new ArgumentException($"应收款币别与发票币别不一致：{recv.ReceivableCode ?? recv.Id}");
            var amt = Math.Round(alloc.Amount, 2, MidpointRounding.AwayFromZero);
            if (amt > recv.InvoiceMatchToBe + 0.0001m)
                throw new ArgumentException($"本次核销超过应收待匹配开票金额：{recv.ReceivableCode ?? recv.Id}");
        }

        var operatorId = ActingUserIdNormalizer.Normalize(actingUserId);

        foreach (var alloc in allocations)
        {
            var recvId = alloc.FinanceReceivableId.Trim();
            var recv = recvById[recvId];
            var amt = Math.Round(alloc.Amount, 2, MidpointRounding.AwayFromZero);

            await _writeOffRepo.AddAsync(new FinanceSellInvoiceWriteOff
            {
                Id = Guid.NewGuid().ToString(),
                FinanceSellInvoiceId = invoice.Id,
                FinanceSellInvoiceItemId = null,
                FinanceReceivableId = recvId,
                StockOutId = string.IsNullOrWhiteSpace(recv.StockOutId) ? null : recv.StockOutId.Trim(),
                Amount = amt,
                Currency = invoice.Currency,
                OperatorUserId = operatorId,
                CreateTime = DateTime.UtcNow
            });

            recv.InvoiceMatchDone = Math.Round(recv.InvoiceMatchDone + amt, 2, MidpointRounding.AwayFromZero);
            recv.InvoiceMatchToBe = Math.Max(0m, Math.Round(recv.Amount - recv.InvoiceMatchDone, 2, MidpointRounding.AwayFromZero));
            recv.InvoiceMatchStatus = ResolveMatchStatus(recv.InvoiceMatchDone, recv.Amount);
            recv.InvoiceMatchCurrency = invoice.Currency;
            recv.ModifyTime = DateTime.UtcNow;
            recv.ModifyByUserId = operatorId;
            await _receivableRepo.UpdateAsync(recv);
        }

        invoice.MatchDone = Math.Round(invoice.MatchDone + applyTotal, 2, MidpointRounding.AwayFromZero);
        invoice.MatchToBe = Math.Max(0m, Math.Round(invoice.InvoiceTotal - invoice.MatchDone, 2, MidpointRounding.AwayFromZero));
        invoice.MatchStatus = ResolveMatchStatus(invoice.MatchDone, invoice.InvoiceTotal);
        invoice.ModifyTime = DateTime.UtcNow;
        invoice.ModifyByUserId = operatorId;
        await _invoiceRepo.UpdateAsync(invoice);

        await RecalculateInvoiceReceiveProgressCoreAsync(invoice, cancellationToken);

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        return new FinanceSellInvoiceWriteOffResult
        {
            FinanceSellInvoiceId = invoice.Id,
            AppliedTotal = applyTotal,
            AllocationCount = allocations.Count
        };
    }

    public async Task RecalculateInvoiceReceiveProgressAsync(
        string financeSellInvoiceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(financeSellInvoiceId))
            return;
        var invoice = await _invoiceRepo.GetByIdAsync(financeSellInvoiceId.Trim());
        if (invoice == null) return;
        await RecalculateInvoiceReceiveProgressCoreAsync(invoice, cancellationToken);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
    }

    public async Task RecalculateReceiveProgressForReceivablesAsync(
        IEnumerable<string> financeReceivableIds, CancellationToken cancellationToken = default)
    {
        var ids = (financeReceivableIds ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0) return;

        var links = (await _writeOffRepo.FindAsync(w => ids.Contains(w.FinanceReceivableId))).ToList();
        var invoiceIds = links
            .Select(w => w.FinanceSellInvoiceId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var invoiceId in invoiceIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var invoice = await _invoiceRepo.GetByIdAsync(invoiceId);
            if (invoice == null) continue;
            await RecalculateInvoiceReceiveProgressCoreAsync(invoice, cancellationToken);
        }

        if (_unitOfWork != null && invoiceIds.Count > 0)
            await _unitOfWork.SaveChangesAsync();
    }

    private async Task RecalculateInvoiceReceiveProgressCoreAsync(
        FinanceSellInvoice invoice, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var links = (await _writeOffRepo.FindAsync(w => w.FinanceSellInvoiceId == invoice.Id)).ToList();
        if (links.Count == 0)
        {
            invoice.ReceiveDone = 0m;
            invoice.ReceiveToBe = 0m;
            invoice.ReceiveStatus = 0;
            invoice.ModifyTime = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);
            return;
        }

        var byRecv = links
            .GroupBy(w => w.FinanceReceivableId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount), StringComparer.OrdinalIgnoreCase);
        var recvIds = byRecv.Keys.ToList();
        var receivables = (await _receivableRepo.FindAsync(r => recvIds.Contains(r.Id))).ToList();
        var recvById = receivables.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

        decimal receiveDone = 0m;
        foreach (var (recvId, linkAmount) in byRecv)
        {
            if (!recvById.TryGetValue(recvId, out var recv)) continue;
            receiveDone += Math.Min(linkAmount, recv.VerifiedDone);
        }

        receiveDone = Math.Round(receiveDone, 2, MidpointRounding.AwayFromZero);
        var matchDone = invoice.MatchDone;
        invoice.ReceiveDone = receiveDone;
        invoice.ReceiveToBe = Math.Max(0m, Math.Round(matchDone - receiveDone, 2, MidpointRounding.AwayFromZero));
        invoice.ReceiveStatus = (byte)ResolveMatchStatus(receiveDone, matchDone);
        invoice.ModifyTime = DateTime.UtcNow;
        await _invoiceRepo.UpdateAsync(invoice);
    }

    private static bool IsMatchablePendingInvoice(FinanceSellInvoice i) =>
        i.InvoiceStatus != InvoiceStatusVoid
        && i.Type != InvoiceTypeRed
        && i.InvoiceStatus == InvoiceStatusIssued
        && i.MatchToBe > 0;

    private async Task<HashSet<(string CustomerId, byte Currency)>> LoadOpenReceivableCustomerCurrencyKeysAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var open = (await _receivableRepo.FindAsync(r => r.InvoiceMatchToBe > 0)).ToList();
        var set = new HashSet<(string, byte)>();
        foreach (var r in open)
        {
            if (string.IsNullOrWhiteSpace(r.CustomerId)) continue;
            var cur = (byte)(r.Currency is >= 1 and <= 3 ? r.Currency : 1);
            set.Add((r.CustomerId.Trim(), cur));
        }
        return set;
    }

    private async Task<List<FinanceSellInvoiceWriteOffReceivableRow>> BuildPendingReceivableRowsAsync(
        string customerId, byte currency, string? currentUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var paged = await _receivableListQuery.GetPagedAsync(new FinanceReceivableQueryRequest
        {
            CustomerId = customerId,
            Page = 1,
            PageSize = 2000,
            CurrentUserId = currentUserId
        }, cancellationToken);

        var list = paged.Items
            .Where(r => r.InvoiceMatchToBe > 0)
            .Where(r =>
            {
                var cur = (byte)(r.Currency is >= 1 and <= 3 ? r.Currency : 1);
                return cur == currency;
            })
            .OrderByDescending(r => r.StockOutDate ?? r.CreateTime)
            .ThenByDescending(r => r.CreateTime)
            .ToList();

        var customer = await _customerRepo.GetByIdAsync(customerId);
        var rows = list.Select(r => new FinanceSellInvoiceWriteOffReceivableRow
        {
            FinanceReceivableId = r.Id,
            ReceivableCode = r.ReceivableCode,
            StockOutId = r.StockOutId,
            StockOutCode = r.StockOutCode,
            StockOutDate = r.StockOutDate,
            SellOrderCode = r.SellOrderCode,
            SalesUserId = r.SalesUserId,
            Amount = r.Amount,
            InvoiceMatchDone = r.InvoiceMatchDone,
            InvoiceMatchToBe = r.InvoiceMatchToBe,
            InvoiceMatchStatus = r.InvoiceMatchStatus,
            Currency = (byte)(r.Currency is >= 1 and <= 3 ? r.Currency : currency),
            CustomerName = customer?.OfficialName ?? customer?.CustomerName ?? r.CustomerName,
            CustomerEnglishName = customer?.EnglishOfficialName
        }).ToList();

        await EnrichReceivableRowsAsync(rows, cancellationToken);
        return rows;
    }

    private async Task EnrichReceivableRowsAsync(
        List<FinanceSellInvoiceWriteOffReceivableRow> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0) return;
        cancellationToken.ThrowIfCancellationRequested();

        var salesUserIds = rows
            .Select(r => r.SalesUserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (salesUserIds.Count > 0)
        {
            var users = (await _userRepo.FindAsync(u => salesUserIds.Contains(u.Id))).ToList();
            var userMap = users.ToDictionary(
                u => u.Id,
                u => EntityLookupService.FormatUserLoginName(u) ?? u.Id,
                StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.SalesUserId)
                    && userMap.TryGetValue(row.SalesUserId.Trim(), out var name))
                    row.SalesUserName = name;
            }
        }

        var stockOutIds = rows
            .Select(r => r.StockOutId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (stockOutIds.Count == 0) return;

        var stockOuts = (await _stockOutRepo.FindAsync(s => stockOutIds.Contains(s.Id))).ToList();
        var stockOutById = stockOuts.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.StockOutId)) continue;
            if (!stockOutById.TryGetValue(row.StockOutId.Trim(), out var so)) continue;
            row.StockOutTotalQuantity = so.TotalQuantity;
            row.StockOutTotalAmount = so.TotalAmount;
            if (string.IsNullOrWhiteSpace(row.StockOutCode))
                row.StockOutCode = so.StockOutCode;
            if (row.StockOutDate == null)
                row.StockOutDate = so.StockOutDate;
        }

        var stockOutItems = (await _stockOutItemRepo.FindAsync(i => stockOutIds.Contains(i.StockOutId))).ToList();
        if (stockOutItems.Count == 0) return;

        var stockOutItemIds = stockOutItems.Select(i => i.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var extends = (await _stockOutItemExtendRepo.FindAsync(e => stockOutItemIds.Contains(e.Id))).ToList();
        var extendByItemId = extends.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);

        var stockInItemIds = extends
            .Select(e => e.StockInItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var stockInItems = stockInItemIds.Count == 0
            ? new List<StockInItem>()
            : (await _stockInItemRepo.FindAsync(i => stockInItemIds.Contains(i.Id))).ToList();
        var stockInIds = stockInItems.Select(i => i.StockInId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var stockIns = stockInIds.Count == 0
            ? new List<StockIn>()
            : (await _stockInRepo.FindAsync(i => stockInIds.Contains(i.Id))).ToList();
        var stockInById = stockIns.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);
        var stockInCodeByItemId = stockInItems.ToDictionary(
            i => i.Id,
            i => stockInById.TryGetValue(i.StockInId, out var si) ? si.StockInCode : null,
            StringComparer.OrdinalIgnoreCase);

        var purchaseOrderItemIds = extends
            .Select(e => e.PurchaseOrderItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var purchaseOrderItems = purchaseOrderItemIds.Count == 0
            ? new List<PurchaseOrderItem>()
            : (await _purchaseOrderItemRepo.FindAsync(i => purchaseOrderItemIds.Contains(i.Id))).ToList();
        var purchaseOrderIds = purchaseOrderItems.Select(i => i.PurchaseOrderId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var purchaseOrders = purchaseOrderIds.Count == 0
            ? new List<PurchaseOrder>()
            : (await _purchaseOrderRepo.FindAsync(p => purchaseOrderIds.Contains(p.Id))).ToList();
        var poById = purchaseOrders.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var poiById = purchaseOrderItems.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

        var stockInCodesByStockOutId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var freightByStockOutId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var outItem in stockOutItems)
        {
            var stockOutId = outItem.StockOutId.Trim();
            if (!extendByItemId.TryGetValue(outItem.Id, out var extend)) continue;

            if (!string.IsNullOrWhiteSpace(extend.StockInItemId)
                && stockInCodeByItemId.TryGetValue(extend.StockInItemId.Trim(), out var stockInCode)
                && !string.IsNullOrWhiteSpace(stockInCode))
            {
                if (!stockInCodesByStockOutId.TryGetValue(stockOutId, out var codes))
                {
                    codes = new List<string>();
                    stockInCodesByStockOutId[stockOutId] = codes;
                }
                if (!codes.Contains(stockInCode, StringComparer.OrdinalIgnoreCase))
                    codes.Add(stockInCode);
            }

            var freight = FreightForwarderOrderNoLookup.FromPurchaseOrderItemId(
                extend.PurchaseOrderItemId, poiById, poById);
            if (string.IsNullOrWhiteSpace(freight)) continue;
            if (!freightByStockOutId.TryGetValue(stockOutId, out var freightNos))
            {
                freightNos = new List<string>();
                freightByStockOutId[stockOutId] = freightNos;
            }
            if (!freightNos.Contains(freight, StringComparer.OrdinalIgnoreCase))
                freightNos.Add(freight);
        }

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.StockOutId)) continue;
            var sid = row.StockOutId.Trim();
            if (stockInCodesByStockOutId.TryGetValue(sid, out var codes) && codes.Count > 0)
                row.StockInCode = FreightForwarderOrderNoDisplay.JoinDistinct(codes);
            if (freightByStockOutId.TryGetValue(sid, out var freightNos) && freightNos.Count > 0)
                row.FreightForwarderOrderNo = FreightForwarderOrderNoDisplay.JoinDistinct(freightNos);
        }
    }

    private async Task EnrichCustomerSummariesAsync(
        List<FinanceSellInvoiceWriteOffCustomerSummary> rows, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = rows.Select(r => r.CustomerId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count == 0) return;
        var customers = (await _customerRepo.FindAsync(c => ids.Contains(c.Id))).ToList();
        var map = customers.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

        var salesUserIds = customers
            .Select(c => c.SalesUserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var userMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (salesUserIds.Count > 0)
        {
            var users = (await _userRepo.FindAsync(u => salesUserIds.Contains(u.Id))).ToList();
            userMap = users.ToDictionary(
                u => u.Id,
                u => EntityLookupService.FormatUserLoginName(u) ?? u.Id,
                StringComparer.OrdinalIgnoreCase);
        }

        foreach (var row in rows)
        {
            if (!map.TryGetValue(row.CustomerId, out var c)) continue;
            row.CustomerName ??= string.IsNullOrWhiteSpace(c.OfficialName) ? c.CustomerName : c.OfficialName;
            row.CustomerEnglishName = c.EnglishOfficialName;
            if (!string.IsNullOrWhiteSpace(c.SalesUserId))
            {
                row.SalesUserId = c.SalesUserId.Trim();
                if (userMap.TryGetValue(row.SalesUserId, out var name))
                    row.SalesUserName = name;
            }
        }
    }

    private static short ResolveMatchStatus(decimal done, decimal total)
    {
        if (done <= 0m) return 0;
        if (total > 0m && done + 0.0001m >= total) return 2;
        return 1;
    }
}
