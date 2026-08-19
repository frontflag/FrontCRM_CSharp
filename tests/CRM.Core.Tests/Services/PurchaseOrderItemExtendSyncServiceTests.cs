using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class PurchaseOrderItemExtendSyncServiceTests
{
    private static PurchaseOrderItemExtendSyncService CreateService(
        MemoryRepository<PurchaseOrderItem> poItemRepo,
        MemoryRepository<PurchaseOrder> poRepo,
        MemoryRepository<PurchaseOrderItemExtend> extendRepo,
        MemoryRepository<StockInNotify> notifyRepo)
    {
        return new PurchaseOrderItemExtendSyncService(
            poItemRepo,
            poRepo,
            extendRepo,
            notifyRepo,
            new MemoryRepository<FinancePaymentItem>(),
            new MemoryRepository<FinancePayment>(),
            new MemoryRepository<FinancePurchaseInvoiceItem>(),
            new MemoryRepository<FinancePurchaseInvoice>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<QCInfo>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderMainStatusSyncService>(),
            Substitute.For<IFinancePurchaseInvoicePaymentSyncService>());
    }

    [Fact]
    public async Task RecalculateAsync_DoesNotExpandPartialSingleArrivalNotice_OnRefresh()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var notifyRepo = new MemoryRepository<StockInNotify>();

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, Status = 30 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            PurchaseOrderItemCode = "PO0023N-1",
            Qty = 1000m,
            Cost = 0.546m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = lineId,
            QtyStockInNotifyNot = 1000m
        });
        var noticeId = Guid.NewGuid().ToString();
        await notifyRepo.AddAsync(new StockInNotify
        {
            Id = noticeId,
            PurchaseOrderItemId = lineId,
            ExpectQty = 300,
            ReceiveQty = 0,
            Cost = 0.546m,
            ExpectTotal = 163.8m
        });

        var service = CreateService(poItemRepo, poRepo, extendRepo, notifyRepo);
        await service.RecalculateAsync(lineId);

        var notice = await notifyRepo.GetByIdAsync(noticeId);
        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(notice);
        Assert.NotNull(ext);
        Assert.Equal(300, notice!.ExpectQty);
        Assert.Equal(300m, ext!.QtyStockInNotifyExpectSum);
        Assert.Equal(700m, ext.QtyStockInNotifyNot);
    }

    [Fact]
    public async Task RecalculateAsync_ShrinksSingleArrivalNotice_WhenPoQtyDecreasesBelowExpect()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var notifyRepo = new MemoryRepository<StockInNotify>();

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, Status = 30 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            Qty = 800m,
            Cost = 1m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend { Id = lineId });
        var noticeId = Guid.NewGuid().ToString();
        await notifyRepo.AddAsync(new StockInNotify
        {
            Id = noticeId,
            PurchaseOrderItemId = lineId,
            ExpectQty = 1000,
            ReceiveQty = 0,
            Cost = 1m,
            ExpectTotal = 1000m
        });

        var service = CreateService(poItemRepo, poRepo, extendRepo, notifyRepo);
        await service.RecalculateAsync(lineId);

        var notice = await notifyRepo.GetByIdAsync(noticeId);
        Assert.NotNull(notice);
        Assert.Equal(800, notice!.ExpectQty);
        Assert.Equal(800m, notice.ExpectTotal);
    }

    [Fact]
    public async Task RecalculateAsync_FullStockInOnly_KeepsPurchaseProgressPartial()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var notifyRepo = new MemoryRepository<StockInNotify>();
        var stockInRepo = new MemoryRepository<StockIn>();
        var stockInItemRepo = new MemoryRepository<StockInItem>();
        var stockInItemExtendRepo = new MemoryRepository<StockInItemExtend>();

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, Status = 50 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            Qty = 4000m,
            Cost = 3.18m,
            Status = 60
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend { Id = lineId });
        await stockInRepo.AddAsync(new StockIn
        {
            Id = stockInId,
            StockInCode = "STI-TEST",
            Status = 2,
            StockInType = StockInTypeCode.Purchase,
            WarehouseId = Guid.NewGuid().ToString()
        });
        await stockInItemExtendRepo.AddAsync(new StockInItemExtend
        {
            Id = stockInItemId,
            StockInId = stockInId,
            PurchaseOrderItemId = lineId
        });
        await stockInItemRepo.AddAsync(new StockInItem
        {
            Id = stockInItemId,
            StockInId = stockInId,
            Quantity = 4000
        });

        var service = new PurchaseOrderItemExtendSyncService(
            poItemRepo,
            poRepo,
            extendRepo,
            notifyRepo,
            new MemoryRepository<FinancePaymentItem>(),
            new MemoryRepository<FinancePayment>(),
            new MemoryRepository<FinancePurchaseInvoiceItem>(),
            new MemoryRepository<FinancePurchaseInvoice>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>(),
            stockInRepo,
            stockInItemRepo,
            stockInItemExtendRepo,
            new MemoryRepository<QCInfo>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderMainStatusSyncService>(),
            Substitute.For<IFinancePurchaseInvoicePaymentSyncService>());

        await service.RecalculateAsync(lineId);

        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(ext);
        Assert.Equal(1, ext!.PurchaseProgressStatus);
        Assert.Equal(2, ext.StockInProgressStatus);
        Assert.Equal(0, ext.PaymentProgressStatus);
    }

    [Fact]
    public async Task RecalculateAsync_FullStockInAndFullPayment_SetsPurchaseProgressComplete()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var paymentId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var notifyRepo = new MemoryRepository<StockInNotify>();
        var stockInRepo = new MemoryRepository<StockIn>();
        var stockInItemRepo = new MemoryRepository<StockInItem>();
        var stockInItemExtendRepo = new MemoryRepository<StockInItemExtend>();
        var payItemRepo = new MemoryRepository<FinancePaymentItem>();
        var paymentRepo = new MemoryRepository<FinancePayment>();

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, Status = 50 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            Qty = 4000m,
            Cost = 3.18m,
            Status = 60
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend { Id = lineId });
        await stockInRepo.AddAsync(new StockIn
        {
            Id = stockInId,
            StockInCode = "STI-TEST",
            Status = 2,
            StockInType = StockInTypeCode.Purchase,
            WarehouseId = Guid.NewGuid().ToString()
        });
        await stockInItemExtendRepo.AddAsync(new StockInItemExtend
        {
            Id = stockInItemId,
            StockInId = stockInId,
            PurchaseOrderItemId = lineId
        });
        await stockInItemRepo.AddAsync(new StockInItem
        {
            Id = stockInItemId,
            StockInId = stockInId,
            Quantity = 4000
        });
        await paymentRepo.AddAsync(new FinancePayment { Id = paymentId, Status = 10 });
        await payItemRepo.AddAsync(new FinancePaymentItem
        {
            Id = Guid.NewGuid().ToString(),
            FinancePaymentId = paymentId,
            PurchaseOrderItemId = lineId,
            PaymentAmountToBe = 12720m,
            VerificationDone = 12720m
        });

        var service = new PurchaseOrderItemExtendSyncService(
            poItemRepo,
            poRepo,
            extendRepo,
            notifyRepo,
            payItemRepo,
            paymentRepo,
            new MemoryRepository<FinancePurchaseInvoiceItem>(),
            new MemoryRepository<FinancePurchaseInvoice>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>(),
            stockInRepo,
            stockInItemRepo,
            stockInItemExtendRepo,
            new MemoryRepository<QCInfo>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderMainStatusSyncService>(),
            Substitute.For<IFinancePurchaseInvoicePaymentSyncService>());

        await service.RecalculateAsync(lineId);

        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(ext);
        Assert.Equal(2, ext!.PurchaseProgressStatus);
        Assert.Equal(2, ext.StockInProgressStatus);
        Assert.Equal(2, ext.PaymentProgressStatus);
    }

    [Theory]
    [InlineData((short)1)]
    [InlineData((short)10)]
    public async Task RecalculateAsync_PostedPurchaseStockIn_SetsStockInProgressComplete(short stockInType)
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var notifyRepo = new MemoryRepository<StockInNotify>();
        var stockInRepo = new MemoryRepository<StockIn>();
        var stockInItemRepo = new MemoryRepository<StockInItem>();
        var stockInItemExtendRepo = new MemoryRepository<StockInItemExtend>();

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, Status = 50 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            Qty = 1680m,
            Cost = 1m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend { Id = lineId });
        await stockInRepo.AddAsync(new StockIn
        {
            Id = stockInId,
            StockInCode = "STI-PURCHASE",
            Status = 2,
            StockInType = stockInType,
            TotalQuantity = 1680,
            WarehouseId = Guid.NewGuid().ToString()
        });
        await stockInItemExtendRepo.AddAsync(new StockInItemExtend
        {
            Id = stockInItemId,
            StockInId = stockInId,
            PurchaseOrderItemId = lineId
        });
        await stockInItemRepo.AddAsync(new StockInItem
        {
            Id = stockInItemId,
            StockInId = stockInId,
            Quantity = 0,
            QtyReceived = 1680
        });

        var service = new PurchaseOrderItemExtendSyncService(
            poItemRepo,
            poRepo,
            extendRepo,
            notifyRepo,
            new MemoryRepository<FinancePaymentItem>(),
            new MemoryRepository<FinancePayment>(),
            new MemoryRepository<FinancePurchaseInvoiceItem>(),
            new MemoryRepository<FinancePurchaseInvoice>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>(),
            stockInRepo,
            stockInItemRepo,
            stockInItemExtendRepo,
            new MemoryRepository<QCInfo>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderMainStatusSyncService>(),
            Substitute.For<IFinancePurchaseInvoicePaymentSyncService>());

        await service.RecalculateAsync(lineId);

        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(ext);
        Assert.Equal(1680m, ext!.QtyReceiveTotal);
        Assert.Equal(2, ext.StockInProgressStatus);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)20)]
    [InlineData((short)30)]
    [InlineData((short)40)]
    public async Task RecalculateAsync_NonPurchaseStockIn_DoesNotCountTowardStockInProgress(short stockInType)
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var stockInRepo = new MemoryRepository<StockIn>();
        var stockInItemRepo = new MemoryRepository<StockInItem>();
        var stockInItemExtendRepo = new MemoryRepository<StockInItemExtend>();

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, Status = 50 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            Qty = 1680m,
            Cost = 1m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend { Id = lineId });
        await stockInRepo.AddAsync(new StockIn
        {
            Id = stockInId,
            Status = 2,
            StockInType = stockInType,
            TotalQuantity = 1680,
            WarehouseId = Guid.NewGuid().ToString()
        });
        await stockInItemExtendRepo.AddAsync(new StockInItemExtend
        {
            Id = stockInItemId,
            StockInId = stockInId,
            PurchaseOrderItemId = lineId
        });
        await stockInItemRepo.AddAsync(new StockInItem
        {
            Id = stockInItemId,
            StockInId = stockInId,
            Quantity = 1680
        });

        var service = new PurchaseOrderItemExtendSyncService(
            poItemRepo,
            poRepo,
            extendRepo,
            new MemoryRepository<StockInNotify>(),
            new MemoryRepository<FinancePaymentItem>(),
            new MemoryRepository<FinancePayment>(),
            new MemoryRepository<FinancePurchaseInvoiceItem>(),
            new MemoryRepository<FinancePurchaseInvoice>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>(),
            stockInRepo,
            stockInItemRepo,
            stockInItemExtendRepo,
            new MemoryRepository<QCInfo>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderMainStatusSyncService>(),
            Substitute.For<IFinancePurchaseInvoicePaymentSyncService>());

        await service.RecalculateAsync(lineId);

        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(ext);
        Assert.Equal(0m, ext!.QtyReceiveTotal);
        Assert.Equal(0, ext.StockInProgressStatus);
    }

    [Fact]
    public async Task RecalculateAsync_TransferStockIn_DoesNotCountTowardStockInProgress()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var stockInRepo = new MemoryRepository<StockIn>();
        var stockInItemRepo = new MemoryRepository<StockInItem>();
        var stockInItemExtendRepo = new MemoryRepository<StockInItemExtend>();

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, Status = 50 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            Qty = 100m,
            Cost = 1m,
            Status = 30
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend { Id = lineId });
        await stockInRepo.AddAsync(new StockIn
        {
            Id = stockInId,
            Status = 2,
            StockInType = StockInTypeCode.Transfer,
            WarehouseId = Guid.NewGuid().ToString()
        });
        await stockInItemExtendRepo.AddAsync(new StockInItemExtend
        {
            Id = stockInItemId,
            StockInId = stockInId,
            PurchaseOrderItemId = lineId
        });
        await stockInItemRepo.AddAsync(new StockInItem
        {
            Id = stockInItemId,
            StockInId = stockInId,
            Quantity = 100
        });

        var service = new PurchaseOrderItemExtendSyncService(
            poItemRepo,
            poRepo,
            extendRepo,
            new MemoryRepository<StockInNotify>(),
            new MemoryRepository<FinancePaymentItem>(),
            new MemoryRepository<FinancePayment>(),
            new MemoryRepository<FinancePurchaseInvoiceItem>(),
            new MemoryRepository<FinancePurchaseInvoice>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>(),
            stockInRepo,
            stockInItemRepo,
            stockInItemExtendRepo,
            new MemoryRepository<QCInfo>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderMainStatusSyncService>(),
            Substitute.For<IFinancePurchaseInvoicePaymentSyncService>());

        await service.RecalculateAsync(lineId);

        var ext = await extendRepo.GetByIdAsync(lineId);
        Assert.NotNull(ext);
        Assert.Equal(0m, ext!.QtyReceiveTotal);
        Assert.Equal(0, ext.StockInProgressStatus);
    }
}
