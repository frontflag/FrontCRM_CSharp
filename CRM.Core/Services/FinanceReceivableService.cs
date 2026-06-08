using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class FinanceReceivableService : IFinanceReceivableService
{
    private const short StockOutCompleted = 2;
    private const short StockOutFinished = 4;
    private const short ReceiptApproved = 2;
    private const short ReceiptReceived = 3;

    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly IRepository<FinanceReceivableWriteOff> _writeOffRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<FinanceReceipt> _receiptRepo;
    private readonly IRepository<FinanceReceiptItem> _receiptItemRepo;
    private readonly ISerialNumberService _serialNumberService;
    private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
    private readonly IFinanceReceivableListQuery _listQuery;
    private readonly IFinanceCustomerAdvanceService _advanceService;
    private readonly IUnitOfWork? _unitOfWork;

    public FinanceReceivableService(
        IRepository<FinanceReceivable> receivableRepo,
        IRepository<FinanceReceivableWriteOff> writeOffRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<FinanceReceipt> receiptRepo,
        IRepository<FinanceReceiptItem> receiptItemRepo,
        ISerialNumberService serialNumberService,
        ISellOrderItemExtendSyncService sellOrderItemExtendSync,
        IFinanceReceivableListQuery listQuery,
        IFinanceCustomerAdvanceService advanceService,
        IUnitOfWork? unitOfWork = null)
    {
        _receivableRepo = receivableRepo;
        _writeOffRepo = writeOffRepo;
        _stockOutRepo = stockOutRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _sellOrderRepo = sellOrderRepo;
        _receiptRepo = receiptRepo;
        _receiptItemRepo = receiptItemRepo;
        _serialNumberService = serialNumberService;
        _sellOrderItemExtendSync = sellOrderItemExtendSync;
        _listQuery = listQuery;
        _advanceService = advanceService;
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
        if (stockOut.StockOutType != StockOutTypeCode.Sales)
            return;
        if (stockOut.Status != StockOutCompleted && stockOut.Status != StockOutFinished)
            return;

        var existing = (await _receivableRepo.FindAsync(r =>
            r.StockOutId == stockOut.Id && !r.IsDeleted)).FirstOrDefault();
        if (existing != null)
            return;

        if (string.IsNullOrWhiteSpace(stockOut.SellOrderItemId))
            return;

        var soItem = await _sellOrderItemRepo.GetByIdAsync(stockOut.SellOrderItemId.Trim());
        if (soItem == null)
            return;

        var sellOrder = await _sellOrderRepo.GetByIdAsync(soItem.SellOrderId);
        if (sellOrder == null)
            return;

        var amount = ResolveReceivableAmount(stockOut, soItem);
        if (amount <= 0m)
            return;

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
            OutboundQty = stockOut.TotalQuantity,
            UnitPrice = soItem.Price,
            Currency = sellOrder.Currency,
            Amount = amount,
            VerifiedDone = 0m,
            VerifiedToBe = amount,
            VerificationStatus = FinanceVerificationStatusCode.Pending,
            StockOutDate = stockOut.StockOutDate,
            CreateTime = DateTime.UtcNow,
            CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
        };
        await _receivableRepo.AddAsync(receivable);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        await _sellOrderItemExtendSync.RecalculateAsync(soItem.Id);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task TrySoftDeleteForStockOutAsync(string stockOutId, string? actingUserId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockOutId))
            return;

        var receivable = (await _receivableRepo.FindAsync(r =>
            r.StockOutId == stockOutId.Trim() && !r.IsDeleted)).FirstOrDefault();
        if (receivable == null)
            return;

        AssertStockOutCanVoid(receivable);

        receivable.IsDeleted = true;
        receivable.ModifyTime = DateTime.UtcNow;
        receivable.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
        await _receivableRepo.UpdateAsync(receivable);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        await _sellOrderItemExtendSync.RecalculateAsync(receivable.SellOrderItemId);
        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();
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
    public Task<PagedResult<FinanceReceivable>> GetPagedAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default) =>
        _listQuery.GetPagedAsync(request, cancellationToken);

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

        var receiptItems = (await _receiptItemRepo.GetAllAsync()).ToList();
        var receiptIds = receiptItems.Select(i => i.FinanceReceiptId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var receipts = (await _receiptRepo.FindAsync(r => receiptIds.Contains(r.Id))).ToList();
        var receiptMap = receipts.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

        var candidates = new List<FinanceReceiptItemWriteOffCandidate>();
        foreach (var item in receiptItems)
        {
            if (!receiptMap.TryGetValue(item.FinanceReceiptId, out var receipt))
                continue;
            if (!string.Equals(receipt.CustomerId, cid, StringComparison.OrdinalIgnoreCase))
                continue;
            if (receipt.Status != ReceiptApproved && receipt.Status != ReceiptReceived)
                continue;

            var remaining = GetReceiptItemRemaining(item);
            if (remaining <= 0m)
                continue;

            candidates.Add(new FinanceReceiptItemWriteOffCandidate
            {
                Item = item,
                FinanceReceiptCode = receipt.FinanceReceiptCode,
                ReceiptStatus = receipt.Status,
                RemainingAmount = remaining,
                ReceiptPurpose = item.ReceiptPurpose,
                AdvanceSellOrderId = string.IsNullOrWhiteSpace(item.AdvanceSellOrderId)
                    ? item.SellOrderId
                    : item.AdvanceSellOrderId
            });
        }

        var advances = (await _advanceService.GetBalancesForCustomerAsync(cid))
            .Where(a => a.Balance > 0m)
            .ToList();

        return new FinanceReceivableWriteOffCandidates
        {
            ReceiptItems = candidates
                .OrderByDescending(c => c.Item.CreateTime)
                .ToList(),
            Receivables = receivables.Items.ToList(),
            AdvanceBalances = advances.Where(a => a.Balance > 0m).ToList()
        };
    }

    private static decimal GetReceiptItemRemaining(FinanceReceiptItem item) =>
        item.ReceiptConvertAmount - item.VerifiedAmount - item.AdvancePoolAmount;

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
                if (receipt.Status != ReceiptApproved && receipt.Status != ReceiptReceived)
                    throw new InvalidOperationException($"收款单 {receipt.FinanceReceiptCode} 未审核，不可核销");
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
        var touchedReceiptItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
            touchedReceiptItems.Add(receiptItem.Id);
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

        foreach (var itemId in touchedReceiptItems)
        {
            var item = receiptItems[itemId];
            var receipt = receipts[item.FinanceReceiptId];
            var excess = GetReceiptItemRemaining(item);
            if (excess > 0m)
                await _advanceService.CreditAutoInExcessAsync(receipt, item, excess, actingUserId);
        }

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

        foreach (var lineId in touchedLines)
            await _sellOrderItemExtendSync.RecalculateAsync(lineId);

        if (_unitOfWork != null)
            await _unitOfWork.SaveChangesAsync();

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
}
