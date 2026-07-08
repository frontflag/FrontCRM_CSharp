using CRM.Core.Constants;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRM.Core.Tests.Services;

public class SellOrderItemExtendSyncServiceTests
{
    [Fact]
    public async Task RecalculateAsync_UpdatesAmountFields_WhenLineQtyChanges()
    {
        var lineId = Guid.NewGuid().ToString();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var soItem = new SellOrderItem
        {
            Id = lineId,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 80m,
            Price = 10m,
            ConvertPrice = 1m
        };
        var extend = new SellOrderItemExtend
        {
            Id = lineId,
            ReceiptAmount = 1000m,
            ReceiptAmountNot = 1000m,
            InvoiceAmount = 1000m,
            InvoiceAmountNot = 1000m,
            PaymentAmountToBe = 1000m
        };
        await soItemRepo.AddAsync(soItem);
        await extendRepo.AddAsync(extend);

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockOutRequest>(),
            new MemoryRepository<StockOut>(),
            new MemoryRepository<FinanceReceivable>(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var updated = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(updated);
        Assert.Equal(800m, updated!.ReceiptAmount);
        Assert.Equal(800m, updated.InvoiceAmount);
        Assert.Equal(800m, updated.PaymentAmountToBe);
        Assert.Equal(800m, updated.ReceiptAmountNot);
        Assert.Equal(800m, updated.InvoiceAmountNot);
    }

    [Fact]
    public async Task RecalculateAsync_DoesNotExpandPartialSingleStockOutRequest_OnRefresh()
    {
        var lineId = Guid.NewGuid().ToString();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var requestRepo = new MemoryRepository<StockOutRequest>();
        var soItem = new SellOrderItem
        {
            Id = lineId,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 1000m,
            Price = 10m,
            ConvertPrice = 1m
        };
        var extend = new SellOrderItemExtend { Id = lineId };
        var request = new StockOutRequest
        {
            Id = Guid.NewGuid().ToString(),
            SalesOrderItemId = lineId,
            Quantity = 300,
            Status = StockOutRequestStatusCode.PendingPacking
        };
        await soItemRepo.AddAsync(soItem);
        await extendRepo.AddAsync(extend);
        await requestRepo.AddAsync(request);

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            requestRepo,
            new MemoryRepository<StockOut>(),
            new MemoryRepository<FinanceReceivable>(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var updatedRequest = await requestRepo.GetByIdAsync(request.Id);
        var updated = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(updatedRequest);
        Assert.NotNull(updated);
        Assert.Equal(300, updatedRequest!.Quantity);
        Assert.Equal(300m, updated!.QtyStockOutNotify);
        Assert.Equal(700m, updated.QtyStockOutNotifyNot);
    }

    [Fact]
    public async Task RecalculateAsync_ShrinksSingleActiveStockOutRequest_WhenQtyDecreasesBelowNotify()
    {
        var lineId = Guid.NewGuid().ToString();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var requestRepo = new MemoryRepository<StockOutRequest>();
        var soItem = new SellOrderItem
        {
            Id = lineId,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 50m,
            Price = 10m,
            ConvertPrice = 1m
        };
        var extend = new SellOrderItemExtend { Id = lineId };
        var request = new StockOutRequest
        {
            Id = Guid.NewGuid().ToString(),
            SalesOrderItemId = lineId,
            Quantity = 80,
            Status = StockOutRequestStatusCode.PendingPacking
        };
        await soItemRepo.AddAsync(soItem);
        await extendRepo.AddAsync(extend);
        await requestRepo.AddAsync(request);

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            requestRepo,
            new MemoryRepository<StockOut>(),
            new MemoryRepository<FinanceReceivable>(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var updatedRequest = await requestRepo.GetByIdAsync(request.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(50, updatedRequest!.Quantity);
    }

    [Fact]
    public async Task RecalculateAsync_Throws_WhenQtyLessThanActiveNotifySum()
    {
        var lineId = Guid.NewGuid().ToString();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var requestRepo = new MemoryRepository<StockOutRequest>();
        var soItem = new SellOrderItem
        {
            Id = lineId,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 50m,
            Price = 10m,
            ConvertPrice = 1m
        };
        var extend = new SellOrderItemExtend { Id = lineId };
        await soItemRepo.AddAsync(soItem);
        await extendRepo.AddAsync(extend);
        await requestRepo.AddAsync(new StockOutRequest
        {
            Id = Guid.NewGuid().ToString(),
            SalesOrderItemId = lineId,
            Quantity = 30,
            Status = StockOutRequestStatusCode.PendingPacking
        });
        await requestRepo.AddAsync(new StockOutRequest
        {
            Id = Guid.NewGuid().ToString(),
            SalesOrderItemId = lineId,
            Quantity = 30,
            Status = StockOutRequestStatusCode.PendingPacking
        });

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            requestRepo,
            new MemoryRepository<StockOut>(),
            new MemoryRepository<FinanceReceivable>(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecalculateAsync(lineId));
        Assert.Contains("出库通知", ex.Message);
    }

    [Fact]
    public async Task RecalculateAsync_IgnoresCustomsStockOutNotify_WhenSummingSalesLineNotifyQty()
    {
        var lineId = Guid.NewGuid().ToString();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var requestRepo = new MemoryRepository<StockOutRequest>();
        var soItem = new SellOrderItem
        {
            Id = lineId,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 66m,
            Price = 10m,
            ConvertPrice = 1m
        };
        var extend = new SellOrderItemExtend { Id = lineId };
        await soItemRepo.AddAsync(soItem);
        await extendRepo.AddAsync(extend);
        await requestRepo.AddAsync(new StockOutRequest
        {
            Id = Guid.NewGuid().ToString(),
            SalesOrderItemId = lineId,
            Quantity = 66,
            Status = StockOutRequestStatusCode.StockedOut,
            StockOutType = StockOutTypeCode.Sales
        });
        await requestRepo.AddAsync(new StockOutRequest
        {
            Id = Guid.NewGuid().ToString(),
            SalesOrderItemId = lineId,
            Quantity = 66,
            Status = StockOutRequestStatusCode.PendingCustoms,
            StockOutType = StockOutTypeCode.Customs
        });

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            requestRepo,
            new MemoryRepository<StockOut>(),
            new MemoryRepository<FinanceReceivable>(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var updated = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(updated);
        Assert.Equal(66m, updated!.QtyStockOutNotify);
        Assert.Equal(0m, updated.QtyStockOutNotifyNot);
    }
}
