using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class FinanceReceivableService : IFinanceReceivableService
{
    private const short StockOutFinished = 4;

    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly IRepository<FinanceReceivableWriteOff> _writeOffRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<FinanceReceipt> _receiptRepo;
    private readonly IRepository<FinanceReceiptItem> _receiptItemRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepo;
    private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepo;
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepo;
    private readonly IRepository<StockInItem> _stockInItemRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly ISerialNumberService _serialNumberService;
    private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
    private readonly IFinanceReceivableListQuery _listQuery;
    private readonly IFinanceCustomerAdvanceService _advanceService;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<FinanceCustomerAdvanceLedger> _advanceLedgerRepo;
    private readonly IFinanceSellInvoiceWriteOffService? _sellInvoiceWriteOffService;
    private readonly IUnitOfWork? _unitOfWork;

    public FinanceReceivableService(
        IRepository<FinanceReceivable> receivableRepo,
        IRepository<FinanceReceivableWriteOff> writeOffRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<FinanceReceipt> receiptRepo,
        IRepository<FinanceReceiptItem> receiptItemRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOutItemExtend> stockOutItemExtendRepo,
        IRepository<PurchaseOrderItem> purchaseOrderItemRepo,
        IRepository<PurchaseOrder> purchaseOrderRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockIn> stockInRepo,
        ISerialNumberService serialNumberService,
        ISellOrderItemExtendSyncService sellOrderItemExtendSync,
        IFinanceReceivableListQuery listQuery,
        IFinanceCustomerAdvanceService advanceService,
        IRepository<FinanceCustomerAdvanceLedger> advanceLedgerRepo,
        IRepository<User> userRepository,
        IRepository<CustomerInfo> customerRepo,
        IFinanceSellInvoiceWriteOffService? sellInvoiceWriteOffService = null,
        IUnitOfWork? unitOfWork = null)
    {
        _receivableRepo = receivableRepo;
        _writeOffRepo = writeOffRepo;
        _stockOutRepo = stockOutRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _sellOrderRepo = sellOrderRepo;
        _receiptRepo = receiptRepo;
        _receiptItemRepo = receiptItemRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutItemExtendRepo = stockOutItemExtendRepo;
        _purchaseOrderItemRepo = purchaseOrderItemRepo;
        _purchaseOrderRepo = purchaseOrderRepo;
        _stockInItemRepo = stockInItemRepo;
        _stockInRepo = stockInRepo;
        _serialNumberService = serialNumberService;
        _sellOrderItemExtendSync = sellOrderItemExtendSync;
        _listQuery = listQuery;
        _advanceService = advanceService;
        _advanceLedgerRepo = advanceLedgerRepo;
        _userRepository = userRepository;
        _customerRepo = customerRepo;
        _sellInvoiceWriteOffService = sellInvoiceWriteOffService;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task TryEnsureFromStockOutAsync(string stockOutId, string? actingUserId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockOutId))
            return;

        var stockOut = await _stockOutRepo.GetByIdAsync(stockOutId.Trim());
        if (stockOut == null || stockOut.IsDeleted)
            return;
        if (!StockOutTypeCode.IsSalesStockOut(stockOut.StockOutType))
            return;
        if (stockOut.Status != StockOutFinished)
            return;

        var existingList = (await _receivableRepo.FindAsync(r =>
            r.StockOutId == stockOut.Id && !r.IsDeleted)).ToList();
        foreach (var existing in existingList)
            await TrySyncReceivableStockOutDateAsync(existing, stockOut, actingUserId);

        var existingLineIds = existingList
            .Select(r => r.SellOrderItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var lineQty = await ResolveReceivableLineQuantitiesAsync(stockOut);
        if (lineQty.Count == 0)
            return;

        var touchedLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (sellLineId, outboundQty) in lineQty)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (existingLineIds.Contains(sellLineId))
                continue;
            if (outboundQty <= 0)
                continue;

            var soItem = await _sellOrderItemRepo.GetByIdAsync(sellLineId);
            if (soItem == null)
                continue;

            var sellOrder = await _sellOrderRepo.GetByIdAsync(soItem.SellOrderId);
            if (sellOrder == null)
                continue;

            var amount = Math.Round(outboundQty * soItem.Price, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0m)
                continue;

            var code = await _serialNumberService.GenerateNextAsync(ModuleCodes.FinanceReceivable);
            var receivable = new FinanceReceivable
            {
                Id = Guid.NewGuid().ToString(),
                ReceivableCode = code,
                StockOutId = stockOut.Id,
                StockOutCode = stockOut.StockOutCode,
                SellOrderId = sellOrder.Id,
                SellOrderCode = sellOrder.SellOrderCode,
                SellOrderItemId = soItem.Id,
                CustomerId = !string.IsNullOrWhiteSpace(stockOut.CustomerId)
                    ? stockOut.CustomerId.Trim()
                    : sellOrder.CustomerId,
                CustomerName = sellOrder.CustomerName,
                SalesUserId = sellOrder.SalesUserId,
                PN = soItem.PN,
                Brand = soItem.Brand,
                OutboundQty = outboundQty,
                UnitPrice = soItem.Price,
                Currency = sellOrder.Currency,
                Amount = amount,
                VerifiedDone = 0m,
                VerifiedToBe = amount,
                VerificationStatus = FinanceVerificationStatusCode.Pending,
                InvoiceMatchDone = 0m,
                InvoiceMatchToBe = amount,
                InvoiceMatchStatus = 0,
                InvoiceMatchCurrency = sellOrder.Currency,
                StockOutDate = ResolveReceivableStockOutDate(stockOut),
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };
            await _receivableRepo.AddAsync(receivable);
            touchedLines.Add(soItem.Id);
        }

        if (touchedLines.Count == 0)
            return;

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        foreach (var lineId in touchedLines)
            await _sellOrderItemExtendSync.RecalculateAsync(lineId);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task TrySoftDeleteForStockOutAsync(string stockOutId, string? actingUserId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockOutId))
            return;

        var receivables = (await _receivableRepo.FindAsync(r =>
            r.StockOutId == stockOutId.Trim() && !r.IsDeleted)).ToList();
        if (receivables.Count == 0)
            return;

        foreach (var receivable in receivables)
            AssertStockOutCanVoid(receivable);

        var actor = ActingUserIdNormalizer.Normalize(actingUserId);
        var now = DateTime.UtcNow;
        var touchedLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var receivable in receivables)
        {
            receivable.IsDeleted = true;
            receivable.ModifyTime = now;
            receivable.ModifyByUserId = actor;
            await _receivableRepo.UpdateAsync(receivable);
            if (!string.IsNullOrWhiteSpace(receivable.SellOrderItemId))
                touchedLines.Add(receivable.SellOrderItemId.Trim());
        }

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        foreach (var lineId in touchedLines)
            await _sellOrderItemExtendSync.RecalculateAsync(lineId);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task VoidUnverifiedAsync(
        string id,
        string confirmBillCode,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("请指定应收款");
        if (string.IsNullOrWhiteSpace(confirmBillCode))
            throw new ArgumentException("请填写 confirmBillCode");

        var receivable = await _receivableRepo.GetByIdAsync(id.Trim())
            ?? throw new InvalidOperationException("应收款不存在");
        if (receivable.IsDeleted)
            throw new InvalidOperationException("应收款已作废");

        var code = (receivable.ReceivableCode ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code)
            || !string.Equals(code, confirmBillCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("确认单号与应收单号不一致");

        AssertStockOutCanVoid(receivable);
        var liveStockOut = string.IsNullOrWhiteSpace(receivable.StockOutId)
            ? null
            : await _stockOutRepo.GetByIdAsync(receivable.StockOutId.Trim());
        FinanceReceivableVoidRules.AssertOrphanStockOutForDetailVoid(liveStockOut);
        await TrySoftDeleteForStockOutAsync(receivable.StockOutId, actingUserId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FinanceReceivableBackfillResult> BackfillReceivablesFromCompletedStockOutsAsync(
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new FinanceReceivableBackfillResult();

        var stockOuts = (await _stockOutRepo.GetAllAsync())
            .Where(so => !so.IsDeleted
                && StockOutTypeCode.IsSalesStockOut(so.StockOutType)
                && so.Status == StockOutFinished)
            .OrderBy(so => so.StockOutDate ?? so.CreateTime)
            .ToList();

        result.TotalCompletedSalesStockOuts = stockOuts.Count;

        await RemovePrematureReceivablesAsync(actingUserId, result, cancellationToken);

        var existingStockOutIds = (await _receivableRepo.FindAsync(r => !r.IsDeleted))
            .Select(r => r.StockOutId.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        result.AlreadyHasReceivableCount = stockOuts.Count(so =>
            existingStockOutIds.Contains(so.Id.Trim()));

        var candidates = stockOuts
            .Where(so => !existingStockOutIds.Contains(so.Id.Trim()))
            .ToList();

        result.CandidateCount = candidates.Count;

        foreach (var stockOut in stockOuts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingBefore = (await _receivableRepo.FindAsync(r =>
                r.StockOutId == stockOut.Id && !r.IsDeleted)).FirstOrDefault();
            var hadReceivable = existingBefore != null;
            var dateBefore = existingBefore?.StockOutDate;

            try
            {
                await TryEnsureFromStockOutAsync(stockOut.Id, actingUserId, cancellationToken);

                var existingAfter = (await _receivableRepo.FindAsync(r =>
                    r.StockOutId == stockOut.Id && !r.IsDeleted)).FirstOrDefault();

                if (!hadReceivable && existingAfter != null)
                {
                    result.CreatedCount++;
                    result.CreatedStockOutCodes.Add(stockOut.StockOutCode);
                }
                else if (hadReceivable
                    && existingAfter?.StockOutDate != null
                    && existingAfter.StockOutDate != dateBefore)
                {
                    result.StockOutDatesSyncedCount++;
                    result.StockOutDatesSyncedStockOutCodes.Add(stockOut.StockOutCode);
                }
                else if (!hadReceivable && existingAfter == null)
                {
                    result.SkippedIneligibleCount++;
                    result.SkippedIneligibleStockOutCodes.Add(stockOut.StockOutCode);
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedStockOutCodes.Add(stockOut.StockOutCode);
                result.FailedMessages.Add($"{stockOut.StockOutCode}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>软删出库单尚未「出库完成」(4) 时误生成的应收（历史兼容）。</summary>
    private async Task RemovePrematureReceivablesAsync(
        string? actingUserId,
        FinanceReceivableBackfillResult result,
        CancellationToken cancellationToken)
    {
        var stockOutById = (await _stockOutRepo.GetAllAsync())
            .Where(so => !so.IsDeleted)
            .GroupBy(so => so.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var receivable in (await _receivableRepo.FindAsync(r => !r.IsDeleted)).ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!stockOutById.TryGetValue(receivable.StockOutId.Trim(), out var stockOut)
                || stockOut.Status == StockOutFinished)
                continue;

            try
            {
                await TrySoftDeleteForStockOutAsync(receivable.StockOutId, actingUserId, cancellationToken);
                result.PrematureReceivablesRemovedCount++;
                if (!string.IsNullOrWhiteSpace(receivable.StockOutCode))
                    result.PrematureReceivablesRemovedStockOutCodes.Add(receivable.StockOutCode);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedStockOutCodes.Add(receivable.StockOutCode ?? receivable.StockOutId);
                result.FailedMessages.Add($"{receivable.StockOutCode}: 移除过早应收失败: {ex.Message}");
            }
        }
    }

    private async Task TrySyncReceivableStockOutDateAsync(
        FinanceReceivable receivable,
        StockOut stockOut,
        string? actingUserId)
    {
        var resolved = ResolveReceivableStockOutDate(stockOut);
        if (!resolved.HasValue || receivable.StockOutDate == resolved)
            return;

        receivable.StockOutDate = resolved;
        receivable.ModifyTime = DateTime.UtcNow;
        receivable.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _receivableRepo.UpdateAsync(receivable);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
    }

    private static DateTime? ResolveReceivableStockOutDate(StockOut stockOut)
    {
        if (stockOut.StockOutDate.HasValue)
            return stockOut.StockOutDate;
        if (stockOut.ConfirmedTime.HasValue)
            return stockOut.ConfirmedTime;
        return stockOut.CreateTime == default ? null : stockOut.CreateTime;
    }

    /// <inheritdoc />
    public void AssertStockOutCanVoid(FinanceReceivable? receivable)
    {
        if (receivable == null || receivable.IsDeleted)
            return;
        if (receivable.VerifiedDone > 0m)
            throw new InvalidOperationException(
                $"出库单 {receivable.StockOutCode} 已有收款核销（已核销 {receivable.VerifiedDone}），不可作废或删除");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockOutForceDeleteReceivableRow>> ListActiveForStockOutForceDeleteAsync(
        string stockOutId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockOutId))
            return Array.Empty<StockOutForceDeleteReceivableRow>();

        var key = stockOutId.Trim();
        var receivables = (await _receivableRepo.FindAsync(r => r.StockOutId == key && !r.IsDeleted))
            .OrderBy(r => r.ReceivableCode)
            .ThenBy(r => r.Id)
            .ToList();
        if (receivables.Count == 0)
            return Array.Empty<StockOutForceDeleteReceivableRow>();

        var arIds = receivables.Select(r => r.Id).ToList();
        var writeOffs = (await _writeOffRepo.FindAsync(w => arIds.Contains(w.FinanceReceivableId) && !w.IsDeleted))
            .ToList();
        var receiptIds = writeOffs
            .Select(w => w.FinanceReceiptId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var receiptCodeById = receiptIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _receiptRepo.FindAsync(r => receiptIds.Contains(r.Id)))
                .Where(r => !string.IsNullOrWhiteSpace(r.FinanceReceiptCode))
                .GroupBy(r => r.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().FinanceReceiptCode!.Trim(), StringComparer.OrdinalIgnoreCase);

        var codesByAr = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var wo in writeOffs)
        {
            if (!codesByAr.TryGetValue(wo.FinanceReceivableId, out var list))
            {
                list = new List<string>();
                codesByAr[wo.FinanceReceivableId] = list;
            }

            if (!string.IsNullOrWhiteSpace(wo.FinanceReceiptId)
                && receiptCodeById.TryGetValue(wo.FinanceReceiptId.Trim(), out var code)
                && !list.Contains(code, StringComparer.OrdinalIgnoreCase))
                list.Add(code);
            else if (wo.WriteOffSource == FinanceReceivableWriteOffSourceCode.AdvancePool
                     && !list.Contains("预收池", StringComparer.OrdinalIgnoreCase))
                list.Add("预收池");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return receivables.Select(r => new StockOutForceDeleteReceivableRow
        {
            Id = r.Id,
            ReceivableCode = r.ReceivableCode,
            Amount = r.Amount,
            VerifiedDone = r.VerifiedDone,
            VerifiedToBe = r.VerifiedToBe,
            VerificationStatus = r.VerificationStatus,
            ReceiptCodes = codesByAr.TryGetValue(r.Id, out var codes) ? codes : Array.Empty<string>()
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockOutDetailReceivableRowDto>> ListForStockOutDetailAsync(
        string stockOutId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockOutId))
            return Array.Empty<StockOutDetailReceivableRowDto>();

        var key = stockOutId.Trim();
        var receivables = (await _receivableRepo.FindAsync(r => r.StockOutId == key && !r.IsDeleted))
            .OrderBy(r => r.ReceivableCode)
            .ThenBy(r => r.Id)
            .ToList();
        if (receivables.Count == 0)
            return Array.Empty<StockOutDetailReceivableRowDto>();

        cancellationToken.ThrowIfCancellationRequested();
        var sellIds = receivables
            .Select(r => r.SellOrderItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sellCodeById = sellIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderItemRepo.FindAsync(i => sellIds.Contains(i.Id)))
                .Where(i => !string.IsNullOrWhiteSpace(i.SellOrderItemCode))
                .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().SellOrderItemCode!.Trim(), StringComparer.OrdinalIgnoreCase);

        return receivables.Select(r =>
        {
            var sellId = string.IsNullOrWhiteSpace(r.SellOrderItemId) ? null : r.SellOrderItemId.Trim();
            string? sellCode = null;
            if (!string.IsNullOrEmpty(sellId))
                sellCodeById.TryGetValue(sellId, out sellCode);
            return new StockOutDetailReceivableRowDto
            {
                Id = r.Id,
                ReceivableCode = r.ReceivableCode,
                SellOrderItemId = sellId,
                SellOrderItemCode = sellCode,
                OutboundQty = r.OutboundQty,
                Amount = r.Amount,
                Currency = r.Currency,
                VerifiedDone = r.VerifiedDone,
                VerifiedToBe = r.VerifiedToBe,
                VerificationStatus = r.VerificationStatus,
                InvoiceMatchDone = r.InvoiceMatchDone,
                InvoiceMatchToBe = r.InvoiceMatchToBe,
                InvoiceMatchStatus = r.InvoiceMatchStatus
            };
        }).ToList();
    }

    /// <inheritdoc />
    public Task<PagedResult<FinanceReceivable>> GetPagedAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default) =>
        _listQuery.GetPagedAsync(request, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<FinanceReceivableListItem>> GetPagedListAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _listQuery.GetPagedAsync(request, cancellationToken);
        var items = result.Items.Select(MapToListItem).ToList();
        await EnrichReceivableListItemsAsync(items, cancellationToken);
        return new PagedResult<FinanceReceivableListItem>
        {
            Items = items,
            TotalCount = result.TotalCount,
            PageIndex = result.PageIndex,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<FinanceReceivable?> GetByIdAsync(
        string id,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var receivable = await _listQuery.GetByIdScopedAsync(id, currentUserId, cancellationToken);
        if (receivable == null)
            return null;

        await EnrichReceivableDetailAsync(receivable, cancellationToken);
        receivable.StockOutMissingOrDeleted = FinanceReceivableVoidRules.IsOrphanStockOut(
            string.IsNullOrWhiteSpace(receivable.StockOutId)
                ? null
                : await _stockOutRepo.GetByIdAsync(receivable.StockOutId.Trim()));
        return receivable;
    }

    private async Task EnrichReceivableDetailAsync(
        FinanceReceivable receivable,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(receivable.CustomerId))
            return;

        cancellationToken.ThrowIfCancellationRequested();
        var cust = await _customerRepo.GetByIdAsync(receivable.CustomerId.Trim());
        if (cust == null)
            return;

        var nameZh = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
        if (!string.IsNullOrWhiteSpace(nameZh))
            receivable.CustomerName = nameZh.Trim();
        if (!string.IsNullOrWhiteSpace(cust.EnglishOfficialName))
            receivable.CustomerEnglishName = cust.EnglishOfficialName.Trim();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceReceivableWriteOffDetailItem>> GetWriteOffsByReceivableIdAsync(
        string receivableId,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(receivableId))
            return Array.Empty<FinanceReceivableWriteOffDetailItem>();

        var receivable = await GetByIdAsync(receivableId, currentUserId, cancellationToken);
        if (receivable == null)
            return Array.Empty<FinanceReceivableWriteOffDetailItem>();

        var rid = receivableId.Trim();
        var writeOffs = (await _writeOffRepo.FindAsync(w => w.FinanceReceivableId == rid))
            .OrderByDescending(w => w.CreateTime)
            .ThenByDescending(w => w.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (writeOffs.Count == 0)
            return Array.Empty<FinanceReceivableWriteOffDetailItem>();

        var receiptIds = writeOffs
            .Select(w => w.FinanceReceiptId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var receipts = receiptIds.Count == 0
            ? new Dictionary<string, FinanceReceipt>(StringComparer.OrdinalIgnoreCase)
            : (await _receiptRepo.FindAsync(r => receiptIds.Contains(r.Id)))
                .Where(r => !string.IsNullOrWhiteSpace(r.Id))
                .GroupBy(r => r.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var itemIds = writeOffs
            .Select(w => w.FinanceReceiptItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var receiptItems = itemIds.Count == 0
            ? new Dictionary<string, FinanceReceiptItem>(StringComparer.OrdinalIgnoreCase)
            : (await _receiptItemRepo.FindAsync(i => itemIds.Contains(i.Id)))
                .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var writeOffIds = writeOffs
            .Select(w => w.Id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var advanceLedgers = writeOffIds.Count == 0
            ? new Dictionary<string, FinanceCustomerAdvanceLedger>(StringComparer.OrdinalIgnoreCase)
            : (await _advanceLedgerRepo.FindAsync(l =>
                    l.FinanceReceivableWriteOffId != null && writeOffIds.Contains(l.FinanceReceivableWriteOffId)))
                .Where(l => !string.IsNullOrWhiteSpace(l.FinanceReceivableWriteOffId))
                .GroupBy(l => l.FinanceReceivableWriteOffId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = writeOffs.Select(w =>
        {
            string? receiptId = !string.IsNullOrWhiteSpace(w.FinanceReceiptId)
                ? w.FinanceReceiptId.Trim()
                : null;
            if (receiptId == null
                && !string.IsNullOrWhiteSpace(w.FinanceReceiptItemId)
                && receiptItems.TryGetValue(w.FinanceReceiptItemId.Trim(), out var item))
            {
                receiptId = item.FinanceReceiptId?.Trim();
            }

            string? receiptCode = null;
            if (receiptId != null && receipts.TryGetValue(receiptId, out var receipt))
                receiptCode = receipt.FinanceReceiptCode;

            string? remark = null;
            if (!string.IsNullOrWhiteSpace(w.FinanceReceiptItemId)
                && receiptItems.TryGetValue(w.FinanceReceiptItemId.Trim(), out var receiptItem))
            {
                remark = receiptItem.Remark;
            }
            else if (advanceLedgers.TryGetValue(w.Id.Trim(), out var ledger))
            {
                remark = ledger.Remark;
            }

            return new FinanceReceivableWriteOffDetailItem
            {
                Id = w.Id,
                Amount = w.Amount,
                WriteOffSource = w.WriteOffSource,
                CreateTime = w.CreateTime,
                FinanceReceiptId = receiptId,
                FinanceReceiptItemId = w.FinanceReceiptItemId,
                FinanceReceiptCode = receiptCode,
                OperatorUserId = w.OperatorUserId,
                Remark = remark
            };
        }).ToList();

        await EnrichReceivableWriteOffOperatorNamesAsync(result);
        return result;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinanceReceivableWriteOffLedgerItem>> GetWriteOffLedgerPagedAsync(
        FinanceReceivableWriteOffLedgerQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _listQuery.GetWriteOffLedgerPagedAsync(request, cancellationToken);
        var items = result.Items.ToList();
        await EnrichWriteOffLedgerItemsAsync(items, cancellationToken);
        return new PagedResult<FinanceReceivableWriteOffLedgerItem>
        {
            Items = items,
            TotalCount = result.TotalCount,
            PageIndex = result.PageIndex,
            PageSize = result.PageSize
        };
    }

    private const int WriteOffLedgerBySellLineMaxRows = 2000;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceReceivableWriteOffLedgerItem>> GetWriteOffLedgerBySellOrderItemIdsAsync(
        IReadOnlyList<string> sellOrderItemIds,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var lineIds = (sellOrderItemIds ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (lineIds.Count == 0)
            return Array.Empty<FinanceReceivableWriteOffLedgerItem>();

        var result = await _listQuery.GetWriteOffLedgerPagedAsync(
            new FinanceReceivableWriteOffLedgerQueryRequest
            {
                SellOrderItemIds = lineIds,
                Page = 1,
                PageSize = WriteOffLedgerBySellLineMaxRows,
                CurrentUserId = currentUserId
            },
            cancellationToken);
        var items = result.Items.ToList();
        await EnrichWriteOffLedgerItemsAsync(items, cancellationToken);
        return items;
    }

    /// <inheritdoc />
    /// <remarks>左栏 API 始终返回「有待核销收款」的客户；hasOpenReceivable 标记该客户该币别是否有未清应收。</remarks>
    public async Task<IReadOnlyList<FinanceWriteOffCustomerSummary>> GetWriteOffCustomerSummariesAsync(
        string? keyword,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        var buckets = new Dictionary<string, CustomerCurrencyWriteOffBucket>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in await BuildPendingReceiptItemCandidatesAsync(cancellationToken))
        {
            var receipt = candidate.Receipt;
            var key = BuildCustomerCurrencyKey(receipt.CustomerId, receipt.ReceiptCurrency);
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new CustomerCurrencyWriteOffBucket
                {
                    CustomerId = receipt.CustomerId.Trim(),
                    CustomerName = receipt.CustomerName,
                    Currency = receipt.ReceiptCurrency
                };
                buckets[key] = bucket;
            }

            bucket.PendingReceiptItemCount++;
            bucket.PendingWriteOffAmount += candidate.RemainingAmount;
            if (string.IsNullOrWhiteSpace(bucket.CustomerName) && !string.IsNullOrWhiteSpace(receipt.CustomerName))
                bucket.CustomerName = receipt.CustomerName;

            if (receipt.ReceiptDate.HasValue)
                bucket.TrackReceiptDate(receipt.ReceiptDate.Value);
        }

        var openReceivableKeys = (await GetPagedAsync(new FinanceReceivableQueryRequest
        {
            OnlyOpen = true,
            Page = 1,
            PageSize = 2000,
            CurrentUserId = currentUserId
        }, cancellationToken)).Items
            .Select(r => BuildCustomerCurrencyKey(r.CustomerId, r.Currency))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var summaries = buckets.Values
            .Where(bucket => bucket.PendingReceiptItemCount > 0 && bucket.PendingWriteOffAmount > 0m)
            .Select(bucket => new FinanceWriteOffCustomerSummary
            {
                CustomerId = bucket.CustomerId,
                CustomerName = bucket.CustomerName,
                Currency = bucket.Currency,
                PendingWriteOffTotal = bucket.PendingWriteOffAmount,
                IsMultiCurrency = false,
                CurrencyTotals =
                [
                    new FinanceWriteOffCustomerCurrencyTotal
                    {
                        Currency = bucket.Currency,
                        Amount = bucket.PendingWriteOffAmount
                    }
                ],
                PendingReceiptItemCount = bucket.PendingReceiptItemCount,
                EarliestReceiptDate = bucket.EarliestReceiptDate,
                LatestReceiptDate = bucket.LatestReceiptDate,
                HasOpenReceivable = openReceivableKeys.Contains(
                    BuildCustomerCurrencyKey(bucket.CustomerId, bucket.Currency))
            })
            .AsEnumerable();

        var list = summaries
            .OrderBy(r => r.CustomerName ?? r.CustomerId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Currency)
            .ToList();
        await EnrichWriteOffCustomerSummariesAsync(list, cancellationToken);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            list = list
                .Where(r => MatchesWriteOffCustomerKeyword(r, k))
                .ToList();
        }

        return list;
    }

    private static bool MatchesWriteOffCustomerKeyword(FinanceWriteOffCustomerSummary row, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return true;

        return ContainsIgnoreCase(row.CustomerName, keyword)
            || ContainsIgnoreCase(row.CustomerEnglishName, keyword)
            || ContainsIgnoreCase(row.CustomerCode, keyword)
            || ContainsIgnoreCase(row.SalesUserName, keyword);
    }

    private static bool ContainsIgnoreCase(string? value, string keyword) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private async Task EnrichWriteOffCustomerSummariesAsync(
        List<FinanceWriteOffCustomerSummary> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var ids = items
            .Select(i => i.CustomerId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (ids.Length == 0)
            return;

        var customers = (await _customerRepo.FindAsync(c => ids.Contains(c.Id))).ToList();
        var byId = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.Id))
            .GroupBy(c => c.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var salesUserIds = customers
            .Select(c => c.SalesUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var salesUserMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (salesUserIds.Count > 0)
        {
            var users = (await _userRepository.FindAsync(u => salesUserIds.Contains(u.Id))).ToList();
            salesUserMap = users
                .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                .GroupBy(u => u.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                    StringComparer.OrdinalIgnoreCase);
        }

        foreach (var row in items)
        {
            if (string.IsNullOrWhiteSpace(row.CustomerId))
                continue;
            if (!byId.TryGetValue(row.CustomerId.Trim(), out var cust))
                continue;

            var nameZh = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
            if (!string.IsNullOrWhiteSpace(nameZh))
                row.CustomerName = nameZh.Trim();
            if (!string.IsNullOrWhiteSpace(cust.EnglishOfficialName))
                row.CustomerEnglishName = cust.EnglishOfficialName.Trim();
            if (!string.IsNullOrWhiteSpace(cust.CustomerCode))
                row.CustomerCode = cust.CustomerCode.Trim();
            if (!string.IsNullOrWhiteSpace(cust.SalesUserId))
            {
                row.SalesUserId = cust.SalesUserId.Trim();
                if (salesUserMap.TryGetValue(row.SalesUserId, out var salesUserName))
                    row.SalesUserName = salesUserName;
            }
        }
    }

    /// <inheritdoc />
    public async Task<FinanceReceivableWriteOffCandidates> GetWriteOffCandidatesAsync(
        string customerId,
        string? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("客户ID不能为空", nameof(customerId));

        var cid = customerId.Trim();
        var receivables = await GetPagedAsync(new FinanceReceivableQueryRequest
        {
            CustomerId = cid,
            OnlyOpen = true,
            Page = 1,
            PageSize = 2000,
            CurrentUserId = currentUserId
        }, cancellationToken);

        var pending = (await BuildPendingReceiptItemCandidatesAsync(cancellationToken))
            .Where(c => string.Equals(c.Receipt.CustomerId, cid, StringComparison.OrdinalIgnoreCase))
            .Select(MapReceiptItemCandidate)
            .OrderByDescending(c => c.ReceiptDate ?? c.Item.CreateTime)
            .ThenByDescending(c => c.Item.CreateTime)
            .ToList();

        var receivableRows = receivables.Items
            .Select(MapReceivableCandidateRow)
            .ToList();
        await EnrichReceivableCandidateRowsAsync(receivableRows, cancellationToken);

        var advances = (await _advanceService.GetBalancesForCustomerAsync(cid))
            .Where(a => a.Balance > 0m)
            .ToList();

        return new FinanceReceivableWriteOffCandidates
        {
            ReceiptItems = pending,
            Receivables = receivableRows,
            AdvanceBalances = advances.Where(a => a.Balance > 0m).ToList()
        };
    }

    private async Task<List<PendingReceiptItemCandidate>> BuildPendingReceiptItemCandidatesAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var receiptItems = (await _receiptItemRepo.GetAllAsync()).ToList();
        var eligibleReceipts = (await _receiptRepo.FindAsync(r =>
            !r.IsDeleted && (r.Status == FinanceReceiptStatusCode.Confirmed
                             || r.Status == FinanceReceiptStatusCode.LegacyApproved))).ToList();

        var mutated = false;
        foreach (var receipt in eligibleReceipts)
        {
            var hasActiveItem = receiptItems.Exists(i =>
                !i.IsDeleted
                && string.Equals(i.FinanceReceiptId, receipt.Id, StringComparison.OrdinalIgnoreCase));
            if (hasActiveItem || receipt.ReceiptAmount <= 0m)
                continue;

            var materialized = new FinanceReceiptItem
            {
                Id = Guid.NewGuid().ToString(),
                FinanceReceiptId = receipt.Id,
                ReceiptAmount = receipt.ReceiptAmount,
                ReceiptConvertAmount = receipt.ReceiptAmount,
                VerificationStatus = 0,
                CreateTime = DateTime.UtcNow
            };
            await _receiptItemRepo.AddAsync(materialized);
            receiptItems.Add(materialized);
            mutated = true;
        }

        if (mutated && _unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        var receiptMap = eligibleReceipts
            .Where(r => !string.IsNullOrWhiteSpace(r.Id))
            .GroupBy(r => r.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = new List<PendingReceiptItemCandidate>();
        foreach (var item in receiptItems)
        {
            if (item.IsDeleted)
                continue;
            if (!receiptMap.TryGetValue(item.FinanceReceiptId, out var receipt))
                continue;

            var remaining = FinanceReceiptItemWriteOffHelper.GetRemaining(item);
            if (remaining <= 0m)
                continue;

            result.Add(new PendingReceiptItemCandidate
            {
                Item = item,
                Receipt = receipt,
                RemainingAmount = remaining
            });
        }

        return result;
    }

    private static FinanceReceiptItemWriteOffCandidate MapReceiptItemCandidate(PendingReceiptItemCandidate source) =>
        new()
        {
            Item = source.Item,
            FinanceReceiptCode = source.Receipt.FinanceReceiptCode,
            ReceiptStatus = source.Receipt.Status,
            RemainingAmount = source.RemainingAmount,
            ReceiptPurpose = source.Item.ReceiptPurpose,
            AdvanceSellOrderId = string.IsNullOrWhiteSpace(source.Item.AdvanceSellOrderId)
                ? source.Item.SellOrderId
                : source.Item.AdvanceSellOrderId,
            ReceiptDate = source.Receipt.ReceiptDate,
            ReceiptAmount = source.Item.ReceiptAmount,
            ReceiptCurrency = source.Receipt.ReceiptCurrency,
            ReceiptMode = source.Receipt.ReceiptMode,
            Remark = source.Item.Remark
        };

    private static FinanceReceivableWriteOffCandidateRow MapReceivableCandidateRow(FinanceReceivable receivable) =>
        new()
        {
            Id = receivable.Id,
            ReceivableCode = receivable.ReceivableCode,
            StockOutId = receivable.StockOutId,
            StockOutCode = receivable.StockOutCode,
            SellOrderId = receivable.SellOrderId,
            SellOrderCode = receivable.SellOrderCode,
            SellOrderItemId = receivable.SellOrderItemId,
            CustomerId = receivable.CustomerId,
            CustomerName = receivable.CustomerName,
            CustomerEnglishName = receivable.CustomerEnglishName,
            SalesUserId = receivable.SalesUserId,
            PN = receivable.PN,
            Brand = receivable.Brand,
            OutboundQty = receivable.OutboundQty,
            UnitPrice = receivable.UnitPrice,
            Currency = receivable.Currency,
            Amount = receivable.Amount,
            VerifiedDone = receivable.VerifiedDone,
            VerifiedToBe = receivable.VerifiedToBe,
            VerificationStatus = receivable.VerificationStatus,
            StockOutDate = receivable.StockOutDate
        };

    private static FinanceReceivableListItem MapToListItem(FinanceReceivable receivable) =>
        new()
        {
            Id = receivable.Id,
            ReceivableCode = receivable.ReceivableCode,
            StockOutId = receivable.StockOutId,
            StockOutCode = receivable.StockOutCode,
            SellOrderId = receivable.SellOrderId,
            SellOrderCode = receivable.SellOrderCode,
            SellOrderItemId = receivable.SellOrderItemId,
            CustomerId = receivable.CustomerId,
            CustomerName = receivable.CustomerName,
            SalesUserId = receivable.SalesUserId,
            PN = receivable.PN,
            Brand = receivable.Brand,
            OutboundQty = receivable.OutboundQty,
            UnitPrice = receivable.UnitPrice,
            Currency = receivable.Currency,
            Amount = receivable.Amount,
            VerifiedDone = receivable.VerifiedDone,
            VerifiedToBe = receivable.VerifiedToBe,
            VerificationStatus = receivable.VerificationStatus,
            InvoiceMatchDone = receivable.InvoiceMatchDone,
            InvoiceMatchToBe = receivable.InvoiceMatchToBe,
            InvoiceMatchStatus = receivable.InvoiceMatchStatus,
            StockOutDate = receivable.StockOutDate,
            CreateTime = receivable.CreateTime
        };

    private async Task EnrichReceivableListItemsAsync(
        List<FinanceReceivableListItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var customerIds = items
            .Select(i => i.CustomerId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var customers = customerIds.Length == 0
            ? new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _customerRepo.FindAsync(c => customerIds.Contains(c.Id)))
                .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                .GroupBy(c => c.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var sellOrderIds = items
            .Select(i => i.SellOrderId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sellOrders = sellOrderIds.Count == 0
            ? new Dictionary<string, SellOrder>(StringComparer.OrdinalIgnoreCase)
            : (await _sellOrderRepo.FindAsync(o => sellOrderIds.Contains(o.Id)))
                .Where(o => !string.IsNullOrWhiteSpace(o.Id))
                .GroupBy(o => o.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var salesUserIds = items
            .Select(i => i.SalesUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Concat(sellOrders.Values
                .Select(o => o.SalesUserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim()))
            .Concat(customers.Values
                .Select(c => c.SalesUserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var salesUserMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (salesUserIds.Count > 0)
        {
            var users = (await _userRepository.FindAsync(u => salesUserIds.Contains(u.Id))).ToList();
            salesUserMap = users
                .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                .GroupBy(u => u.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                    StringComparer.OrdinalIgnoreCase);
        }

        foreach (var row in items)
        {
            SellOrder? sellOrder = null;
            if (!string.IsNullOrWhiteSpace(row.SellOrderId))
                sellOrders.TryGetValue(row.SellOrderId.Trim(), out sellOrder);

            CustomerInfo? cust = null;
            if (!string.IsNullOrWhiteSpace(row.CustomerId))
                customers.TryGetValue(row.CustomerId.Trim(), out cust);

            if (cust != null)
            {
                var nameZh = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                if (!string.IsNullOrWhiteSpace(nameZh))
                    row.CustomerName = nameZh.Trim();
                if (!string.IsNullOrWhiteSpace(cust.EnglishOfficialName))
                    row.CustomerEnglishName = cust.EnglishOfficialName.Trim();
                if (!string.IsNullOrWhiteSpace(cust.CustomerCode))
                    row.CustomerCode = cust.CustomerCode.Trim();
            }

            if (string.IsNullOrWhiteSpace(row.SalesUserId))
            {
                if (!string.IsNullOrWhiteSpace(sellOrder?.SalesUserId))
                    row.SalesUserId = sellOrder.SalesUserId.Trim();
                else if (cust != null && !string.IsNullOrWhiteSpace(cust.SalesUserId))
                    row.SalesUserId = cust.SalesUserId.Trim();
            }

            string? salesUserName = null;
            if (!string.IsNullOrWhiteSpace(row.SalesUserId)
                && salesUserMap.TryGetValue(row.SalesUserId.Trim(), out var login)
                && !string.IsNullOrWhiteSpace(login))
            {
                salesUserName = login;
            }
            else if (!string.IsNullOrWhiteSpace(sellOrder?.SalesUserName))
            {
                salesUserName = sellOrder.SalesUserName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(salesUserName))
                row.SalesUserName = salesUserName;
        }
    }

    private static string BuildCustomerCurrencyKey(string customerId, short currency) =>
        $"{customerId.Trim()}::{currency}";

    private static string BuildCustomerCurrencyKey(string customerId, byte currency) =>
        BuildCustomerCurrencyKey(customerId, (short)currency);

    private sealed class CustomerCurrencyWriteOffBucket
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public short Currency { get; set; }
        public int PendingReceiptItemCount { get; set; }
        public decimal PendingWriteOffAmount { get; set; }
        public DateTime? EarliestReceiptDate { get; private set; }
        public DateTime? LatestReceiptDate { get; private set; }

        public void TrackReceiptDate(DateTime date)
        {
            if (!EarliestReceiptDate.HasValue || date < EarliestReceiptDate.Value)
                EarliestReceiptDate = date;
            if (!LatestReceiptDate.HasValue || date > LatestReceiptDate.Value)
                LatestReceiptDate = date;
        }
    }

    private sealed class PendingReceiptItemCandidate
    {
        public FinanceReceiptItem Item { get; init; } = null!;
        public FinanceReceipt Receipt { get; init; } = null!;
        public decimal RemainingAmount { get; init; }
    }

    private async Task EnrichReceivableCandidateRowsAsync(
        List<FinanceReceivableWriteOffCandidateRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var salesUserIds = rows
            .Select(r => r.SalesUserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (salesUserIds.Count > 0)
        {
            var users = (await _userRepository.FindAsync(u => salesUserIds.Contains(u.Id))).ToList();
            var userMap = users
                .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                .GroupBy(u => u.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.SalesUserId)
                    && userMap.TryGetValue(row.SalesUserId.Trim(), out var name))
                {
                    row.SalesUserName = name;
                }
            }
        }

        var stockOutIds = rows
            .Select(r => r.StockOutId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (stockOutIds.Count == 0)
            return;

        var stockOutItems = (await _stockOutItemRepo.FindAsync(i => stockOutIds.Contains(i.StockOutId)))
            .Where(i => !i.IsDeleted)
            .ToList();
        if (stockOutItems.Count == 0)
            return;

        var stockOutItemIds = stockOutItems
            .Select(i => i.Id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var extends = (await _stockOutItemExtendRepo.FindAsync(e => stockOutItemIds.Contains(e.Id)))
            .Where(e => !e.IsDeleted)
            .ToList();
        var extendByItemId = extends
            .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var stockInItemIds = extends
            .Select(e => e.StockInItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var stockInItems = stockInItemIds.Count == 0
            ? new List<StockInItem>()
            : (await _stockInItemRepo.FindAsync(i => stockInItemIds.Contains(i.Id)))
                .Where(i => !i.IsDeleted)
                .ToList();
        var stockInIds = stockInItems
            .Select(i => i.StockInId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var stockIns = stockInIds.Count == 0
            ? new List<StockIn>()
            : (await _stockInRepo.FindAsync(i => stockInIds.Contains(i.Id)))
                .Where(i => !i.IsDeleted)
                .ToList();
        var stockInById = stockIns
            .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var stockInCodeByItemId = stockInItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Id))
            .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var stockInId = g.First().StockInId?.Trim();
                    if (string.IsNullOrEmpty(stockInId) || !stockInById.TryGetValue(stockInId, out var stockIn))
                        return null;
                    return stockIn.StockInCode;
                },
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
        var purchaseOrderIds = purchaseOrderItems
            .Select(i => i.PurchaseOrderId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var purchaseOrders = purchaseOrderIds.Count == 0
            ? new List<PurchaseOrder>()
            : (await _purchaseOrderRepo.FindAsync(p => purchaseOrderIds.Contains(p.Id))).ToList();
        var poById = purchaseOrders
            .GroupBy(p => p.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var poiById = purchaseOrderItems
            .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var stockInCodesByStockOutId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var freightByStockOutId = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var outItem in stockOutItems)
        {
            var stockOutId = outItem.StockOutId.Trim();
            if (!extendByItemId.TryGetValue(outItem.Id.Trim(), out var extend))
                continue;

            if (!string.IsNullOrWhiteSpace(extend.StockInItemId)
                && stockInCodeByItemId.TryGetValue(extend.StockInItemId.Trim(), out var stockInCode)
                && !string.IsNullOrWhiteSpace(stockInCode))
            {
                if (!stockInCodesByStockOutId.TryGetValue(stockOutId, out var stockInCodes))
                {
                    stockInCodes = new List<string>();
                    stockInCodesByStockOutId[stockOutId] = stockInCodes;
                }

                if (!stockInCodes.Contains(stockInCode, StringComparer.OrdinalIgnoreCase))
                    stockInCodes.Add(stockInCode);
            }

            var freight = FreightForwarderOrderNoLookup.FromPurchaseOrderItemId(
                extend.PurchaseOrderItemId,
                poiById,
                poById);
            if (string.IsNullOrWhiteSpace(freight))
                continue;

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
            var stockOutId = row.StockOutId.Trim();
            if (stockInCodesByStockOutId.TryGetValue(stockOutId, out var stockInCodes) && stockInCodes.Count > 0)
                row.StockInCode = FreightForwarderOrderNoDisplay.JoinDistinct(stockInCodes);
            if (freightByStockOutId.TryGetValue(stockOutId, out var freightNos) && freightNos.Count > 0)
                row.FreightForwarderOrderNo = FreightForwarderOrderNoDisplay.JoinDistinct(freightNos);
        }
    }

    private static decimal GetReceiptItemRemaining(FinanceReceiptItem item) =>
        FinanceReceiptItemWriteOffHelper.GetRemaining(item);

    /// <inheritdoc />
    public async Task<FinanceReceivableWriteOffResult> ApplyWriteOffAsync(
        FinanceReceivableWriteOffRequest request,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var result = new FinanceReceivableWriteOffResult();
        var itemAllocs = (request.Allocations ?? new List<FinanceReceivableWriteOffAllocation>())
            .Where(a => a.Amount > 0m
                && !string.IsNullOrWhiteSpace(a.FinanceReceiptItemId)
                && !string.IsNullOrWhiteSpace(a.FinanceReceivableId))
            .Select(a => new FinanceReceivableWriteOffAllocation
            {
                FinanceReceiptItemId = a.FinanceReceiptItemId.Trim(),
                FinanceReceivableId = a.FinanceReceivableId.Trim(),
                Amount = a.Amount
            }).ToList();

        var poolAllocs = (request.AdvancePoolAllocations ?? new List<FinanceAdvancePoolAllocation>())
            .Where(a => a.Amount > 0m && !string.IsNullOrWhiteSpace(a.FinanceReceivableId))
            .Select(a => new FinanceAdvancePoolAllocation
            {
                FinanceReceivableId = a.FinanceReceivableId.Trim(),
                Amount = a.Amount,
                AdvanceSellOrderId = a.AdvanceSellOrderId?.Trim()
            }).ToList();

        if (itemAllocs.Count == 0 && poolAllocs.Count == 0)
            throw new ArgumentException("请至少填写一条核销明细");

        var receivableIds = itemAllocs.Select(a => a.FinanceReceivableId)
            .Concat(poolAllocs.Select(a => a.FinanceReceivableId))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var receivables = new Dictionary<string, FinanceReceivable>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in receivableIds)
        {
            var recv = await _receivableRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"应收款 {id} 不存在");
            if (recv.IsDeleted)
                throw new InvalidOperationException($"应收款 {recv.ReceivableCode ?? recv.StockOutCode} 已失效");
            receivables[id] = recv;
        }

        var receiptItems = new Dictionary<string, FinanceReceiptItem>(StringComparer.OrdinalIgnoreCase);
        var receipts = new Dictionary<string, FinanceReceipt>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in itemAllocs.Select(a => a.FinanceReceiptItemId).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var item = await _receiptItemRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"收款明细 {id} 不存在");
            receiptItems[id] = item;
            if (!receipts.ContainsKey(item.FinanceReceiptId))
            {
                var receipt = await _receiptRepo.GetByIdAsync(item.FinanceReceiptId)
                    ?? throw new InvalidOperationException($"收款单 {item.FinanceReceiptId} 不存在");
                if (!FinanceReceiptStatusCode.IsConfirmed(receipt.Status))
                    throw new InvalidOperationException($"收款单 {receipt.FinanceReceiptCode} 未确认，不可核销");
                receipts[item.FinanceReceiptId] = receipt;
            }
        }

        string? customerId = null;
        short currency = 1;
        if (receipts.Count > 0)
        {
            customerId = receipts.Values.First().CustomerId;
            currency = (short)receipts.Values.First().ReceiptCurrency;
            if (receipts.Values.Any(r => !string.Equals(r.CustomerId, customerId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("一次核销不可跨多个客户收款单");
        }
        else
        {
            customerId = receivables.Values.First().CustomerId;
            currency = receivables.Values.First().Currency;
        }

        if (receivables.Values.Any(r => !string.Equals(r.CustomerId, customerId, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("应收款客户不一致");

        if (receipts.Count > 0 && receivables.Values.Any(r => r.Currency != currency))
            throw new InvalidOperationException("收款币别与应收款币别不一致");

        var soMismatches = CollectSoMismatches(itemAllocs, poolAllocs, receiptItems, receivables);
        if (soMismatches.Count > 0 && !request.ConfirmSoMismatch)
        {
            result.RequiresSoMismatchConfirm = true;
            result.SoMismatches = soMismatches;
            return result;
        }

        var receiptRemaining = receiptItems.ToDictionary(
            kv => kv.Key,
            kv => GetReceiptItemRemaining(kv.Value),
            StringComparer.OrdinalIgnoreCase);
        var receivableRemaining = receivables.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.VerifiedToBe,
            StringComparer.OrdinalIgnoreCase);

        var poolTotal = poolAllocs.Sum(a => a.Amount);
        if (poolTotal > 0m)
        {
            var balance = await _advanceService.GetBalanceAsync(customerId!, currency);
            if (balance == null || balance.Balance + 0.0001m < poolTotal)
                throw new InvalidOperationException($"客户预收余额不足（可用 {balance?.Balance ?? 0m}，本次 {poolTotal}）");
        }

        var touchedLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alloc in itemAllocs)
        {
            if (alloc.Amount > receiptRemaining[alloc.FinanceReceiptItemId])
                throw new InvalidOperationException($"收款明细可核销余额不足（剩余 {receiptRemaining[alloc.FinanceReceiptItemId]}）");
            if (alloc.Amount > receivableRemaining[alloc.FinanceReceivableId])
                throw new InvalidOperationException($"应收款待核销余额不足（剩余 {receivableRemaining[alloc.FinanceReceivableId]}）");

            receiptRemaining[alloc.FinanceReceiptItemId] -= alloc.Amount;
            receivableRemaining[alloc.FinanceReceivableId] -= alloc.Amount;

            var receiptItem = receiptItems[alloc.FinanceReceiptItemId];
            var receivable = receivables[alloc.FinanceReceivableId];
            var receipt = receipts[receiptItem.FinanceReceiptId];

            await ApplyReceivableWriteOffCoreAsync(
                receivable, receipt, receiptItem, alloc.Amount,
                FinanceReceivableWriteOffSourceCode.ReceiptItem, actingUserId);

            touchedLines.Add(receivable.SellOrderItemId);
        }

        foreach (var alloc in poolAllocs)
        {
            if (alloc.Amount > receivableRemaining[alloc.FinanceReceivableId])
                throw new InvalidOperationException($"应收款待核销余额不足（剩余 {receivableRemaining[alloc.FinanceReceivableId]}）");

            receivableRemaining[alloc.FinanceReceivableId] -= alloc.Amount;
            var receivable = receivables[alloc.FinanceReceivableId];
            var writeOffId = Guid.NewGuid().ToString();

            await _advanceService.ApplyFromPoolAsync(
                customerId!,
                currency,
                alloc.Amount,
                receivable,
                writeOffId,
                alloc.AdvanceSellOrderId,
                actingUserId,
                soMismatches.Count > 0 ? "SO不匹配已确认" : null);

            receivable.VerifiedDone += alloc.Amount;
            receivable.VerifiedToBe = Math.Max(0m, receivable.Amount - receivable.VerifiedDone);
            receivable.VerificationStatus = FinanceVerificationStatusCode.Resolve(
                receivable.Amount, receivable.VerifiedDone);
            receivable.ModifyTime = DateTime.UtcNow;
            receivable.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _receivableRepo.UpdateAsync(receivable);

            await _writeOffRepo.AddAsync(new FinanceReceivableWriteOff
            {
                Id = writeOffId,
                FinanceReceivableId = receivable.Id,
                WriteOffSource = FinanceReceivableWriteOffSourceCode.AdvancePool,
                Amount = alloc.Amount,
                OperatorUserId = ActingUserIdNormalizer.Normalize(actingUserId),
                CreateTime = DateTime.UtcNow
            });

            touchedLines.Add(receivable.SellOrderItemId);
        }

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        foreach (var lineId in touchedLines)
            await _sellOrderItemExtendSync.RecalculateAsync(lineId);

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        if (_sellInvoiceWriteOffService != null && receivableIds.Count > 0)
            await _sellInvoiceWriteOffService.RecalculateReceiveProgressForReceivablesAsync(receivableIds, cancellationToken);

        result.Applied = true;
        result.SoMismatches = soMismatches;
        return result;
    }

    private async Task ApplyReceivableWriteOffCoreAsync(
        FinanceReceivable receivable,
        FinanceReceipt receipt,
        FinanceReceiptItem receiptItem,
        decimal amount,
        short writeOffSource,
        string? actingUserId)
    {
        receiptItem.VerifiedAmount += amount;
        receiptItem.VerificationStatus = FinanceVerificationStatusCode.Resolve(
            receiptItem.ReceiptConvertAmount, receiptItem.VerifiedAmount);
        receiptItem.ModifyTime = DateTime.UtcNow;
        await _receiptItemRepo.UpdateAsync(receiptItem);

        receivable.VerifiedDone += amount;
        receivable.VerifiedToBe = Math.Max(0m, receivable.Amount - receivable.VerifiedDone);
        receivable.VerificationStatus = FinanceVerificationStatusCode.Resolve(
            receivable.Amount, receivable.VerifiedDone);
        receivable.ModifyTime = DateTime.UtcNow;
        receivable.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _receivableRepo.UpdateAsync(receivable);

        await _writeOffRepo.AddAsync(new FinanceReceivableWriteOff
        {
            Id = Guid.NewGuid().ToString(),
            FinanceReceivableId = receivable.Id,
            FinanceReceiptId = receipt.Id,
            FinanceReceiptItemId = receiptItem.Id,
            WriteOffSource = writeOffSource,
            Amount = amount,
            OperatorUserId = ActingUserIdNormalizer.Normalize(actingUserId),
            CreateTime = DateTime.UtcNow
        });
    }

    private static List<FinanceReceivableWriteOffSoMismatch> CollectSoMismatches(
        List<FinanceReceivableWriteOffAllocation> itemAllocs,
        List<FinanceAdvancePoolAllocation> poolAllocs,
        Dictionary<string, FinanceReceiptItem> receiptItems,
        Dictionary<string, FinanceReceivable> receivables)
    {
        var list = new List<FinanceReceivableWriteOffSoMismatch>();
        foreach (var alloc in itemAllocs)
        {
            if (!receiptItems.TryGetValue(alloc.FinanceReceiptItemId, out var item)
                || !receivables.TryGetValue(alloc.FinanceReceivableId, out var recv))
                continue;
            var advSo = string.IsNullOrWhiteSpace(item.AdvanceSellOrderId) ? item.SellOrderId : item.AdvanceSellOrderId;
            if (string.IsNullOrWhiteSpace(advSo) || string.IsNullOrWhiteSpace(recv.SellOrderId))
                continue;
            if (string.Equals(advSo.Trim(), recv.SellOrderId.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add(new FinanceReceivableWriteOffSoMismatch
            {
                FinanceReceivableId = recv.Id,
                ReceivableSellOrderId = recv.SellOrderId,
                AdvanceSellOrderId = advSo,
                Message = $"预收挂销售单与应收销售单不一致（预收 SO={advSo}，应收 SO={recv.SellOrderId}）"
            });
        }

        foreach (var alloc in poolAllocs)
        {
            if (string.IsNullOrWhiteSpace(alloc.AdvanceSellOrderId)
                || !receivables.TryGetValue(alloc.FinanceReceivableId, out var recv))
                continue;
            if (string.IsNullOrWhiteSpace(recv.SellOrderId))
                continue;
            if (string.Equals(alloc.AdvanceSellOrderId.Trim(), recv.SellOrderId.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add(new FinanceReceivableWriteOffSoMismatch
            {
                FinanceReceivableId = recv.Id,
                ReceivableSellOrderId = recv.SellOrderId,
                AdvanceSellOrderId = alloc.AdvanceSellOrderId,
                Message = $"预收池核销：挂单 SO={alloc.AdvanceSellOrderId} 与应收 SO={recv.SellOrderId} 不一致"
            });
        }

        return list
            .GroupBy(m => m.FinanceReceivableId + "|" + m.Message, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static decimal ResolveReceivableAmount(StockOut stockOut, SellOrderItem soItem)
    {
        if (stockOut.TotalAmount > 0m)
            return Math.Round(stockOut.TotalAmount, 2, MidpointRounding.AwayFromZero);
        return Math.Round(stockOut.TotalQuantity * soItem.Price, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 按销售行汇总本出库单应收数量：优先明细扩展；无扩展销售行时回退头表单一销售行（历史单）。
    /// </summary>
    private async Task<Dictionary<string, int>> ResolveReceivableLineQuantitiesAsync(StockOut stockOut)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var items = (await _stockOutItemRepo.FindAsync(i =>
                !i.IsDeleted && i.StockOutId == stockOut.Id))
            .ToList();
        if (items.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(stockOut.SellOrderItemId) && stockOut.TotalQuantity > 0)
                result[stockOut.SellOrderItemId.Trim()] = stockOut.TotalQuantity;
            return result;
        }

        var itemIds = items.Select(i => i.Id.Trim()).ToList();
        var extends = (await _stockOutItemExtendRepo.FindAsync(e =>
                !e.IsDeleted && itemIds.Contains(e.Id)))
            .ToList();
        var extByItemId = extends
            .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (!extByItemId.TryGetValue(item.Id.Trim(), out var ext))
                continue;
            var lineId = ext.SellOrderItemId?.Trim();
            if (string.IsNullOrEmpty(lineId))
                continue;
            var qty = ext.QtyStockOut > 0
                ? ext.QtyStockOut
                : (item.ActualQty > 0 ? item.ActualQty : item.Quantity);
            if (qty <= 0)
                continue;
            result.TryGetValue(lineId, out var prev);
            result[lineId] = prev + qty;
        }

        if (result.Count == 0
            && !string.IsNullOrWhiteSpace(stockOut.SellOrderItemId)
            && stockOut.TotalQuantity > 0)
        {
            result[stockOut.SellOrderItemId.Trim()] = stockOut.TotalQuantity;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinanceReceivableWriteOffListItem>> GetWriteOffsByReceiptIdAsync(
        string receiptId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(receiptId))
            return Array.Empty<FinanceReceivableWriteOffListItem>();

        var rid = receiptId.Trim();
        var receipt = await _receiptRepo.GetByIdAsync(rid);
        if (receipt == null)
            return Array.Empty<FinanceReceivableWriteOffListItem>();

        var itemIds = (await _receiptItemRepo.FindAsync(i => i.FinanceReceiptId == rid))
            .Select(i => i.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var writeOffs = (await _writeOffRepo.FindAsync(w =>
                (w.FinanceReceiptId != null && w.FinanceReceiptId == rid)
                || (w.FinanceReceiptItemId != null && itemIds.Contains(w.FinanceReceiptItemId.Trim()))))
            .OrderByDescending(w => w.CreateTime)
            .ThenByDescending(w => w.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (writeOffs.Count == 0)
            return Array.Empty<FinanceReceivableWriteOffListItem>();

        var receivableIds = writeOffs
            .Select(w => w.FinanceReceivableId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var receivables = (await _receivableRepo.FindAsync(r => receivableIds.Contains(r.Id)))
            .Where(r => !r.IsDeleted)
            .GroupBy(r => r.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = writeOffs.Select(w =>
        {
            receivables.TryGetValue(w.FinanceReceivableId.Trim(), out var rec);
            return new FinanceReceivableWriteOffListItem
            {
                Id = w.Id,
                Amount = w.Amount,
                WriteOffSource = w.WriteOffSource,
                CreateTime = w.CreateTime,
                FinanceReceiptItemId = w.FinanceReceiptItemId,
                FinanceReceivableId = w.FinanceReceivableId,
                ReceivableCode = rec?.ReceivableCode,
                StockOutCode = rec?.StockOutCode,
                SellOrderCode = rec?.SellOrderCode,
                PN = rec?.PN,
                Brand = rec?.Brand,
                Currency = rec?.Currency ?? 1,
                OperatorUserId = w.OperatorUserId
            };
        }).ToList();

        await EnrichWriteOffOperatorNamesAsync(result);
        return result;
    }

    /// <inheritdoc />
    public async Task<FinanceReceiptReverseWriteOffResult> ReverseWriteOffsByReceiptAsync(
        string receiptId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(receiptId))
            throw new ArgumentException("收款单ID不能为空", nameof(receiptId));

        var rid = receiptId.Trim();
        var itemIds = (await _receiptItemRepo.FindAsync(i => i.FinanceReceiptId == rid))
            .Select(i => i.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var writeOffs = (await _writeOffRepo.FindAsync(w =>
                w.WriteOffSource == FinanceReceivableWriteOffSourceCode.ReceiptItem
                && ((w.FinanceReceiptId != null && w.FinanceReceiptId == rid)
                    || (w.FinanceReceiptItemId != null && itemIds.Contains(w.FinanceReceiptItemId.Trim())))))
            .OrderByDescending(w => w.CreateTime)
            .ThenByDescending(w => w.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var touchedLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var receivableCodes = new List<string>();
        var stockOutCodes = new List<string>();

        foreach (var writeOff in writeOffs)
        {
            var receivable = await _receivableRepo.GetByIdAsync(writeOff.FinanceReceivableId);
            if (receivable == null || receivable.IsDeleted)
                continue;

            var amount = writeOff.Amount;
            receivable.VerifiedDone = Math.Max(0m, receivable.VerifiedDone - amount);
            receivable.VerifiedToBe = Math.Max(0m, receivable.Amount - receivable.VerifiedDone);
            receivable.VerificationStatus = FinanceVerificationStatusCode.Resolve(
                receivable.Amount, receivable.VerifiedDone);
            receivable.ModifyTime = DateTime.UtcNow;
            receivable.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _receivableRepo.UpdateAsync(receivable);

            if (!string.IsNullOrWhiteSpace(writeOff.FinanceReceiptItemId))
            {
                var receiptItem = await _receiptItemRepo.GetByIdAsync(writeOff.FinanceReceiptItemId);
                if (receiptItem != null)
                {
                    receiptItem.VerifiedAmount = Math.Max(0m, receiptItem.VerifiedAmount - amount);
                    receiptItem.VerificationStatus = FinanceVerificationStatusCode.Resolve(
                        FinanceReceiptItemWriteOffHelper.EffectiveConvertAmount(receiptItem),
                        receiptItem.VerifiedAmount);
                    receiptItem.ModifyTime = DateTime.UtcNow;
                    await _receiptItemRepo.UpdateAsync(receiptItem);
                }
            }

            await _writeOffRepo.DeleteAsync(writeOff.Id); // ISoftDeletable：软删流水，核销页不再展示

            touchedLines.Add(receivable.SellOrderItemId);
            if (receivableCodes.Count < 5 && !string.IsNullOrWhiteSpace(receivable.ReceivableCode))
                receivableCodes.Add(receivable.ReceivableCode.Trim());
            if (stockOutCodes.Count < 5 && !string.IsNullOrWhiteSpace(receivable.StockOutCode))
                stockOutCodes.Add(receivable.StockOutCode.Trim());
        }

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        foreach (var lineId in touchedLines)
            await _sellOrderItemExtendSync.RecalculateAsync(lineId);

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        if (_sellInvoiceWriteOffService != null && writeOffs.Count > 0)
        {
            var touchedReceivableIds = writeOffs
                .Select(w => w.FinanceReceivableId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            await _sellInvoiceWriteOffService.RecalculateReceiveProgressForReceivablesAsync(
                touchedReceivableIds, cancellationToken);
        }

        return new FinanceReceiptReverseWriteOffResult
        {
            WriteOffCount = writeOffs.Count,
            ReceivableCodes = receivableCodes
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            StockOutCodes = stockOutCodes
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private async Task EnrichWriteOffOperatorNamesAsync(IReadOnlyList<FinanceReceivableWriteOffListItem> items)
    {
        if (items.Count == 0) return;
        var ids = items
            .Select(x => x.OperatorUserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0) return;

        var users = (await _userRepository.FindAsync(u => ids.Contains(u.Id))).ToList();
        var map = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Id))
            .GroupBy(u => u.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.OperatorUserId)) continue;
            if (map.TryGetValue(item.OperatorUserId.Trim(), out var name))
                item.OperatorUserName = name;
        }
    }

    private async Task EnrichWriteOffLedgerItemsAsync(
        IList<FinanceReceivableWriteOffLedgerItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0) return;

        var receiptIds = items
            .Select(x => x.FinanceReceiptId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var receipts = receiptIds.Count == 0
            ? new Dictionary<string, FinanceReceipt>(StringComparer.OrdinalIgnoreCase)
            : (await _receiptRepo.FindAsync(r => receiptIds.Contains(r.Id)))
                .Where(r => !string.IsNullOrWhiteSpace(r.Id))
                .GroupBy(r => r.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var itemIds = items
            .Select(x => x.FinanceReceiptItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var receiptItems = itemIds.Count == 0
            ? new Dictionary<string, FinanceReceiptItem>(StringComparer.OrdinalIgnoreCase)
            : (await _receiptItemRepo.FindAsync(i => itemIds.Contains(i.Id)))
                .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                .GroupBy(i => i.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var writeOffIds = items
            .Select(x => x.Id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var advanceLedgers = writeOffIds.Count == 0
            ? new Dictionary<string, FinanceCustomerAdvanceLedger>(StringComparer.OrdinalIgnoreCase)
            : (await _advanceLedgerRepo.FindAsync(l =>
                    l.FinanceReceivableWriteOffId != null && writeOffIds.Contains(l.FinanceReceivableWriteOffId)))
                .Where(l => !string.IsNullOrWhiteSpace(l.FinanceReceivableWriteOffId))
                .GroupBy(l => l.FinanceReceivableWriteOffId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var customerIds = items
            .Select(x => x.CustomerId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customers = customerIds.Count == 0
            ? new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase)
            : (await _customerRepo.FindAsync(c => customerIds.Contains(c.Id)))
                .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                .GroupBy(c => c.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var row in items)
        {
            if (string.IsNullOrWhiteSpace(row.FinanceReceiptCode)
                && !string.IsNullOrWhiteSpace(row.FinanceReceiptItemId)
                && receiptItems.TryGetValue(row.FinanceReceiptItemId.Trim(), out var receiptItem))
            {
                var receiptId = receiptItem.FinanceReceiptId?.Trim();
                if (receiptId != null)
                {
                    row.FinanceReceiptId ??= receiptId;
                    if (receipts.TryGetValue(receiptId, out var receipt))
                        row.FinanceReceiptCode = receipt.FinanceReceiptCode;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.FinanceReceiptItemId)
                && receiptItems.TryGetValue(row.FinanceReceiptItemId.Trim(), out var item))
            {
                row.Remark = item.Remark;
            }
            else if (advanceLedgers.TryGetValue(row.Id.Trim(), out var ledger))
            {
                row.Remark = ledger.Remark;
            }

            if (!string.IsNullOrWhiteSpace(row.CustomerId)
                && customers.TryGetValue(row.CustomerId.Trim(), out var cust)
                && !string.IsNullOrWhiteSpace(cust.EnglishOfficialName))
            {
                row.CustomerEnglishName = cust.EnglishOfficialName.Trim();
                var nameZh = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                if (!string.IsNullOrWhiteSpace(nameZh))
                    row.CustomerName = nameZh.Trim();
            }
        }

        await EnrichWriteOffLedgerOperatorNamesAsync(items);
    }

    private async Task EnrichWriteOffLedgerOperatorNamesAsync(IList<FinanceReceivableWriteOffLedgerItem> items)
    {
        if (items.Count == 0) return;
        var ids = items
            .Select(x => x.OperatorUserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0) return;

        var users = (await _userRepository.FindAsync(u => ids.Contains(u.Id))).ToList();
        var map = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Id))
            .GroupBy(u => u.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.OperatorUserId)) continue;
            if (map.TryGetValue(item.OperatorUserId.Trim(), out var name))
                item.OperatorUserName = name;
        }
    }

    private async Task EnrichReceivableWriteOffOperatorNamesAsync(IReadOnlyList<FinanceReceivableWriteOffDetailItem> items)
    {
        if (items.Count == 0) return;
        var ids = items
            .Select(x => x.OperatorUserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0) return;

        var users = (await _userRepository.FindAsync(u => ids.Contains(u.Id))).ToList();
        var map = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Id))
            .GroupBy(u => u.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.OperatorUserId)) continue;
            if (map.TryGetValue(item.OperatorUserId.Trim(), out var name))
                item.OperatorUserName = name;
        }
    }
}
