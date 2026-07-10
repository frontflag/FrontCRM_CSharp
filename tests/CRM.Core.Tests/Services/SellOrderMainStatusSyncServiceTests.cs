using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRM.Core.Tests.Services;

public class SellOrderMainStatusSyncServiceTests
{
    [Fact]
    public async Task TrySync_ApprovedWithPurchaseProgress_PromotesToInProgress()
    {
        var orderRepo = new MemoryRepository<SellOrder>();
        var itemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();

        var order = new SellOrder
        {
            Id = "so-1",
            Status = SellOrderMainStatus.Approved,
            SellOrderCode = "SO-001"
        };
        await orderRepo.AddAsync(order);

        var line = new SellOrderItem
        {
            Id = "line-1",
            SellOrderId = "so-1",
            Qty = 10m,
            Price = 1m
        };
        await itemRepo.AddAsync(line);

        await extendRepo.AddAsync(new SellOrderItemExtend
        {
            Id = "line-1",
            PurchaseProgressStatus = 1,
            StockOutProgressStatus = 0
        });

        var svc = new SellOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<SellOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("so-1");

        Assert.True(changed);
        var updated = await orderRepo.GetByIdAsync("so-1");
        Assert.Equal(SellOrderMainStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async Task TrySync_ApprovedWithoutExecution_KeepsApproved()
    {
        var orderRepo = new MemoryRepository<SellOrder>();
        var itemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();

        var order = new SellOrder
        {
            Id = "so-2",
            Status = SellOrderMainStatus.Approved,
            SellOrderCode = "SO-002"
        };
        await orderRepo.AddAsync(order);

        var line = new SellOrderItem
        {
            Id = "line-2",
            SellOrderId = "so-2",
            Qty = 10m,
            Price = 1m
        };
        await itemRepo.AddAsync(line);

        await extendRepo.AddAsync(new SellOrderItemExtend
        {
            Id = "line-2",
            PurchaseProgressStatus = 0,
            StockOutProgressStatus = 0
        });

        var svc = new SellOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<SellOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("so-2");

        Assert.False(changed);
        var updated = await orderRepo.GetByIdAsync("so-2");
        Assert.Equal(SellOrderMainStatus.Approved, updated!.Status);
    }

    [Fact]
    public async Task TrySync_InProgressWithAllReceiptComplete_PromotesToCompleted()
    {
        var orderRepo = new MemoryRepository<SellOrder>();
        var itemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();

        var order = new SellOrder
        {
            Id = "so-3",
            Status = SellOrderMainStatus.InProgress,
            SellOrderCode = "SO-003"
        };
        await orderRepo.AddAsync(order);

        var line = new SellOrderItem
        {
            Id = "line-3",
            SellOrderId = "so-3",
            Qty = 10m,
            Price = 1m
        };
        await itemRepo.AddAsync(line);

        await extendRepo.AddAsync(new SellOrderItemExtend
        {
            Id = "line-3",
            ReceiptProgressStatus = 2,
            StockOutProgressStatus = 2
        });

        var svc = new SellOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<SellOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("so-3");

        Assert.True(changed);
        var updated = await orderRepo.GetByIdAsync("so-3");
        Assert.Equal(SellOrderMainStatus.Completed, updated!.Status);
    }

    [Fact]
    public async Task TrySync_InProgressWithStockOutOnlyComplete_KeepsInProgress()
    {
        var orderRepo = new MemoryRepository<SellOrder>();
        var itemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();

        var order = new SellOrder
        {
            Id = "so-4",
            Status = SellOrderMainStatus.InProgress,
            SellOrderCode = "SO-004"
        };
        await orderRepo.AddAsync(order);

        var line = new SellOrderItem
        {
            Id = "line-4",
            SellOrderId = "so-4",
            Qty = 10m,
            Price = 1m
        };
        await itemRepo.AddAsync(line);

        await extendRepo.AddAsync(new SellOrderItemExtend
        {
            Id = "line-4",
            ReceiptProgressStatus = 0,
            StockOutProgressStatus = 2
        });

        var svc = new SellOrderMainStatusSyncService(
            orderRepo, itemRepo, extendRepo, NullLogger<SellOrderMainStatusSyncService>.Instance);
        var changed = await svc.TrySyncOrderMainStatusAsync("so-4");

        Assert.False(changed);
        var updated = await orderRepo.GetByIdAsync("so-4");
        Assert.Equal(SellOrderMainStatus.InProgress, updated!.Status);
    }
}
