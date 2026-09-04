using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class SalesOrderCustomerDownstreamSyncService : ISalesOrderCustomerDownstreamSyncService
{
    private const short StockOutStatusCompleted = 2;
    private const short StockOutStatusCancelled = 3;
    private const short SellInvoiceStatusInvoiced = 100;

    private readonly IRepository<SellOrder> _soRepo;
    private readonly IRepository<SellOrderItem> _soItemRepo;
    private readonly IRepository<StockOutRequest> _notifyRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly IRepository<FinanceSellInvoice> _sellInvoiceRepo;
    private readonly IRepository<SellInvoiceItem> _sellInvoiceItemRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISalesParamsService _salesParams;
    private readonly ILogger<SalesOrderCustomerDownstreamSyncService> _logger;

    public SalesOrderCustomerDownstreamSyncService(
        IRepository<SellOrder> soRepo,
        IRepository<SellOrderItem> soItemRepo,
        IRepository<StockOutRequest> notifyRepo,
        IRepository<Packing> packingRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<FinanceReceivable> receivableRepo,
        IRepository<FinanceSellInvoice> sellInvoiceRepo,
        IRepository<SellInvoiceItem> sellInvoiceItemRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<CustomerInfo> customerRepo,
        IUnitOfWork unitOfWork,
        ISalesParamsService salesParams,
        ILogger<SalesOrderCustomerDownstreamSyncService> logger)
    {
        _soRepo = soRepo;
        _soItemRepo = soItemRepo;
        _notifyRepo = notifyRepo;
        _packingRepo = packingRepo;
        _packingItemRepo = packingItemRepo;
        _stockOutRepo = stockOutRepo;
        _receivableRepo = receivableRepo;
        _sellInvoiceRepo = sellInvoiceRepo;
        _sellInvoiceItemRepo = sellInvoiceItemRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _customerRepo = customerRepo;
        _unitOfWork = unitOfWork;
        _salesParams = salesParams;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SalesOrderCustomerDownstreamSyncPreviewResult> PreviewAsync(
        string salesOrderId,
        string? proposedCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        var bundle = await LoadBundleAsync(salesOrderId, proposedCustomerId, cancellationToken);
        var preview = await BuildPreviewAsync(bundle, cancellationToken);
        await EnrichPreviewItemsAsync(bundle, preview, cancellationToken);
        return preview;
    }

    /// <inheritdoc />
    public async Task<SalesOrderCustomerDownstreamSyncApplyResult> ApplyAsync(
        SellOrder order,
        string? actingUserId = null,
        string? proposedCustomerId = null,
        bool saveChanges = true,
        CancellationToken cancellationToken = default,
        bool confirmCompleted = false)
    {
        ArgumentNullException.ThrowIfNull(order);

        var bundle = await LoadBundleAsync(order.Id, proposedCustomerId, cancellationToken);
        bundle.Order = order;
        var preview = await BuildPreviewAsync(bundle, cancellationToken);
        await EnrichPreviewItemsAsync(bundle, preview, cancellationToken);

        if (preview.NoOp)
            return new SalesOrderCustomerDownstreamSyncApplyResult { Preview = preview, Applied = false };

        if (!preview.CanSync)
            throw new InvalidOperationException(preview.BlockReason ?? "存在已完结下游单据，无法同步客户信息");

        if (saveChanges && preview.HasCompleted && !confirmCompleted)
            throw new InvalidOperationException(
                "存在已完结下游，须确认后再刷新：" + string.Join("；", preview.CompletedDocuments));

        var targetCustomerId = bundle.TargetCustomerId!;
        var now = DateTime.UtcNow;
        var orderEntity = bundle.Order!;

        if (bundle.NeedRefreshSellOrderCustomerName || bundle.CustomerIdChanging)
        {
            orderEntity.CustomerId = targetCustomerId;
            orderEntity.CustomerName = bundle.TargetCustomerName;
            orderEntity.ModifyTime = now;
            await _soRepo.UpdateAsync(orderEntity);
        }

        foreach (var notify in bundle.SyncNotifies)
        {
            notify.CustomerId = targetCustomerId;
            notify.ModifyTime = now;
            await _notifyRepo.UpdateAsync(notify);
        }

        foreach (var packing in bundle.SyncPackings)
        {
            packing.CustomerId = targetCustomerId;
            packing.ModifyTime = now;
            await _packingRepo.UpdateAsync(packing);
        }

        foreach (var stockOut in bundle.SyncStockOuts)
        {
            stockOut.CustomerId = targetCustomerId;
            stockOut.ModifyTime = now;
            await _stockOutRepo.UpdateAsync(stockOut);
        }

        foreach (var receivable in bundle.SyncReceivables)
        {
            receivable.CustomerId = targetCustomerId;
            receivable.CustomerName = bundle.TargetCustomerName;
            await _receivableRepo.UpdateAsync(receivable);
        }

        if (bundle.PackingItemIdsForExtendSync.Count > 0)
        {
            foreach (var chunk in bundle.PackingItemIdsForExtendSync.Chunk(200))
            {
                await _unitOfWork.ExecuteAsync(
                    """
                    UPDATE packing_item_extend pie
                    SET customer_id = {0}
                    FROM packing_item pi
                    WHERE pie."PackingItemId" = pi."Id"
                      AND COALESCE(pie.is_deleted, false) = false
                      AND COALESCE(pi.is_deleted, false) = false
                      AND pi."Id" = ANY ({1})
                      AND TRIM(COALESCE(pie.customer_id, '')) IS DISTINCT FROM TRIM({0})
                    """,
                    targetCustomerId,
                    chunk.ToArray());
            }
        }

        if (saveChanges)
            await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "SO同步客户: SalesOrderId={SalesOrderId} Code={Code} CustomerId={CustomerId} HeaderName={HeaderName} Notifies={Notifies} Packings={Packings} Extends={Extends} StockOuts={StockOuts} Receivables={Receivables} Actor={Actor}",
            orderEntity.Id,
            orderEntity.SellOrderCode,
            targetCustomerId,
            bundle.NeedRefreshSellOrderCustomerName || bundle.CustomerIdChanging ? 1 : 0,
            bundle.SyncNotifies.Count,
            bundle.SyncPackings.Count,
            bundle.PackingItemIdsForExtendSync.Count,
            bundle.SyncStockOuts.Count,
            bundle.SyncReceivables.Count,
            actingUserId ?? "(null)");

        return new SalesOrderCustomerDownstreamSyncApplyResult { Preview = preview, Applied = true };
    }

    private async Task<SalesOrderCustomerDownstreamSyncPreviewResult> BuildPreviewAsync(
        CustomerSyncBundle bundle,
        CancellationToken cancellationToken)
    {
        var order = bundle.Order
            ?? throw new InvalidOperationException("销售订单不存在");

        var allowRefreshCompleted = await _salesParams.GetAllowRefreshCompletedBizNodesAsync(cancellationToken);
        Classify(bundle, allowRefreshCompleted);

        var preview = new SalesOrderCustomerDownstreamSyncPreviewResult
        {
            SalesOrderId = order.Id,
            SellOrderCode = order.SellOrderCode,
            CustomerId = bundle.TargetCustomerId,
            CustomerName = bundle.TargetCustomerName,
            OldCustomerId = order.CustomerId?.Trim(),
            OldCustomerName = order.CustomerName?.Trim(),
            SellOrderCustomerNameToSync = bundle.NeedRefreshSellOrderCustomerName || bundle.CustomerIdChanging ? 1 : 0,
            StockOutNotifiesToSync = bundle.SyncNotifies.Count,
            PackingsToSync = bundle.SyncPackings.Count,
            PackingItemExtendsToSync = bundle.PackingItemIdsForExtendSync.Count,
            StockOutsToSync = bundle.SyncStockOuts.Count,
            ReceivablesToSync = bundle.SyncReceivables.Count,
            CompletedDocuments = bundle.CompletedDocuments.ToList(),
            AllowCompletedParam = allowRefreshCompleted
        };

        if (!string.IsNullOrWhiteSpace(bundle.ProposedCustomerMissingReason))
        {
            preview.CanSync = false;
            preview.BlockReason = bundle.ProposedCustomerMissingReason;
            return preview;
        }

        if (string.IsNullOrWhiteSpace(bundle.TargetCustomerId))
        {
            preview.CanSync = false;
            preview.BlockReason = "销售订单未设置有效客户，请先在编辑页保存正确客户。";
            return preview;
        }

        if (bundle.BlockingDocuments.Count > 0)
        {
            preview.CanSync = false;
            preview.BlockReason = "存在已完结下游单据，无法同步客户信息：" + string.Join("；", bundle.BlockingDocuments);
            preview.BlockingDocuments = bundle.BlockingDocuments.ToList();
            return preview;
        }

        var hasWork = preview.SellOrderCustomerNameToSync > 0
            || preview.StockOutNotifiesToSync > 0
            || preview.PackingsToSync > 0
            || preview.PackingItemExtendsToSync > 0
            || preview.StockOutsToSync > 0
            || preview.ReceivablesToSync > 0;

        if (!hasWork)
        {
            preview.NoOp = true;
            preview.CanSync = true;
            return preview;
        }

        preview.CanSync = true;
        return preview;
    }

    private async Task EnrichPreviewItemsAsync(
        CustomerSyncBundle bundle,
        SalesOrderCustomerDownstreamSyncPreviewResult preview,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var targetId = bundle.TargetCustomerId?.Trim() ?? string.Empty;
        var items = new List<SalesOrderCustomerDownstreamSyncPreviewItem>();
        var customerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if ((bundle.NeedRefreshSellOrderCustomerName || bundle.CustomerIdChanging) && bundle.Order != null)
        {
            AddCustomerId(customerIds, bundle.Order.CustomerId);
            items.Add(new SalesOrderCustomerDownstreamSyncPreviewItem
            {
                Category = "sellOrder",
                DocumentCode = bundle.Order.SellOrderCode,
                CustomerId = bundle.Order.CustomerId?.Trim(),
                CustomerName = bundle.Order.CustomerName?.Trim(),
                IsMismatch = true
            });
        }

        foreach (var notify in bundle.SyncNotifies)
        {
            AddCustomerId(customerIds, notify.CustomerId);
            items.Add(new SalesOrderCustomerDownstreamSyncPreviewItem
            {
                Category = "stockOutNotify",
                DocumentCode = notify.RequestCode,
                CustomerId = notify.CustomerId?.Trim(),
                IsMismatch = !CustomerIdsMatch(targetId, notify.CustomerId)
            });
        }

        foreach (var packing in bundle.SyncPackings)
        {
            AddCustomerId(customerIds, packing.CustomerId);
            items.Add(new SalesOrderCustomerDownstreamSyncPreviewItem
            {
                Category = "packing",
                DocumentCode = packing.Code ?? packing.Id,
                CustomerId = packing.CustomerId?.Trim(),
                IsMismatch = !CustomerIdsMatch(targetId, packing.CustomerId)
            });
        }

        foreach (var stockOut in bundle.SyncStockOuts)
        {
            AddCustomerId(customerIds, stockOut.CustomerId);
            items.Add(new SalesOrderCustomerDownstreamSyncPreviewItem
            {
                Category = "stockOut",
                DocumentCode = stockOut.StockOutCode,
                CustomerId = stockOut.CustomerId?.Trim(),
                IsMismatch = !CustomerIdsMatch(targetId, stockOut.CustomerId)
            });
        }

        foreach (var receivable in bundle.SyncReceivables)
        {
            AddCustomerId(customerIds, receivable.CustomerId);
            items.Add(new SalesOrderCustomerDownstreamSyncPreviewItem
            {
                Category = "receivable",
                DocumentCode = receivable.ReceivableCode ?? receivable.Id,
                CustomerId = receivable.CustomerId?.Trim(),
                CustomerName = receivable.CustomerName?.Trim(),
                IsMismatch = !CustomerIdsMatch(targetId, receivable.CustomerId)
                    || !NamesMatch(receivable.CustomerName, bundle.TargetCustomerName)
            });
        }

        if (bundle.PackingItemIdsForExtendSync.Count > 0)
        {
            var extendIds = bundle.PackingItemIdsForExtendSync
                .Select(id => id.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var packingItemById = bundle.PackingItems
                .Where(pi => extendIds.Contains(pi.Id.Trim()))
                .ToDictionary(pi => pi.Id.Trim(), pi => pi, StringComparer.OrdinalIgnoreCase);
            var packingById = bundle.Packings
                .ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

            var extends = await LoadPackingItemExtendCustomersAsync(extendIds, cancellationToken);

            foreach (var packingItemId in bundle.PackingItemIdsForExtendSync)
            {
                var trimmedId = packingItemId.Trim();
                packingItemById.TryGetValue(trimmedId, out var packingItem);
                extends.TryGetValue(trimmedId, out var extendCustomerId);
                AddCustomerId(customerIds, extendCustomerId);

                string documentCode;
                if (packingItem != null
                    && !string.IsNullOrWhiteSpace(packingItem.PackingId)
                    && packingById.TryGetValue(packingItem.PackingId.Trim(), out var packing))
                {
                    var itemCode = packingItem.ItemCode?.Trim();
                    documentCode = string.IsNullOrWhiteSpace(itemCode)
                        ? $"{packing.Code ?? packing.Id} · 明细"
                        : $"{packing.Code ?? packing.Id} · {itemCode}";
                }
                else
                {
                    documentCode = packingItem?.ItemCode?.Trim() ?? trimmedId;
                }

                items.Add(new SalesOrderCustomerDownstreamSyncPreviewItem
                {
                    Category = "packingItemExtend",
                    DocumentCode = documentCode,
                    CustomerId = extendCustomerId,
                    IsMismatch = !CustomerIdsMatch(targetId, extendCustomerId)
                });
            }
        }

        var nameMap = await LoadCustomerNameMapAsync(customerIds, cancellationToken);
        foreach (var item in items)
        {
            // 销售订单行展示「当前快照名」；其余展示单据上的客户名
            if (string.Equals(item.Category, "sellOrder", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(item.CustomerName))
                continue;
            item.CustomerName = ResolveCustomerName(item.CustomerId, nameMap);
        }

        preview.SyncItems = items;
    }

    private async Task<Dictionary<string, string>> LoadPackingItemExtendCustomersAsync(
        HashSet<string> packingItemIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (packingItemIds.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chunk in packingItemIds.Chunk(200))
        {
            var idList = string.Join(
                ",",
                chunk.Select(id => $"'{id.Replace("'", "''", StringComparison.Ordinal)}'"));
            var sql = $"""
                SELECT pie."PackingItemId" AS PackingItemId, pie.customer_id AS CustomerId
                FROM packing_item_extend pie
                WHERE COALESCE(pie.is_deleted, false) = false
                  AND pie."PackingItemId" IN ({idList})
                """;
            var rows = await _unitOfWork.QueryAsync<PackingItemExtendCustomerRow>(sql);
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.PackingItemId))
                    continue;
                result[row.PackingItemId.Trim()] = row.CustomerId?.Trim() ?? string.Empty;
            }
        }

        return result;
    }

    private async Task<Dictionary<string, string>> LoadCustomerNameMapAsync(
        IEnumerable<string> customerIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ids = customerIds
            .Select(id => id.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ids.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var customers = await _customerRepo.FindAsync(c => !c.IsDeleted && ids.Contains(c.Id));
        return customers.ToDictionary(
            c => c.Id.Trim(),
            c => string.IsNullOrWhiteSpace(c.OfficialName) ? (c.CustomerName ?? c.Id) : c.OfficialName!,
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddCustomerId(ISet<string> customerIds, string? customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return;
        customerIds.Add(customerId.Trim());
    }

    private static string? ResolveCustomerName(string? customerId, IReadOnlyDictionary<string, string> nameMap)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return null;
        return nameMap.TryGetValue(customerId.Trim(), out var name) ? name : customerId.Trim();
    }

    private static void Classify(CustomerSyncBundle bundle, bool allowRefreshCompleted)
    {
        bundle.SyncNotifies.Clear();
        bundle.SyncPackings.Clear();
        bundle.SyncStockOuts.Clear();
        bundle.SyncReceivables.Clear();
        bundle.PackingItemIdsForExtendSync.Clear();
        bundle.BlockingDocuments.Clear();
        bundle.CompletedDocuments.Clear();

        var targetId = bundle.TargetCustomerId?.Trim() ?? string.Empty;

        foreach (var notify in bundle.Notifies)
        {
            if (CustomerIdsMatch(targetId, notify.CustomerId))
                continue;

            if (notify.Status >= StockOutRequestStatusCode.StockedOut)
            {
                if (allowRefreshCompleted)
                {
                    bundle.SyncNotifies.Add(notify);
                    bundle.CompletedDocuments.Add($"出库通知 {notify.RequestCode} 已出库");
                }
                else
                    bundle.BlockingDocuments.Add($"出库通知 {notify.RequestCode} 已出库");
                continue;
            }

            if (StockOutRequestStatusCode.IsCancelled(notify.Status))
                continue;

            bundle.SyncNotifies.Add(notify);
        }

        foreach (var packing in bundle.Packings)
        {
            if (CustomerIdsMatch(targetId, packing.CustomerId))
                continue;

            if (packing.Status >= PackingStatusCode.StockOutFinished)
            {
                if (allowRefreshCompleted)
                {
                    bundle.SyncPackings.Add(packing);
                    bundle.CompletedDocuments.Add($"装箱单 {packing.Code} 已出库完成");
                }
                else
                    bundle.BlockingDocuments.Add($"装箱单 {packing.Code} 已出库完成");
                continue;
            }

            bundle.SyncPackings.Add(packing);
        }

        var syncPackingIds = bundle.SyncPackings
            .Select(p => p.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in bundle.PackingItems)
        {
            if (string.IsNullOrWhiteSpace(item.PackingId))
                continue;
            if (!syncPackingIds.Contains(item.PackingId.Trim()))
                continue;
            bundle.PackingItemIdsForExtendSync.Add(item.Id.Trim());
        }

        foreach (var stockOut in bundle.StockOuts)
        {
            if (CustomerIdsMatch(targetId, stockOut.CustomerId))
                continue;

            if (stockOut.Status == StockOutStatusCompleted)
            {
                if (allowRefreshCompleted)
                {
                    bundle.SyncStockOuts.Add(stockOut);
                    bundle.CompletedDocuments.Add($"出库单 {stockOut.StockOutCode} 已出库");
                }
                else
                    bundle.BlockingDocuments.Add($"出库单 {stockOut.StockOutCode} 已出库");
                continue;
            }

            if (stockOut.Status == StockOutStatusCancelled)
                continue;

            bundle.SyncStockOuts.Add(stockOut);
        }

        foreach (var receivable in bundle.Receivables)
        {
            var idMismatch = !CustomerIdsMatch(targetId, receivable.CustomerId);
            var nameMismatch = !string.IsNullOrWhiteSpace(bundle.TargetCustomerName)
                && !NamesMatch(receivable.CustomerName, bundle.TargetCustomerName);

            if (!idMismatch && !nameMismatch)
                continue;

            var writtenOff = receivable.VerifiedDone > 0m
                || receivable.VerificationStatus > FinanceVerificationStatusCode.Pending;

            if (writtenOff && idMismatch)
            {
                if (!allowRefreshCompleted)
                {
                    bundle.BlockingDocuments.Add(
                        $"应收 {receivable.ReceivableCode ?? receivable.Id} 已有核销记录");
                }

                continue;
            }

            bundle.SyncReceivables.Add(receivable);
        }

        foreach (var invoice in bundle.SellInvoices)
        {
            if (invoice.InvoiceStatus >= SellInvoiceStatusInvoiced)
            {
                if (!allowRefreshCompleted)
                {
                    bundle.BlockingDocuments.Add(
                        $"销项发票 {invoice.InvoiceCode ?? invoice.InvoiceNo ?? invoice.Id} 已开票");
                }
            }
        }
    }

    private async Task<CustomerSyncBundle> LoadBundleAsync(
        string salesOrderId,
        string? proposedCustomerId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var orderId = salesOrderId.Trim();

        var order = await _soRepo.GetByIdAsync(orderId)
            ?? throw new InvalidOperationException($"销售订单 {orderId} 不存在");

        string? proposedCustomerMissingReason = null;
        var proposedTrimmed = string.IsNullOrWhiteSpace(proposedCustomerId) ? null : proposedCustomerId.Trim();
        var customerIdChanging = !string.IsNullOrWhiteSpace(proposedTrimmed)
            && !CustomerIdsMatch(order.CustomerId, proposedTrimmed);

        var targetCustomerId = !string.IsNullOrWhiteSpace(proposedTrimmed)
            ? proposedTrimmed
            : order.CustomerId?.Trim();

        string? targetCustomerName = null;
        if (!string.IsNullOrWhiteSpace(targetCustomerId))
        {
            var cust = await _customerRepo.GetByIdAsync(targetCustomerId);
            if (cust == null && !string.IsNullOrWhiteSpace(proposedTrimmed))
            {
                proposedCustomerMissingReason = $"客户 {targetCustomerId} 不存在";
            }
            else
            {
                targetCustomerName = ResolveMasterCustomerDisplayName(cust);
                if (string.IsNullOrWhiteSpace(targetCustomerName))
                    targetCustomerName = order.CustomerName?.Trim();
            }
        }
        else
        {
            targetCustomerName = order.CustomerName?.Trim();
        }

        var needRefreshHeaderName = customerIdChanging
            || (!string.IsNullOrWhiteSpace(targetCustomerId)
                && !string.IsNullOrWhiteSpace(targetCustomerName)
                && !NamesMatch(order.CustomerName, targetCustomerName));

        var lineIds = (await _soItemRepo.FindAsync(i => i.SellOrderId == orderId && !i.IsDeleted))
            .Select(i => i.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var notifies = (await _notifyRepo.FindAsync(n =>
                !n.IsDeleted && n.SalesOrderId == orderId))
            .ToList();

        var packingItems = (await _packingItemRepo.FindAsync(pi =>
                !pi.IsDeleted && pi.SellOrderId == orderId))
            .ToList();

        var packingIds = packingItems
            .Select(pi => pi.PackingId?.Trim())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        var packings = packingIds.Count == 0
            ? new List<Packing>()
            : (await _packingRepo.FindAsync(p => !p.IsDeleted && packingIds.Contains(p.Id))).ToList();

        var stockOuts = lineIds.Count == 0
            ? new List<StockOut>()
            : (await _stockOutRepo.FindAsync(s =>
                !s.IsDeleted
                && s.SellOrderItemId != null
                && lineIds.Contains(s.SellOrderItemId.Trim()))).ToList();

        var receivables = (await _receivableRepo.FindAsync(r =>
                !r.IsDeleted && r.SellOrderId == orderId))
            .ToList();

        var sellInvoices = await LoadMismatchSellInvoicesForOrderAsync(targetCustomerId, lineIds);

        return new CustomerSyncBundle
        {
            Order = order,
            TargetCustomerId = targetCustomerId,
            TargetCustomerName = targetCustomerName,
            CustomerIdChanging = customerIdChanging,
            NeedRefreshSellOrderCustomerName = needRefreshHeaderName,
            ProposedCustomerMissingReason = proposedCustomerMissingReason,
            Notifies = notifies,
            PackingItems = packingItems,
            Packings = packings,
            StockOuts = stockOuts,
            Receivables = receivables,
            SellInvoices = sellInvoices
        };
    }

    private async Task<List<FinanceSellInvoice>> LoadMismatchSellInvoicesForOrderAsync(
        string? targetCustomerId,
        HashSet<string> lineIds)
    {
        if (lineIds.Count == 0 || string.IsNullOrWhiteSpace(targetCustomerId))
            return new List<FinanceSellInvoice>();

        var stockOuts = (await _stockOutRepo.FindAsync(s =>
            !s.IsDeleted && s.SellOrderItemId != null && lineIds.Contains(s.SellOrderItemId.Trim()))).ToList();
        if (stockOuts.Count == 0)
            return new List<FinanceSellInvoice>();

        var stockOutIds = stockOuts.Select(s => s.Id.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stockOutItems = (await _stockOutItemRepo.FindAsync(i =>
            !i.IsDeleted && stockOutIds.Contains(i.StockOutId.Trim()))).ToList();
        if (stockOutItems.Count == 0)
            return new List<FinanceSellInvoice>();

        var stockOutItemIds = stockOutItems
            .Select(i => i.Id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invoiceItems = (await _sellInvoiceItemRepo.FindAsync(i =>
            !i.IsDeleted
            && i.StockOutItemId != null
            && stockOutItemIds.Contains(i.StockOutItemId.Trim()))).ToList();
        if (invoiceItems.Count == 0)
            return new List<FinanceSellInvoice>();

        var invoiceIds = invoiceItems
            .Select(i => i.FinanceSellInvoiceId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (await _sellInvoiceRepo.FindAsync(inv =>
                !inv.IsDeleted
                && invoiceIds.Contains(inv.Id)
                && inv.CustomerId != null
                && !CustomerIdsMatch(targetCustomerId, inv.CustomerId)))
            .ToList();
    }

    private static bool CustomerIdsMatch(string? expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(expected))
            return string.IsNullOrWhiteSpace(actual);
        return string.Equals(expected.Trim(), actual?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool NamesMatch(string? left, string? right)
    {
        var a = left?.Trim() ?? string.Empty;
        var b = right?.Trim() ?? string.Empty;
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    private static string? ResolveMasterCustomerDisplayName(CustomerInfo? cust)
    {
        if (cust == null)
            return null;
        var zh = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.NickName : cust.OfficialName;
        if (string.IsNullOrWhiteSpace(zh))
            zh = cust.CustomerName;
        return string.IsNullOrWhiteSpace(zh) ? null : zh.Trim();
    }

    private sealed class CustomerSyncBundle
    {
        public SellOrder? Order { get; set; }
        public string? TargetCustomerId { get; set; }
        public string? TargetCustomerName { get; set; }
        public bool CustomerIdChanging { get; set; }
        public bool NeedRefreshSellOrderCustomerName { get; set; }
        public string? ProposedCustomerMissingReason { get; set; }
        public List<StockOutRequest> Notifies { get; set; } = new();
        public List<PackingItem> PackingItems { get; set; } = new();
        public List<Packing> Packings { get; set; } = new();
        public List<StockOut> StockOuts { get; set; } = new();
        public List<FinanceReceivable> Receivables { get; set; } = new();
        public List<FinanceSellInvoice> SellInvoices { get; set; } = new();
        public List<StockOutRequest> SyncNotifies { get; } = new();
        public List<Packing> SyncPackings { get; } = new();
        public List<StockOut> SyncStockOuts { get; } = new();
        public List<FinanceReceivable> SyncReceivables { get; } = new();
        public List<string> PackingItemIdsForExtendSync { get; } = new();
        public List<string> BlockingDocuments { get; } = new();
        public List<string> CompletedDocuments { get; } = new();
    }

    private sealed class PackingItemExtendCustomerRow
    {
        public string PackingItemId { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
    }
}
