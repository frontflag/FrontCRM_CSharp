using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc cref="IPurchaseOrderMainStatusSyncService" />
public sealed class PurchaseOrderMainStatusSyncService : IPurchaseOrderMainStatusSyncService
{
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<PurchaseOrderItemExtend> _poItemExtendRepo;
    private readonly ILogger<PurchaseOrderMainStatusSyncService> _logger;

    public PurchaseOrderMainStatusSyncService(
        IRepository<PurchaseOrder> poRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<PurchaseOrderItemExtend> poItemExtendRepo,
        ILogger<PurchaseOrderMainStatusSyncService> logger)
    {
        _poRepo = poRepo;
        _poItemRepo = poItemRepo;
        _poItemExtendRepo = poItemExtendRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TrySyncOrderMainStatusAsync(string purchaseOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purchaseOrderId))
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        var orderId = purchaseOrderId.Trim();

        var order = await _poRepo.GetByIdAsync(orderId);
        if (order == null)
            return false;

        var items = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == orderId)).ToList();
        if (items.Count == 0)
            return false;

        var lineIds = items.Select(i => i.Id).ToList();
        var extendById = new Dictionary<string, PurchaseOrderItemExtend>(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in await _poItemExtendRepo.FindAsync(e => lineIds.Contains(e.Id)))
            extendById[ext.Id] = ext;

        var before = order.Status;
        var target = PurchaseOrderMainStatusCompute.ComputeAfterRefresh(order, items, extendById);
        if (target == before)
            return false;

        order.Status = target;
        order.ModifyTime = DateTime.UtcNow;
        await _poRepo.UpdateAsync(order);

        _logger.LogInformation(
            "采购订单主状态已同步: PurchaseOrderId={PurchaseOrderId} Code={Code} {Before} -> {After}",
            orderId,
            order.PurchaseOrderCode,
            before,
            target);

        return true;
    }
}

/// <summary>采购订单主状态与明细扩展付款/入库/采购进度对齐规则。</summary>
internal static class PurchaseOrderMainStatusCompute
{
    private const short StatusCancelled = -2;
    private const short StatusAuditFailed = -1;
    private const short StatusConfirmed = 30;
    private const short StatusInProgress = 50;
    private const short StatusCompleted = 100;
    private const short ItemCancelled = -2;

    public static short ComputeAfterRefresh(
        PurchaseOrder order,
        IReadOnlyList<PurchaseOrderItem> items,
        IReadOnlyDictionary<string, PurchaseOrderItemExtend> extendByItemId)
    {
        if (order.Status is StatusCancelled or StatusAuditFailed)
            return order.Status;

        if (order.Status < StatusConfirmed)
            return order.Status;

        var activeItems = items.Where(i => i.Status != ItemCancelled).ToList();
        if (activeItems.Count == 0)
            return order.Status;

        var allPurchaseComplete = activeItems.All(it =>
            extendByItemId.TryGetValue(it.Id, out var e) && e.PurchaseProgressStatus >= 2);

        var anyPartialPayOrStockIn = activeItems.Any(it =>
        {
            if (!extendByItemId.TryGetValue(it.Id, out var e))
                return false;
            return IsPartial(e.PaymentProgressStatus) || IsPartial(e.StockInProgressStatus);
        });

        if (allPurchaseComplete)
            return StatusCompleted;

        if (anyPartialPayOrStockIn)
            return StatusInProgress;

        if (order.Status is StatusCompleted or StatusInProgress)
            return StatusConfirmed;

        return order.Status;
    }

    private static bool IsPartial(short status) => status > 0 && status < 2;
}
