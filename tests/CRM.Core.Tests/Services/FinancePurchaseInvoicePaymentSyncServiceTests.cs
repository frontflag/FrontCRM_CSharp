using CRM.Core.Models.Finance;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using Xunit;

namespace CRM.Core.Tests.Services;

public class FinancePurchaseInvoicePaymentSyncServiceTests
{
    [Fact]
    public async Task RecalculateForInvoiceAsync_FullPayAgainstVerifiedDone_IsComplete()
    {
        var invoiceId = Guid.NewGuid().ToString();
        var poItemId = Guid.NewGuid().ToString();
        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var writeOffRepo = new MemoryRepository<FinancePurchaseInvoiceWriteOff>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await invoiceRepo.AddAsync(new FinancePurchaseInvoice
        {
            Id = invoiceId,
            VendorId = "v1",
            InvoiceAmount = 1000m,
            VerifiedDone = 800m,
            VerifiedToBe = 200m,
            VerificationStatus = 1
        });
        await writeOffRepo.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = Guid.NewGuid().ToString(),
            FinancePurchaseInvoiceId = invoiceId,
            StockInItemId = "si1",
            StockInId = "s1",
            PurchaseOrderItemId = poItemId,
            Amount = 800m
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = poItemId,
            PaymentAmountFinish = 800m
        });

        var svc = new FinancePurchaseInvoicePaymentSyncService(invoiceRepo, writeOffRepo, extendRepo);
        await svc.RecalculateForInvoiceAsync(invoiceId);

        var inv = await invoiceRepo.GetByIdAsync(invoiceId);
        Assert.NotNull(inv);
        Assert.Equal(800m, inv!.PaymentDone);
        Assert.Equal(0m, inv.PaymentToBe);
        Assert.Equal((byte)2, inv.PaymentStatus);
    }

    [Fact]
    public async Task RecalculateForInvoiceAsync_PayLessThanVerifiedDone_IsPartial()
    {
        var invoiceId = Guid.NewGuid().ToString();
        var poItemId = Guid.NewGuid().ToString();
        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var writeOffRepo = new MemoryRepository<FinancePurchaseInvoiceWriteOff>();
        var extendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await invoiceRepo.AddAsync(new FinancePurchaseInvoice
        {
            Id = invoiceId,
            VendorId = "v1",
            InvoiceAmount = 1000m,
            VerifiedDone = 800m,
            VerificationStatus = 1
        });
        await writeOffRepo.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = Guid.NewGuid().ToString(),
            FinancePurchaseInvoiceId = invoiceId,
            StockInItemId = "si1",
            StockInId = "s1",
            PurchaseOrderItemId = poItemId,
            Amount = 800m
        });
        await extendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = poItemId,
            PaymentAmountFinish = 799.99m
        });

        var svc = new FinancePurchaseInvoicePaymentSyncService(invoiceRepo, writeOffRepo, extendRepo);
        await svc.RecalculateForInvoiceAsync(invoiceId);

        var inv = await invoiceRepo.GetByIdAsync(invoiceId);
        Assert.NotNull(inv);
        Assert.Equal(799.99m, inv!.PaymentDone);
        Assert.Equal(0.01m, inv.PaymentToBe);
        Assert.Equal((byte)1, inv.PaymentStatus);
    }
}
