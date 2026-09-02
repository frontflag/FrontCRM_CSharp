using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class SellOrderItemExtendSyncServiceTests
{
    private static ISellOrderMainStatusSyncService NoOpMainStatusSync()
        => Substitute.For<ISellOrderMainStatusSyncService>();

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
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
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
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
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
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var updatedRequest = await requestRepo.GetByIdAsync(request.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(50, updatedRequest!.Quantity);
    }

    [Fact]
    public async Task RecalculateAsync_StatusOnly_DoesNotShrinkSingleActiveStockOutRequest()
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
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId, SellOrderItemRecalculateOptions.StatusOnly);

        var updatedRequest = await requestRepo.GetByIdAsync(request.Id);
        Assert.NotNull(updatedRequest);
        Assert.Equal(80, updatedRequest!.Quantity);
    }

    [Fact]
    public async Task SyncStockOutNotifyPlanQtyAsync_ShrinksSinglePendingOverQty()
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
        var request = new StockOutRequest
        {
            Id = Guid.NewGuid().ToString(),
            SalesOrderItemId = lineId,
            Quantity = 80,
            Status = StockOutRequestStatusCode.PendingPacking
        };
        await soItemRepo.AddAsync(soItem);
        await extendRepo.AddAsync(new SellOrderItemExtend { Id = lineId });
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
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        var updated = await service.SyncStockOutNotifyPlanQtyAsync(lineId);
        var updatedRequest = await requestRepo.GetByIdAsync(request.Id);
        Assert.Equal(1, updated);
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
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecalculateAsync(lineId));
        Assert.Contains("出库通知", ex.Message);
    }

    [Fact]
    public async Task RecalculateAsync_SkipsOutboundGuards_WhenEnforceFalse()
    {
        var lineId = Guid.NewGuid().ToString();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var stockOutRepo = new MemoryRepository<StockOut>();
        var stockOutItemRepo = new MemoryRepository<StockOutItem>();
        var stockOutItemExtendRepo = new MemoryRepository<StockOutItemExtend>();
        var soItem = new SellOrderItem
        {
            Id = lineId,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 100m,
            Price = 10m,
            ConvertPrice = 1m
        };
        await soItemRepo.AddAsync(soItem);
        await extendRepo.AddAsync(new SellOrderItemExtend { Id = lineId });

        var stockOutId = Guid.NewGuid().ToString();
        var itemId = Guid.NewGuid().ToString();
        await stockOutRepo.AddAsync(new StockOut
        {
            Id = stockOutId,
            StockOutCode = "STO-TEST",
            SellOrderItemId = lineId,
            Status = 2,
            StockOutType = 1,
            TotalQuantity = 4773
        });
        await stockOutItemRepo.AddAsync(new StockOutItem
        {
            Id = itemId,
            StockOutId = stockOutId,
            Quantity = 4773,
            ActualQty = 4773
        });
        await stockOutItemExtendRepo.AddAsync(new StockOutItemExtend
        {
            Id = itemId,
            SellOrderItemId = lineId
        });

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockOutRequest>(),
            stockOutRepo,
            stockOutItemRepo,
            stockOutItemExtendRepo,
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId, enforceLineQtyOutboundGuards: false);

        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(ext);
        Assert.Equal(4773m, ext!.QtyStockOutActual);
    }

    [Fact]
    public async Task RecalculateAsync_UsesItemLevelQty_ForMultiLineSalesStockOutHeader()
    {
        var lineA = Guid.NewGuid().ToString();
        var lineB = Guid.NewGuid().ToString();
        var stockOutId = Guid.NewGuid().ToString();
        var itemA = Guid.NewGuid().ToString();
        var itemB = Guid.NewGuid().ToString();

        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var stockOutRepo = new MemoryRepository<StockOut>();
        var stockOutItemRepo = new MemoryRepository<StockOutItem>();
        var stockOutItemExtendRepo = new MemoryRepository<StockOutItemExtend>();

        await soItemRepo.AddAsync(new SellOrderItem
        {
            Id = lineA,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 800m,
            Price = 1m,
            ConvertPrice = 1m
        });
        await extendRepo.AddAsync(new SellOrderItemExtend { Id = lineA });

        // 头表 SellOrderItemId 误挂 lineA，TotalQuantity 为多行合计 3450
        await stockOutRepo.AddAsync(new StockOut
        {
            Id = stockOutId,
            StockOutCode = "STO-MULTI",
            StockOutType = StockOutTypeCode.Sales,
            Status = 4,
            SellOrderItemId = lineA,
            TotalQuantity = 3450,
            WarehouseId = Guid.NewGuid().ToString()
        });
        await stockOutItemRepo.AddAsync(new StockOutItem
        {
            Id = itemA,
            StockOutId = stockOutId,
            MaterialId = Guid.NewGuid().ToString(),
            Quantity = 800,
            ActualQty = 800
        });
        await stockOutItemRepo.AddAsync(new StockOutItem
        {
            Id = itemB,
            StockOutId = stockOutId,
            MaterialId = Guid.NewGuid().ToString(),
            Quantity = 2650,
            ActualQty = 2650
        });
        await stockOutItemExtendRepo.AddAsync(new StockOutItemExtend
        {
            Id = itemA,
            SellOrderItemId = lineA,
            QtyStockOut = 800
        });
        await stockOutItemExtendRepo.AddAsync(new StockOutItemExtend
        {
            Id = itemB,
            SellOrderItemId = lineB,
            QtyStockOut = 2650
        });

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockOutRequest>(),
            stockOutRepo,
            stockOutItemRepo,
            stockOutItemExtendRepo,
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineA);

        var updated = await extendRepo.GetByIdAsync(lineA);
        Assert.NotNull(updated);
        Assert.Equal(800m, updated!.QtyStockOutActual);
        Assert.Equal(2, updated.StockOutProgressStatus);
    }

    /// <summary>
    /// 备货层出库时扩展行曾误写库存原绑定销售行；头表已挂客单行时，刷新应把实出与出库状态归到客单行。
    /// </summary>
    [Fact]
    public async Task RecalculateAsync_AttributesStockingOutbound_WhenExtendSellLinePointsToStockingLine()
    {
        var customerLine = Guid.NewGuid().ToString();
        var stockingLine = Guid.NewGuid().ToString();
        var stockOutId = Guid.NewGuid().ToString();
        var itemId = Guid.NewGuid().ToString();

        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var stockOutRepo = new MemoryRepository<StockOut>();
        var stockOutItemRepo = new MemoryRepository<StockOutItem>();
        var stockOutItemExtendRepo = new MemoryRepository<StockOutItemExtend>();

        await soItemRepo.AddAsync(new SellOrderItem
        {
            Id = customerLine,
            SellOrderId = Guid.NewGuid().ToString(),
            Qty = 17500m,
            Price = 1m,
            ConvertPrice = 1m
        });
        await extendRepo.AddAsync(new SellOrderItemExtend
        {
            Id = customerLine,
            QtyStockOutActual = 0m,
            StockOutProgressStatus = 0
        });

        await stockOutRepo.AddAsync(new StockOut
        {
            Id = stockOutId,
            StockOutCode = "STO-STOCKING",
            StockOutType = StockOutTypeCode.Sales,
            Status = 4,
            SellOrderItemId = customerLine,
            TotalQuantity = 17500,
            WarehouseId = Guid.NewGuid().ToString()
        });
        await stockOutItemRepo.AddAsync(new StockOutItem
        {
            Id = itemId,
            StockOutId = stockOutId,
            MaterialId = Guid.NewGuid().ToString(),
            Quantity = 17500,
            ActualQty = 17500
        });
        await stockOutItemExtendRepo.AddAsync(new StockOutItemExtend
        {
            Id = itemId,
            SellOrderItemId = stockingLine,
            StockType = 2,
            QtyStockOut = 17500,
            PurchasePriceUsd = 0.9m
        });

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockOutRequest>(),
            stockOutRepo,
            stockOutItemRepo,
            stockOutItemExtendRepo,
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(customerLine);

        var updated = await extendRepo.GetByIdAsync(customerLine);
        Assert.NotNull(updated);
        Assert.Equal(17500m, updated!.QtyStockOutActual);
        Assert.Equal(2, updated.StockOutProgressStatus);
        // 无本行 PO：备货出库批次覆盖整行 → 预计销售利润 = 收入 − 备货采购成本
        // 17500×1 − 17500×0.9 = 1750（旧逻辑仅按扩展 SellOrderItemId 匹配时成本=0，会误写成整行收入）
        Assert.Equal(1750m, updated.SalesProfitExpected);
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
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NoOpMainStatusSync(),
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var updated = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(updated);
        Assert.Equal(66m, updated!.QtyStockOutNotify);
        Assert.Equal(0m, updated.QtyStockOutNotifyNot);
    }

    [Fact]
    public async Task RecalculateAsync_WithPurchaseQty_SyncsOrderMainStatusToInProgress()
    {
        var orderId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var orderRepo = new MemoryRepository<SellOrder>();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();

        await orderRepo.AddAsync(new SellOrder
        {
            Id = orderId,
            Status = SellOrderMainStatus.Approved,
            SellOrderCode = "SO-SYNC"
        });
        await soItemRepo.AddAsync(new SellOrderItem
        {
            Id = lineId,
            SellOrderId = orderId,
            Qty = 10m,
            Price = 1m
        });
        await extendRepo.AddAsync(new SellOrderItemExtend { Id = lineId });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = Guid.NewGuid().ToString(),
            SellOrderItemId = lineId,
            Qty = 5m
        });

        var mainStatusSync = new SellOrderMainStatusSyncService(
            orderRepo, soItemRepo, extendRepo, NullLogger<SellOrderMainStatusSyncService>.Instance);

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            poItemRepo,
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockOutRequest>(),
            new MemoryRepository<StockOut>(),
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            mainStatusSync,
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var order = await orderRepo.GetByIdAsync(orderId);
        Assert.Equal(SellOrderMainStatus.InProgress, order!.Status);
    }

    [Fact]
    public async Task RecalculateAsync_WithFullReceipt_SyncsOrderMainStatusToCompleted()
    {
        var orderId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var orderRepo = new MemoryRepository<SellOrder>();
        var soItemRepo = new MemoryRepository<SellOrderItem>();
        var extendRepo = new MemoryRepository<SellOrderItemExtend>();
        var receivableRepo = new MemoryRepository<FinanceReceivable>();

        await orderRepo.AddAsync(new SellOrder
        {
            Id = orderId,
            Status = SellOrderMainStatus.InProgress,
            SellOrderCode = "SO-RECEIPT"
        });
        await soItemRepo.AddAsync(new SellOrderItem
        {
            Id = lineId,
            SellOrderId = orderId,
            Qty = 10m,
            Price = 1m
        });
        await extendRepo.AddAsync(new SellOrderItemExtend { Id = lineId });
        await receivableRepo.AddAsync(new FinanceReceivable
        {
            Id = Guid.NewGuid().ToString(),
            SellOrderItemId = lineId,
            SellOrderId = orderId,
            StockOutId = Guid.NewGuid().ToString(),
            StockOutCode = "OUT-001",
            VerifiedDone = 10m,
            IsDeleted = false
        });

        var mainStatusSync = new SellOrderMainStatusSyncService(
            orderRepo, soItemRepo, extendRepo, NullLogger<SellOrderMainStatusSyncService>.Instance);

        var service = new SellOrderItemExtendSyncService(
            soItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockOutRequest>(),
            new MemoryRepository<StockOut>(),
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<StockOutItemExtend>(),
            receivableRepo,
            mainStatusSync,
            NullLogger<SellOrderItemExtendSyncService>.Instance);

        await service.RecalculateAsync(lineId);

        var order = await orderRepo.GetByIdAsync(orderId);
        Assert.Equal(SellOrderMainStatus.Completed, order!.Status);
        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.Equal(2, ext!.ReceiptProgressStatus);
    }
}
