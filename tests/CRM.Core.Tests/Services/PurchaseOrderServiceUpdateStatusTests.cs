using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class PurchaseOrderServiceUpdateStatusTests
{
    private static PurchaseOrderService CreateService(
        MemoryRepository<PurchaseOrder> poRepo,
        MemoryRepository<PurchaseOrderItem> poItemRepo,
        MemoryRepository<PurchaseOrderItemExtend> extendRepo,
        IPurchaseOrderRevertVendorConfirmGuard? revertGuard = null)
    {
        return new PurchaseOrderService(
            poRepo,
            poItemRepo,
            extendRepo,
            new MemoryRepository<CRM.Core.Models.Sales.SellOrder>(),
            new MemoryRepository<CRM.Core.Models.Sales.SellOrderItem>(),
            Substitute.For<IDataPermissionService>(),
            Substitute.For<IPurchaseOrderListQuery>(),
            Substitute.For<ISerialNumberService>(),
            Substitute.For<IFinanceExchangeRateService>(),
            Substitute.For<IOrderJourneyLogService>(),
            Substitute.For<ISellOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderItemExtendSyncService>(),
            Substitute.For<IPurchaseOrderExtendLineSeqService>(),
            NullLogger<PurchaseOrderService>.Instance,
            revertVendorConfirmGuard: revertGuard);
    }

    [Fact]
    public async Task UpdateStatusAsync_CancelConfirmedOrder_ShouldSyncItemStatusToCancelled()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await poRepo.AddAsync(new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00087",
            Status = 30
        });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            PurchaseOrderItemCode = "PO00087-1",
            Status = 30
        });

        var service = CreateService(poRepo, poItemRepo, extendRepo);
        await service.UpdateStatusAsync(poId, -2);

        var order = await poRepo.GetByIdAsync(poId);
        var item = await poItemRepo.GetByIdAsync(lineId);

        Assert.Equal((short)-2, order!.Status);
        Assert.Equal((short)-2, item!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_RevertVendorConfirm_SyncsLineToPendingConfirm()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var guard = CreateEmptyGuard(poItemRepo);

        await poRepo.AddAsync(new PurchaseOrder
        {
            Id = poId,
            PurchaseOrderCode = "PO00087",
            Status = 30
        });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            PurchaseOrderItemCode = "PO00087-1",
            Status = 30
        });

        var service = CreateService(poRepo, poItemRepo, extendRepo, guard);
        await service.UpdateStatusAsync(poId, 20);

        var order = await poRepo.GetByIdAsync(poId);
        var item = await poItemRepo.GetByIdAsync(lineId);
        Assert.Equal((short)20, order!.Status);
        Assert.Equal((short)20, item!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_RevertVendorConfirm_BlocksWhenActivePaymentExists()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var paymentRepo = new MemoryRepository<FinancePayment>();
        var paymentItemRepo = new MemoryRepository<FinancePaymentItem>();
        var guard = CreateGuard(poItemRepo, paymentRepo: paymentRepo, paymentItemRepo: paymentItemRepo);

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, PurchaseOrderCode = "PO00087", Status = 30 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            PurchaseOrderItemCode = "PO00087-1",
            Status = 30
        });
        var payId = Guid.NewGuid().ToString();
        await paymentRepo.AddAsync(new FinancePayment
        {
            Id = payId,
            FinancePaymentCode = "PAY0099X",
            VendorId = Guid.NewGuid().ToString(),
            Status = 1
        });
        await paymentItemRepo.AddAsync(new FinancePaymentItem
        {
            Id = Guid.NewGuid().ToString(),
            FinancePaymentId = payId,
            PurchaseOrderId = poId,
            PurchaseOrderItemId = lineId
        });

        var service = CreateService(poRepo, poItemRepo, extendRepo, guard);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStatusAsync(poId, 20));
        Assert.Contains("付款单", ex.Message);
        var order = await poRepo.GetByIdAsync(poId);
        Assert.Equal((short)30, order!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_RevertVendorConfirm_AllowsWhenPaymentCancelled()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var poRepo = new MemoryRepository<PurchaseOrder>();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();
        var paymentRepo = new MemoryRepository<FinancePayment>();
        var paymentItemRepo = new MemoryRepository<FinancePaymentItem>();
        var guard = CreateGuard(poItemRepo, paymentRepo: paymentRepo, paymentItemRepo: paymentItemRepo);

        await poRepo.AddAsync(new PurchaseOrder { Id = poId, PurchaseOrderCode = "PO00087", Status = 30 });
        await poItemRepo.AddAsync(new PurchaseOrderItem
        {
            Id = lineId,
            PurchaseOrderId = poId,
            PurchaseOrderItemCode = "PO00087-1",
            Status = 30
        });
        var payId = Guid.NewGuid().ToString();
        await paymentRepo.AddAsync(new FinancePayment
        {
            Id = payId,
            FinancePaymentCode = "PAYDEL",
            VendorId = Guid.NewGuid().ToString(),
            Status = -2
        });
        await paymentItemRepo.AddAsync(new FinancePaymentItem
        {
            Id = Guid.NewGuid().ToString(),
            FinancePaymentId = payId,
            PurchaseOrderItemId = lineId
        });

        var service = CreateService(poRepo, poItemRepo, extendRepo, guard);
        await service.UpdateStatusAsync(poId, 20);
        var order = await poRepo.GetByIdAsync(poId);
        Assert.Equal((short)20, order!.Status);
    }

    [Fact]
    public async Task RevertGuard_BlocksArrivalNotice()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var notifyRepo = new MemoryRepository<StockInNotify>();
        await poItemRepo.AddAsync(new PurchaseOrderItem { Id = lineId, PurchaseOrderId = poId, Status = 30 });
        await notifyRepo.AddAsync(new StockInNotify
        {
            Id = Guid.NewGuid().ToString(),
            NoticeCode = "AN0001",
            PurchaseOrderId = poId,
            PurchaseOrderItemId = lineId
        });

        var guard = CreateGuard(poItemRepo, notifyRepo: notifyRepo);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => guard.EnsureCanRevertAsync(poId, "PO00087"));
        Assert.Contains("到货通知", ex.Message);
    }

    [Fact]
    public async Task RevertGuard_BlocksPostedPurchaseStockIn()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var noticeId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var notifyRepo = new MemoryRepository<StockInNotify>();
        var stockInRepo = new MemoryRepository<StockIn>();
        await poItemRepo.AddAsync(new PurchaseOrderItem { Id = lineId, PurchaseOrderId = poId, Status = 30 });
        await notifyRepo.AddAsync(new StockInNotify
        {
            Id = noticeId,
            NoticeCode = "AN0002",
            PurchaseOrderId = poId,
            PurchaseOrderItemId = lineId
        });
        await stockInRepo.AddAsync(new StockIn
        {
            Id = Guid.NewGuid().ToString(),
            StockInCode = "SI0001",
            WarehouseId = Guid.NewGuid().ToString(),
            SourceId = noticeId,
            Status = StockInHeaderStatusCode.Posted,
            StockInType = StockInTypeCode.Purchase
        });

        var guard = CreateGuard(poItemRepo, notifyRepo: notifyRepo, stockInRepo: stockInRepo);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => guard.EnsureCanRevertAsync(poId, "PO00087"));
        Assert.Contains("入库单", ex.Message);
    }

    [Fact]
    public async Task RevertGuard_BlocksPurchaseInvoiceByOrderCode()
    {
        var poId = Guid.NewGuid().ToString();
        var lineId = Guid.NewGuid().ToString();
        var invId = Guid.NewGuid().ToString();
        var poItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var invoiceItemRepo = new MemoryRepository<FinancePurchaseInvoiceItem>();
        await poItemRepo.AddAsync(new PurchaseOrderItem { Id = lineId, PurchaseOrderId = poId, Status = 30 });
        await invoiceRepo.AddAsync(new FinancePurchaseInvoice
        {
            Id = invId,
            VendorId = Guid.NewGuid().ToString(),
            InvoiceNo = "INV-88"
        });
        await invoiceItemRepo.AddAsync(new FinancePurchaseInvoiceItem
        {
            Id = Guid.NewGuid().ToString(),
            FinancePurchaseInvoiceId = invId,
            PurchaseOrderCode = "PO00087"
        });

        var guard = CreateGuard(poItemRepo, invoiceRepo: invoiceRepo, invoiceItemRepo: invoiceItemRepo);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => guard.EnsureCanRevertAsync(poId, "PO00087"));
        Assert.Contains("进项发票", ex.Message);
    }

    private static PurchaseOrderRevertVendorConfirmGuard CreateEmptyGuard(
        MemoryRepository<PurchaseOrderItem> poItemRepo) =>
        CreateGuard(poItemRepo);

    private static PurchaseOrderRevertVendorConfirmGuard CreateGuard(
        MemoryRepository<PurchaseOrderItem> poItemRepo,
        MemoryRepository<StockInNotify>? notifyRepo = null,
        MemoryRepository<StockIn>? stockInRepo = null,
        MemoryRepository<StockInItemExtend>? extendRepo = null,
        MemoryRepository<FinancePayment>? paymentRepo = null,
        MemoryRepository<FinancePaymentItem>? paymentItemRepo = null,
        MemoryRepository<FinancePurchaseInvoice>? invoiceRepo = null,
        MemoryRepository<FinancePurchaseInvoiceItem>? invoiceItemRepo = null)
    {
        return new PurchaseOrderRevertVendorConfirmGuard(
            poItemRepo,
            notifyRepo ?? new MemoryRepository<StockInNotify>(),
            stockInRepo ?? new MemoryRepository<StockIn>(),
            extendRepo ?? new MemoryRepository<StockInItemExtend>(),
            paymentRepo ?? new MemoryRepository<FinancePayment>(),
            paymentItemRepo ?? new MemoryRepository<FinancePaymentItem>(),
            invoiceRepo ?? new MemoryRepository<FinancePurchaseInvoice>(),
            invoiceItemRepo ?? new MemoryRepository<FinancePurchaseInvoiceItem>());
    }
}
