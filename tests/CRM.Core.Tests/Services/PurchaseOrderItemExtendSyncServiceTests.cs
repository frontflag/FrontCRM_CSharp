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
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockInItemExtend>(),
            new MemoryRepository<QCInfo>(),
            Substitute.For<ISellOrderItemExtendSyncService>());
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
}
