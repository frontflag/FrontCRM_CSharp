using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;

namespace CRM.Core.Tests.Services;

public class ForceDeleteGuardServiceTests
{
    [Fact]
    public async Task CanForceDeleteFinancePaymentAsync_ShouldBlockWhenVerified()
    {
        var sut = CreateSut(out var payItems, out _, out _, out _, out _, out _, out _, out _, out _);
        await payItems.AddAsync(new FinancePaymentItem
        {
            Id = "pi-1",
            FinancePaymentId = "pay-1",
            VerificationStatus = 2,
            VerificationDone = 100m
        });

        var result = await sut.CanForceDeleteFinancePaymentAsync("pay-1");

        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task CanForceDeleteFinanceSellInvoiceAsync_ShouldAllowWhenNoReceive()
    {
        var sut = CreateSut(out _, out _, out _, out var sellInvRepo, out var sellInvItemRepo, out _, out _, out _, out _);
        await sellInvRepo.AddAsync(new FinanceSellInvoice
        {
            Id = "si-1",
            InvoiceCode = "SI001",
            ReceiveStatus = 0,
            ReceiveDone = 0m
        });
        await sellInvItemRepo.AddAsync(new SellInvoiceItem
        {
            Id = "sii-1",
            FinanceSellInvoiceId = "si-1",
            ReceiveStatus = 0
        });

        var result = await sut.CanForceDeleteFinanceSellInvoiceAsync("si-1");

        Assert.True(result.CanDelete);
    }

    [Fact]
    public async Task CanForceDeleteStockOutRequestAsync_ShouldBlockWhenHasActivePickingTask()
    {
        var sut = CreateSut(out _, out _, out _, out _, out _, out var reqRepo, out _, out var pickingRepo, out _);
        await reqRepo.AddAsync(new StockOutRequest { Id = "sor-1", RequestCode = "SOR001" });
        await pickingRepo.AddAsync(new PickingTask
        {
            Id = "pt-1",
            TaskCode = "PAK001",
            StockOutRequestId = "sor-1",
            WarehouseId = "w-1",
            OperatorId = "u-1",
            Status = 1
        });

        var result = await sut.CanForceDeleteStockOutRequestAsync("sor-1");

        Assert.False(result.CanDelete);
    }

    private static ForceDeleteGuardService CreateSut(
        out MemoryRepository<FinancePaymentItem> financePaymentItemRepo,
        out MemoryRepository<FinanceReceiptItem> financeReceiptItemRepo,
        out MemoryRepository<FinancePurchaseInvoice> financePurchaseInvoiceRepo,
        out MemoryRepository<FinanceSellInvoice> financeSellInvoiceRepo,
        out MemoryRepository<SellInvoiceItem> financeSellInvoiceItemRepo,
        out MemoryRepository<StockOutRequest> stockOutRequestRepo,
        out MemoryRepository<StockOut> stockOutRepo,
        out MemoryRepository<PickingTask> pickingTaskRepo,
        out MemoryRepository<StockOutItem> stockOutItemRepo)
    {
        financePaymentItemRepo = new MemoryRepository<FinancePaymentItem>();
        financeReceiptItemRepo = new MemoryRepository<FinanceReceiptItem>();
        financePurchaseInvoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        financeSellInvoiceRepo = new MemoryRepository<FinanceSellInvoice>();
        financeSellInvoiceItemRepo = new MemoryRepository<SellInvoiceItem>();
        stockOutRequestRepo = new MemoryRepository<StockOutRequest>();
        stockOutRepo = new MemoryRepository<StockOut>();
        pickingTaskRepo = new MemoryRepository<PickingTask>();
        stockOutItemRepo = new MemoryRepository<StockOutItem>();
        var purchaseOrderItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var financeReceiptRepo = new MemoryRepository<FinanceReceipt>();
        return new ForceDeleteGuardService(
            financePaymentItemRepo,
            financeReceiptItemRepo,
            financePurchaseInvoiceRepo,
            financeSellInvoiceRepo,
            financeSellInvoiceItemRepo,
            stockOutRequestRepo,
            pickingTaskRepo,
            stockOutRepo,
            stockOutItemRepo,
            purchaseOrderItemRepo,
            financeReceiptRepo);
    }
}
