using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc cref="ISellOrderMainStatusSyncService" />
public sealed class SellOrderMainStatusSyncService : ISellOrderMainStatusSyncService
{
    private readonly IRepository<SellOrder> _soRepo;
    private readonly IRepository<SellOrderItem> _soItemRepo;
    private readonly IRepository<SellOrderItemExtend> _soItemExtendRepo;
    private readonly ILogger<SellOrderMainStatusSyncService> _logger;

    public SellOrderMainStatusSyncService(
        IRepository<SellOrder> soRepo,
        IRepository<SellOrderItem> soItemRepo,
        IRepository<SellOrderItemExtend> soItemExtendRepo,
        ILogger<SellOrderMainStatusSyncService> logger)
    {
        _soRepo = soRepo;
        _soItemRepo = soItemRepo;
        _soItemExtendRepo = soItemExtendRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TrySyncOrderMainStatusAsync(string sellOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellOrderId))
            return false;

        cancellationToken.ThrowIfCancellationRequested();
        var orderId = sellOrderId.Trim();

        var order = await _soRepo.GetByIdAsync(orderId);
        if (order == null)
            return false;

        var items = (await _soItemRepo.FindAsync(x => x.SellOrderId == orderId)).ToList();
        if (items.Count == 0)
            return false;

        var lineIds = items.Select(i => i.Id).ToList();
        var extendById = new Dictionary<string, SellOrderItemExtend>(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in await _soItemExtendRepo.FindAsync(e => lineIds.Contains(e.Id)))
            extendById[ext.Id] = ext;

        var before = order.Status;
        var target = SellOrderMainStatusCompute.ComputeAfterRefresh(order, items, extendById);
        if (target == before)
            return false;

        order.Status = target;
        order.ModifyTime = DateTime.UtcNow;
        await _soRepo.UpdateAsync(order);

        _logger.LogInformation(
            "销售订单主状态已同步: SellOrderId={SellOrderId} Code={Code} {Before} -> {After}",
            orderId,
            order.SellOrderCode,
            before,
            target);

        return true;
    }
}

/// <summary>销售订单主状态与明细扩展执行链对齐规则（与列表 orderStatus 同源）。</summary>
internal static class SellOrderMainStatusCompute
{
    /// <summary>sellorderitem.status：1=已取消</summary>
    private const short CancelledLine = 1;

    public static SellOrderMainStatus ComputeAfterRefresh(
        SellOrder order,
        IReadOnlyList<SellOrderItem> items,
        IReadOnlyDictionary<string, SellOrderItemExtend> extendByItemId)
    {
        if (order.Status is SellOrderMainStatus.Cancelled or SellOrderMainStatus.AuditFailed
            or SellOrderMainStatus.New or SellOrderMainStatus.PendingAudit)
            return order.Status;

        var hasActiveLine = items.Any(it => it.Status != CancelledLine);
        if (!hasActiveLine)
            return order.Status;

        var next = order.Status;

        if (next == SellOrderMainStatus.Approved && AnyActiveExecutionStarted(items, extendByItemId))
            next = SellOrderMainStatus.InProgress;

        if (next == SellOrderMainStatus.InProgress && AllActiveReceiptComplete(items, extendByItemId))
            next = SellOrderMainStatus.Completed;

        if (next == SellOrderMainStatus.Completed && !AllActiveReceiptComplete(items, extendByItemId))
            next = SellOrderMainStatus.InProgress;

        return next;
    }

    private static bool AnyActiveExecutionStarted(
        IReadOnlyList<SellOrderItem> items,
        IReadOnlyDictionary<string, SellOrderItemExtend> extendByItemId)
    {
        foreach (var it in items)
        {
            if (it.Status == CancelledLine)
                continue;
            if (!extendByItemId.TryGetValue(it.Id, out var e))
                continue;
            if (e.PurchaseProgressStatus > 0 || e.StockInProgressStatus > 0 || e.StockOutProgressStatus > 0
                || e.ReceiptProgressStatus > 0 || e.InvoiceProgressStatus > 0
                || e.QtyStockOutNotify > 0m)
                return true;
        }

        return false;
    }

    private static bool AllActiveReceiptComplete(
        IReadOnlyList<SellOrderItem> items,
        IReadOnlyDictionary<string, SellOrderItemExtend> extendByItemId)
    {
        foreach (var it in items)
        {
            if (it.Status == CancelledLine)
                continue;
            if (!extendByItemId.TryGetValue(it.Id, out var e) || e.ReceiptProgressStatus < 2)
                return false;
        }

        return true;
    }
}
