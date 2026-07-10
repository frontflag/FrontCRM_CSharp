using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRM.Core.Tests.Services;

public class PurchaseOrderMainStatusSyncServiceTests
{
    [Fact]
    public async Task TrySync_ConfirmedWithPartialPayment_PromotesToInProgress()
    {
        var orderRepo = new MemoryRepository<PurchaseOrder>();
        var itemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await orderRepo.AddAsync(new PurchaseOrder { Id = "po-1", Status = 30, PurchaseOrderCode = "PO-001" });
        await itemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = "line-1",
            PurchaseOrderId = "po-1",
            Qty = 100m,
            Cost = 10m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = "line-1",
            PaymentProgressStatus = 1,
            StockInProgressStatus = 0,
            PurchaseProgressStatus = 1
        });

        var svc = new PurchaseOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<PurchaseOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("po-1");

        Assert.True(changed);
        Assert.Equal(50, (await orderRepo.GetByIdAsync("po-1"))!.Status);
    }

    [Fact]
    public async Task TrySync_ConfirmedWithPartialStockIn_PromotesToInProgress()
    {
        var orderRepo = new MemoryRepository<PurchaseOrder>();
        var itemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await orderRepo.AddAsync(new PurchaseOrder { Id = "po-2", Status = 30, PurchaseOrderCode = "PO-002" });
        await itemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = "line-2",
            PurchaseOrderId = "po-2",
            Qty = 100m,
            Cost = 10m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = "line-2",
            PaymentProgressStatus = 0,
            StockInProgressStatus = 1,
            PurchaseProgressStatus = 1
        });

        var svc = new PurchaseOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<PurchaseOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("po-2");

        Assert.True(changed);
        Assert.Equal(50, (await orderRepo.GetByIdAsync("po-2"))!.Status);
    }

    [Fact]
    public async Task TrySync_AllLinesPurchaseComplete_PromotesToCompleted()
    {
        var orderRepo = new MemoryRepository<PurchaseOrder>();
        var itemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await orderRepo.AddAsync(new PurchaseOrder { Id = "po-3", Status = 50, PurchaseOrderCode = "PO-003" });
        await itemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = "line-3a",
            PurchaseOrderId = "po-3",
            Qty = 100m,
            Cost = 10m,
            Status = 60
        });
        await itemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = "line-3b",
            PurchaseOrderId = "po-3",
            Qty = 50m,
            Cost = 10m,
            Status = 60
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = "line-3a",
            PaymentProgressStatus = 2,
            StockInProgressStatus = 2,
            PurchaseProgressStatus = 2
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = "line-3b",
            PaymentProgressStatus = 2,
            StockInProgressStatus = 2,
            PurchaseProgressStatus = 2
        });

        var svc = new PurchaseOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<PurchaseOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("po-3");

        Assert.True(changed);
        Assert.Equal(100, (await orderRepo.GetByIdAsync("po-3"))!.Status);
    }

    [Fact]
    public async Task TrySync_CompletedWithOneLineIncomplete_RevertsToInProgress()
    {
        var orderRepo = new MemoryRepository<PurchaseOrder>();
        var itemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await orderRepo.AddAsync(new PurchaseOrder { Id = "po-4", Status = 100, PurchaseOrderCode = "PO-004" });
        await itemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = "line-4a",
            PurchaseOrderId = "po-4",
            Qty = 100m,
            Cost = 10m,
            Status = 60
        });
        await itemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = "line-4b",
            PurchaseOrderId = "po-4",
            Qty = 50m,
            Cost = 10m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = "line-4a",
            PaymentProgressStatus = 2,
            StockInProgressStatus = 2,
            PurchaseProgressStatus = 2
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = "line-4b",
            PaymentProgressStatus = 1,
            StockInProgressStatus = 0,
            PurchaseProgressStatus = 1
        });

        var svc = new PurchaseOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<PurchaseOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("po-4");

        Assert.True(changed);
        Assert.Equal(50, (await orderRepo.GetByIdAsync("po-4"))!.Status);
    }

    [Fact]
    public async Task TrySync_InProgressWithoutPartial_RevertsToConfirmed()
    {
        var orderRepo = new MemoryRepository<PurchaseOrder>();
        var itemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await orderRepo.AddAsync(new PurchaseOrder { Id = "po-5", Status = 50, PurchaseOrderCode = "PO-005" });
        await itemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = "line-5",
            PurchaseOrderId = "po-5",
            Qty = 100m,
            Cost = 10m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = "line-5",
            PaymentProgressStatus = 0,
            StockInProgressStatus = 0,
            PurchaseProgressStatus = 0
        });

        var svc = new PurchaseOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<PurchaseOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("po-5");

        Assert.True(changed);
        Assert.Equal(30, (await orderRepo.GetByIdAsync("po-5"))!.Status);
    }
}
