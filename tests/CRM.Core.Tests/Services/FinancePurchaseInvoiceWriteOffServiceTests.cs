using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public class FinancePurchaseInvoiceWriteOffServiceTests
{
    [Fact]
    public async Task ReverseByInvoiceAsync_FullWriteOff_RestoresInvoiceAndStockInBalances()
    {
        var ctx = await SeedFullWriteOffAsync(invoiceAmount: 1000m, writeOffAmount: 1000m);
        var result = await ctx.Sut.ReverseByInvoiceAsync(ctx.InvoiceId, "user-1");

        Assert.Equal(1, result.WriteOffCount);
        Assert.Equal(1000m, result.ReversedTotal);
        Assert.Contains("SI001", result.StockInCodes);

        var invoice = await ctx.InvoiceRepo.GetByIdAsync(ctx.InvoiceId);
        Assert.NotNull(invoice);
        Assert.Equal(0m, invoice!.VerifiedDone);
        Assert.Equal(1000m, invoice.VerifiedToBe);
        Assert.Equal((short)0, invoice.VerificationStatus);
        Assert.Equal(0m, invoice.PaymentDone);
        Assert.Equal(0m, invoice.PaymentToBe);
        Assert.Equal((byte)0, invoice.PaymentStatus);

        var remaining = (await ctx.WriteOffRepo.FindAsync(w => w.FinancePurchaseInvoiceId == ctx.InvoiceId)).ToList();
        Assert.Empty(remaining);
        Assert.True(ctx.WriteOffRepo.Snapshot().Single().IsDeleted);

        var ext = await ctx.ExtendRepo.GetByIdAsync(ctx.StockInItemId);
        Assert.NotNull(ext);
        Assert.Equal(0m, ext!.InvoiceMatchDone);
        Assert.Equal(1000m, ext.InvoiceMatchToBe);
        Assert.Equal((short)0, ext.InvoiceMatchStatus);

        await ctx.PoExtendSync.Received(1).RecalculateAsync(ctx.PoItemId, Arg.Any<CancellationToken>());
        await ctx.LineSeq.Received(1).UpsertInvoiceMatchCacheAsync(
            ctx.StockInId, 0m, 1000m, 0, Arg.Any<byte?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReverseByInvoiceAsync_KeepsOtherInvoiceWriteOffOnSameItem()
    {
        var ctx = await SeedFullWriteOffAsync(invoiceAmount: 800m, writeOffAmount: 800m, stockInItemAmount: 1000m);
        await ctx.WriteOffRepo.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = Guid.NewGuid().ToString(),
            FinancePurchaseInvoiceId = "other-inv",
            StockInItemId = ctx.StockInItemId,
            StockInId = ctx.StockInId,
            PurchaseOrderItemId = ctx.PoItemId,
            Amount = 200m,
            Currency = 1
        });
        var extBefore = await ctx.ExtendRepo.GetByIdAsync(ctx.StockInItemId);
        extBefore!.InvoiceMatchDone = 1000m;
        extBefore.InvoiceMatchToBe = 0m;
        extBefore.InvoiceMatchStatus = 2;
        await ctx.ExtendRepo.UpdateAsync(extBefore);

        await ctx.Sut.ReverseByInvoiceAsync(ctx.InvoiceId, "user-1");

        var ext = await ctx.ExtendRepo.GetByIdAsync(ctx.StockInItemId);
        Assert.Equal(200m, ext!.InvoiceMatchDone);
        Assert.Equal(800m, ext.InvoiceMatchToBe);
        Assert.Equal((short)1, ext.InvoiceMatchStatus);
    }

    [Fact]
    public async Task ReverseByInvoiceAsync_WhenStockInDeleted_StillSucceeds()
    {
        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var writeOffRepo = new MemoryRepository<FinancePurchaseInvoiceWriteOff>();
        var invoiceId = Guid.NewGuid().ToString();
        await invoiceRepo.AddAsync(new FinancePurchaseInvoice
        {
            Id = invoiceId,
            VendorId = "v1",
            InvoiceCode = "INVI002",
            InvoiceAmount = 500m,
            VerifiedDone = 500m,
            VerifiedToBe = 0m,
            VerificationStatus = 2
        });
        await writeOffRepo.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = Guid.NewGuid().ToString(),
            FinancePurchaseInvoiceId = invoiceId,
            StockInItemId = "missing-item",
            StockInId = "missing-si",
            PurchaseOrderItemId = "missing-po",
            Amount = 500m,
            Currency = 1
        });

        var lineSeq = Substitute.For<IStockInExtendLineSeqService>();
        var poExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();
        var sut = CreateSut(
            invoiceRepo,
            writeOffRepo,
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockInItemExtend>(),
            lineSeq,
            poExtendSync);

        var result = await sut.ReverseByInvoiceAsync(invoiceId, "user-1");

        Assert.Equal(1, result.WriteOffCount);
        var invoice = await invoiceRepo.GetByIdAsync(invoiceId);
        Assert.Equal(0m, invoice!.VerifiedDone);
        Assert.Equal(500m, invoice.VerifiedToBe);
        Assert.Empty(result.StockInCodes);
        await lineSeq.DidNotReceiveWithAnyArgs().UpsertInvoiceMatchCacheAsync(
            default!, default, default, default, default, default);
        await poExtendSync.Received(1).RecalculateAsync("missing-po", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReverseByInvoiceAsync_WhenNoWriteOff_Throws()
    {
        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var invoiceId = Guid.NewGuid().ToString();
        await invoiceRepo.AddAsync(new FinancePurchaseInvoice
        {
            Id = invoiceId,
            VendorId = "v1",
            InvoiceAmount = 100m,
            VerifiedToBe = 100m
        });
        var sut = CreateSut(
            invoiceRepo,
            new MemoryRepository<FinancePurchaseInvoiceWriteOff>(),
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockInItemExtend>(),
            Substitute.For<IStockInExtendLineSeqService>(),
            Substitute.For<IPurchaseOrderItemExtendSyncService>());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.ReverseByInvoiceAsync(invoiceId, "user-1"));
        Assert.Contains("无需反核销", ex.Message);
    }

    [Fact]
    public async Task ReverseByInvoiceAsync_WhenRedInvoice_Throws()
    {
        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var writeOffRepo = new MemoryRepository<FinancePurchaseInvoiceWriteOff>();
        var invoiceId = Guid.NewGuid().ToString();
        await invoiceRepo.AddAsync(new FinancePurchaseInvoice
        {
            Id = invoiceId,
            VendorId = "v1",
            RedInvoiceStatus = 1,
            InvoiceAmount = 100m,
            VerifiedDone = 100m
        });
        await writeOffRepo.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = Guid.NewGuid().ToString(),
            FinancePurchaseInvoiceId = invoiceId,
            StockInItemId = "sii-1",
            StockInId = "si-1",
            Amount = 100m
        });
        var sut = CreateSut(
            invoiceRepo,
            writeOffRepo,
            new MemoryRepository<StockIn>(),
            new MemoryRepository<StockInItem>(),
            new MemoryRepository<StockInItemExtend>(),
            Substitute.For<IStockInExtendLineSeqService>(),
            Substitute.For<IPurchaseOrderItemExtendSyncService>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ReverseByInvoiceAsync(invoiceId, "user-1"));
        Assert.Contains("已冲红", ex.Message);
    }

    [Fact]
    public async Task ReverseVerificationAsync_AcceptsInvoiceCodeOrInvoiceNo_AndLogs()
    {
        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var invoice = new FinancePurchaseInvoice
        {
            Id = Guid.NewGuid().ToString(),
            VendorId = "v1",
            InvoiceCode = "INVI00AA",
            InvoiceNo = "PAPER-001",
            InvoiceAmount = 10m,
            VerifiedDone = 10m,
            VerificationStatus = 2
        };
        await invoiceRepo.AddAsync(invoice);

        var writeOff = Substitute.For<IFinancePurchaseInvoiceWriteOffService>();
        writeOff.ReverseByInvoiceAsync(invoice.Id, "user-1", Arg.Any<CancellationToken>())
            .Returns(new FinancePurchaseInvoiceWriteOffReverseResult
            {
                FinancePurchaseInvoiceId = invoice.Id,
                WriteOffCount = 1,
                ReversedTotal = 10m
            });
        var log = Substitute.For<ILogOperationAppendService>();
        var svc = CreateInvoiceService(invoiceRepo, writeOff, log);

        await svc.ReverseVerificationAsync(invoice.Id, "INVI00AA", "user-1", "admin");
        await svc.ReverseVerificationAsync(invoice.Id, "PAPER-001", "user-1", "admin");

        var mismatch = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.ReverseVerificationAsync(invoice.Id, "WRONG", "user-1", null));
        Assert.Contains("确认单号不匹配", mismatch.Message);

        await writeOff.Received(2).ReverseByInvoiceAsync(invoice.Id, "user-1", Arg.Any<CancellationToken>());
        await log.Received(2).AppendAsync(
            BusinessLogTypes.FinancePurchaseInvoice,
            invoice.Id,
            invoice.InvoiceCode,
            OperationLogActionTypes.FinancePurchaseInvoiceReverseVerification,
            "user-1",
            "admin",
            Arg.Any<string>());
    }

    private static async Task<SeedContext> SeedFullWriteOffAsync(
        decimal invoiceAmount, decimal writeOffAmount, decimal? stockInItemAmount = null)
    {
        var invoiceId = Guid.NewGuid().ToString();
        var stockInId = Guid.NewGuid().ToString();
        var stockInItemId = Guid.NewGuid().ToString();
        var poItemId = Guid.NewGuid().ToString();

        var invoiceRepo = new MemoryRepository<FinancePurchaseInvoice>();
        var writeOffRepo = new MemoryRepository<FinancePurchaseInvoiceWriteOff>();
        var stockInRepo = new MemoryRepository<StockIn>();
        var stockInItemRepo = new MemoryRepository<StockInItem>();
        var extendRepo = new MemoryRepository<StockInItemExtend>();
        var poExtendRepo = new MemoryRepository<PurchaseOrderItemExtend>();

        await invoiceRepo.AddAsync(new FinancePurchaseInvoice
        {
            Id = invoiceId,
            VendorId = "v1",
            InvoiceCode = "INVI001",
            InvoiceAmount = invoiceAmount,
            VerifiedDone = writeOffAmount,
            VerifiedToBe = invoiceAmount - writeOffAmount,
            VerificationStatus = 2,
            PaymentDone = writeOffAmount,
            PaymentToBe = 0m,
            PaymentStatus = 2
        });
        await writeOffRepo.AddAsync(new FinancePurchaseInvoiceWriteOff
        {
            Id = Guid.NewGuid().ToString(),
            FinancePurchaseInvoiceId = invoiceId,
            StockInItemId = stockInItemId,
            StockInId = stockInId,
            PurchaseOrderItemId = poItemId,
            Amount = writeOffAmount,
            Currency = 1
        });
        await stockInRepo.AddAsync(new StockIn
        {
            Id = stockInId,
            StockInCode = "SI001",
            WarehouseId = "w1",
            Status = 2
        });
        var lineAmount = stockInItemAmount ?? invoiceAmount;
        await stockInItemRepo.AddAsync(new StockInItem
        {
            Id = stockInItemId,
            StockInId = stockInId,
            MaterialId = "m1",
            Amount = lineAmount
        });
        await extendRepo.AddAsync(new StockInItemExtend
        {
            Id = stockInItemId,
            StockInId = stockInId,
            PurchaseOrderItemId = poItemId,
            InvoiceMatchDone = writeOffAmount,
            InvoiceMatchToBe = lineAmount - writeOffAmount,
            InvoiceMatchStatus = 2
        });
        await poExtendRepo.AddAsync(new PurchaseOrderItemExtend
        {
            Id = poItemId,
            PaymentAmountFinish = writeOffAmount
        });

        var lineSeq = Substitute.For<IStockInExtendLineSeqService>();
        var poExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();
        var sut = CreateSut(
            invoiceRepo,
            writeOffRepo,
            stockInRepo,
            stockInItemRepo,
            extendRepo,
            lineSeq,
            poExtendSync,
            poExtendRepo);

        return new SeedContext(sut, invoiceRepo, writeOffRepo, extendRepo, lineSeq, poExtendSync,
            invoiceId, stockInId, stockInItemId, poItemId);
    }

    private static FinancePurchaseInvoiceWriteOffService CreateSut(
        IRepository<FinancePurchaseInvoice> invoiceRepo,
        IRepository<FinancePurchaseInvoiceWriteOff> writeOffRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<StockInItem> stockInItemRepo,
        IRepository<StockInItemExtend> extendRepo,
        IStockInExtendLineSeqService lineSeq,
        IPurchaseOrderItemExtendSyncService poExtendSync,
        IRepository<PurchaseOrderItemExtend>? poExtendRepo = null)
    {
        var paymentSync = new FinancePurchaseInvoicePaymentSyncService(
            invoiceRepo,
            writeOffRepo,
            poExtendRepo ?? new MemoryRepository<PurchaseOrderItemExtend>());
        return new FinancePurchaseInvoiceWriteOffService(
            invoiceRepo,
            writeOffRepo,
            stockInRepo,
            stockInItemRepo,
            extendRepo,
            new MemoryRepository<PurchaseOrderItem>(),
            new MemoryRepository<PurchaseOrder>(),
            new MemoryRepository<VendorInfo>(),
            new MemoryRepository<User>(),
            Substitute.For<IDataPermissionService>(),
            poExtendSync,
            lineSeq,
            paymentSync);
    }

    private static FinancePurchaseInvoiceService CreateInvoiceService(
        IRepository<FinancePurchaseInvoice> invoiceRepo,
        IFinancePurchaseInvoiceWriteOffService writeOff,
        ILogOperationAppendService log)
    {
        return new FinancePurchaseInvoiceService(
            invoiceRepo,
            new MemoryRepository<FinancePurchaseInvoiceItem>(),
            Substitute.For<IDataPermissionService>(),
            Substitute.For<IPurchaseOrderItemExtendSyncService>(),
            Substitute.For<IForceDeleteGuardService>(),
            log,
            Substitute.For<IFinancePurchaseInvoiceListQuery>(),
            new MemoryRepository<VendorInfo>(),
            Substitute.For<ISerialNumberService>(),
            Substitute.For<IFinancePurchaseInvoicePaymentSyncService>(),
            writeOff);
    }

    private sealed record SeedContext(
        FinancePurchaseInvoiceWriteOffService Sut,
        MemoryRepository<FinancePurchaseInvoice> InvoiceRepo,
        MemoryRepository<FinancePurchaseInvoiceWriteOff> WriteOffRepo,
        MemoryRepository<StockInItemExtend> ExtendRepo,
        IStockInExtendLineSeqService LineSeq,
        IPurchaseOrderItemExtendSyncService PoExtendSync,
        string InvoiceId,
        string StockInId,
        string StockInItemId,
        string PoItemId);
}
