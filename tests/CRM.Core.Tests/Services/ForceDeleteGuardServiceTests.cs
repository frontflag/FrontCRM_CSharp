using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;

namespace CRM.Core.Tests.Services;

public class ForceDeleteGuardServiceTests
{
    [Fact]
    public async Task CanForceDeleteFinancePaymentAsync_ShouldBlockWhenVerified()
    {
        var sut = CreateSut(out var payItems, out _, out _, out _, out _, out _, out _, out _, out _, out _);
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
        var sut = CreateSut(out _, out _, out _, out var sellInvRepo, out var sellInvItemRepo, out _, out _, out _, out _, out _);
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
        var sut = CreateSut(out _, out _, out _, out _, out _, out var reqRepo, out _, out var packingItemRepo, out var pickingRepo, out _);
        await reqRepo.AddAsync(new StockOutRequest { Id = "sor-1", RequestCode = "SOR001" });
        await packingItemRepo.AddAsync(new PackingItem
        {
            Id = "pi-1",
            PackingId = "pk-1",
            StockOutNotifyId = "sor-1",
            ItemCode = "PK-1",
            SellOrderId = "so-1",
            SellOrderItemId = "soi-1",
            Pn = "PN",
            Qty = 1
        });
        await pickingRepo.AddAsync(new PickingTask
        {
            Id = "pt-1",
            TaskCode = "PAK001",
            PackingId = "pk-1",
            WarehouseId = "w-1",
            OperatorId = "u-1",
            Status = 1
        });

        var result = await sut.CanForceDeleteStockOutRequestAsync("sor-1");

        Assert.False(result.CanDelete);
    }

    [Fact]
    public async Task CanForceDeleteFinancePurchaseInvoiceAsync_ShouldBlockWhenHasWriteOff()
    {
        var invoices = new MemoryRepository<FinancePurchaseInvoice>();
        var writeOffs = new MemoryRepository<FinancePurchaseInvoiceWriteOff>();
        await invoices.AddAsync(new FinancePurchaseInvoice
        {
            Id = "inv-1",
            VendorId = "v1",
            InvoiceCode = "INVI001",
            ConfirmStatus = 0,
            RedInvoiceStatus = 0
        });
        await writeOffs.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = "wo-1",
            FinancePurchaseInvoiceId = "inv-1",
            StockInId = "si-1",
            StockInItemId = "sii-1",
            Amount = 100m
        });

        var sut = CreateGuard(invoices, writeOffs);
        var result = await sut.CanForceDeleteFinancePurchaseInvoiceAsync("inv-1");

        Assert.False(result.CanDelete);
        Assert.Contains("反核销", result.Message);
    }

    [Fact]
    public async Task CanForceDeleteStockInAsync_ShouldBlockWhenHasWriteOff_AndListInvoiceCode()
    {
        var invoices = new MemoryRepository<FinancePurchaseInvoice>();
        var writeOffs = new MemoryRepository<FinancePurchaseInvoiceWriteOff>();
        await invoices.AddAsync(new FinancePurchaseInvoice
        {
            Id = "inv-1",
            VendorId = "v1",
            InvoiceCode = "INVI0099"
        });
        await writeOffs.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = "wo-1",
            FinancePurchaseInvoiceId = "inv-1",
            StockInId = "si-1",
            StockInItemId = "sii-1",
            Amount = 50m
        });

        var sut = CreateGuard(invoices, writeOffs);
        var result = await sut.CanForceDeleteStockInAsync("si-1");

        Assert.False(result.CanDelete);
        Assert.Contains("INVI0099", result.Message);
    }

    [Fact]
    public async Task CanForceDeleteStockInAsync_ShouldAllowWhenNoWriteOff()
    {
        var sut = CreateGuard(
            new MemoryRepository<FinancePurchaseInvoice>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>());

        var result = await sut.CanForceDeleteStockInAsync("si-1");

        Assert.True(result.CanDelete);
    }

    private static ForceDeleteGuardService CreateGuard(
        MemoryRepository<FinancePurchaseInvoice> invoices,
        MemoryRepository<FinancePurchaseInvoiceWriteOff> writeOffs)
    {
        return new ForceDeleteGuardService(
            new MemoryRepository<FinancePaymentItem>(),
            new MemoryRepository<FinanceReceiptItem>(),
            invoices,
            new MemoryRepository<FinanceSellInvoice>(),
            new MemoryRepository<SellInvoiceItem>(),
            new MemoryRepository<StockOutRequest>(),
            new MemoryRepository<PackingItem>(),
            new MemoryRepository<PickingTask>(),
            new MemoryRepository<StockOut>(),
            new MemoryRepository<StockOutItem>(),
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<FinanceReceipt>(),
            new MemoryRepository<FinanceReceivable>(),
            new MemoryRepository<Packing>(),
            new MemoryRepository<CustomsDeclaration>(),
            writeOffs);
    }

    private static ForceDeleteGuardService CreateSut(
        out MemoryRepository<FinancePaymentItem> financePaymentItemRepo,
        out MemoryRepository<FinanceReceiptItem> financeReceiptItemRepo,
        out MemoryRepository<FinancePurchaseInvoice> financePurchaseInvoiceRepo,
        out MemoryRepository<FinanceSellInvoice> financeSellInvoiceRepo,
        out MemoryRepository<SellInvoiceItem> financeSellInvoiceItemRepo,
        out MemoryRepository<StockOutRequest> stockOutRequestRepo,
        out MemoryRepository<StockOut> stockOutRepo,
        out MemoryRepository<PackingItem> packingItemRepo,
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
        packingItemRepo = new MemoryRepository<PackingItem>();
        pickingTaskRepo = new MemoryRepository<PickingTask>();
        stockOutItemRepo = new MemoryRepository<StockOutItem>();
        var purchaseOrderItemRepo = new MemoryRepository<PurchaseOrderItem>();
        var financeReceiptRepo = new MemoryRepository<FinanceReceipt>();
        var financeReceivableRepo = new MemoryRepository<FinanceReceivable>();
        return new ForceDeleteGuardService(
            financePaymentItemRepo,
            financeReceiptItemRepo,
            financePurchaseInvoiceRepo,
            financeSellInvoiceRepo,
            financeSellInvoiceItemRepo,
            stockOutRequestRepo,
            packingItemRepo,
            pickingTaskRepo,
            stockOutRepo,
            stockOutItemRepo,
            purchaseOrderItemRepo,
            financeReceiptRepo,
            financeReceivableRepo,
            new MemoryRepository<Packing>(),
            new MemoryRepository<CustomsDeclaration>(),
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>());
    }
}
