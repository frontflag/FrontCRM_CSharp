using System.Linq.Expressions;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public class PurchaseOrderPurchasePriceDownstreamSyncServiceTests
{
    private readonly List<StockInNotify> _notices = new();
    private readonly List<StockIn> _stockIns = new();
    private readonly List<StockInItem> _stockInItems = new();
    private readonly List<StockInItemExtend> _stockInExtends = new();
    private readonly List<StockItem> _stockItems = new();
    private readonly List<StockOutItem> _stockOutItems = new();
    private readonly List<StockOutItemExtend> _stockOutExtends = new();
    private readonly List<FinancePayment> _payments = new();
    private readonly List<FinancePaymentItem> _payItems = new();
    private readonly PurchaseOrderPurchasePriceDownstreamSyncService _service;

    public PurchaseOrderPurchasePriceDownstreamSyncServiceTests()
    {
        var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
        var stockInRepo = Substitute.For<IRepository<StockIn>>();
        var stockInItemRepo = Substitute.For<IRepository<StockInItem>>();
        var stockInExtendRepo = Substitute.For<IRepository<StockInItemExtend>>();
        var stockItemRepo = Substitute.For<IRepository<StockItem>>();
        var stockOutItemRepo = Substitute.For<IRepository<StockOutItem>>();
        var stockOutExtendRepo = Substitute.For<IRepository<StockOutItemExtend>>();
        var paymentRepo = Substitute.For<IRepository<FinancePayment>>();
        var payItemRepo = Substitute.For<IRepository<FinancePaymentItem>>();

        Bind(notifyRepo, _notices);
        Bind(stockInRepo, _stockIns);
        Bind(stockInItemRepo, _stockInItems);
        Bind(stockInExtendRepo, _stockInExtends);
        Bind(stockItemRepo, _stockItems);
        Bind(stockOutItemRepo, _stockOutItems);
        Bind(stockOutExtendRepo, _stockOutExtends);
        Bind(paymentRepo, _payments);
        Bind(payItemRepo, _payItems);

        _service = new PurchaseOrderPurchasePriceDownstreamSyncService(
            notifyRepo,
            stockInRepo,
            stockInItemRepo,
            stockInExtendRepo,
            stockItemRepo,
            stockOutItemRepo,
            stockOutExtendRepo,
            paymentRepo,
            payItemRepo,
            NullLogger<PurchaseOrderPurchasePriceDownstreamSyncService>.Instance);
    }

    [Fact]
    public async Task ApplyAsync_WhenCostChanged_ShouldOverwriteDownstreamSnapshots()
    {
        var item = NewPoItem(cost: 6m, convert: 6m, qty: 4m);
        _notices.Add(new StockInNotify
        {
            Id = "N-1",
            PurchaseOrderItemId = item.Id,
            ExpectQty = 2,
            ReceiveQty = 2,
            Cost = 10m,
            ExpectTotal = 20m,
            ReceiveTotal = 20m
        });
        _notices.Add(new StockInNotify
        {
            Id = "N-2",
            PurchaseOrderItemId = item.Id,
            ExpectQty = 2,
            ReceiveQty = 1,
            Cost = 10m,
            ExpectTotal = 20m,
            ReceiveTotal = 10m
        });
        _stockInItems.Add(new StockInItem
        {
            Id = "SII-1",
            StockInId = "SI-1",
            StockInItemCode = "IN001-1",
            Quantity = 2,
            Price = 10m,
            Currency = 2,
            Amount = 20m
        });
        _stockInExtends.Add(new StockInItemExtend
        {
            Id = "SII-1",
            StockInId = "SI-1",
            PurchaseOrderItemId = item.Id,
            InvoiceMatchDone = 0m,
            InvoiceMatchToBe = 20m,
            InvoiceMatchStatus = FinanceVerificationStatusCode.Pending
        });
        _stockIns.Add(new StockIn { Id = "SI-1", TotalAmount = 20m });
        var layer = new StockItem
        {
            Id = "STK-1",
            PurchaseOrderItemId = item.Id,
            SellOrderItemId = "SOI-1",
            PurchasePrice = 10m,
            PurchaseCurrency = 2,
            PurchasePriceUsd = 10m,
            PurchaseAmount = 40m,
            SalesPriceUsd = 20m,
            QtyInbound = 4
        };
        layer.SyncDenormalizedComputedFields();
        _stockItems.Add(layer);
        _stockOutItems.Add(new StockOutItem
        {
            Id = "SOI-1",
            StockOutId = "SOUT-1",
            ActualQty = 2,
            Quantity = 2
        });
        _stockOutExtends.Add(new StockOutItemExtend
        {
            Id = "SOI-1",
            PurchaseOrderItemId = item.Id,
            SellOrderItemId = "SOI-1",
            QtyStockOut = 2,
            PurchasePrice = 10m,
            PurchaseCurrency = 2,
            PurchasePriceUsd = 10m,
            SalesPrice = 20m,
            SalesCurrency = 2,
            SalesPriceUsd = 20m,
            ProfitOutBizUsd = 20m,
            OriginalPurchasePrice = 9m
        });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.Equal(2, result.ArrivalNoticesUpdated);
        Assert.Equal(1, result.StockInItemsUpdated);
        Assert.Equal(1, result.StockInHeadersUpdated);
        Assert.Equal(1, result.StockItemsUpdated);
        Assert.Equal(1, result.StockOutItemExtendsUpdated);
        Assert.Equal(6m, _notices[0].Cost);
        Assert.Equal(12m, _notices[0].ExpectTotal);
        Assert.Equal(6m, _notices[1].Cost);
        Assert.Equal(2, _notices[1].ExpectQty);
        Assert.Equal(6m, _stockInItems[0].Price);
        Assert.Equal(12m, _stockInItems[0].Amount);
        Assert.Equal(12m, _stockInExtends[0].InvoiceMatchToBe);
        Assert.Equal(12m, _stockIns[0].TotalAmount);
        Assert.Equal(6m, _stockItems[0].PurchasePrice);
        Assert.Equal(6m, _stockItems[0].PurchasePriceUsd);
        Assert.Equal(24m, _stockItems[0].PurchaseAmount);
        Assert.Equal(56m, _stockItems[0].ProfitOutBizUsd);
        Assert.Equal(6m, _stockOutExtends[0].PurchasePrice);
        Assert.Equal(28m, _stockOutExtends[0].ProfitOutBizUsd);
        Assert.Equal(9m, _stockOutExtends[0].OriginalPurchasePrice);
        Assert.Equal(20m, _stockOutExtends[0].SalesPrice);
        var line = Assert.Single(result.LineChanges);
        Assert.Equal(10m, line.OldCost);
        Assert.Equal(6m, line.NewCost);
        Assert.Empty(result.InvoiceMatchWarnings);
        Assert.Empty(result.PaymentOverWarnings);
    }

    [Fact]
    public async Task ApplyAsync_WhenInvoiceMatchDoneExceedsNewAmount_ShouldNotBlockAndWarn()
    {
        var item = NewPoItem(cost: 8m, convert: 8m, qty: 2m);
        _stockInItems.Add(new StockInItem
        {
            Id = "SII-OVER",
            StockInId = "SI-OVER",
            StockInItemCode = "IN0099-1",
            Quantity = 2,
            Price = 10m,
            Currency = 2,
            Amount = 20m
        });
        _stockInExtends.Add(new StockInItemExtend
        {
            Id = "SII-OVER",
            StockInId = "SI-OVER",
            PurchaseOrderItemId = item.Id,
            InvoiceMatchDone = 20m,
            InvoiceMatchToBe = 0m,
            InvoiceMatchStatus = FinanceVerificationStatusCode.Complete
        });
        _stockIns.Add(new StockIn { Id = "SI-OVER", TotalAmount = 20m });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.Equal(1, result.StockInItemsUpdated);
        Assert.Equal(16m, _stockInItems[0].Amount);
        Assert.Equal(20m, _stockInExtends[0].InvoiceMatchDone);
        Assert.Equal(-4m, _stockInExtends[0].InvoiceMatchToBe);
        Assert.Equal(FinanceVerificationStatusCode.Complete, _stockInExtends[0].InvoiceMatchStatus);
        var warning = Assert.Single(result.InvoiceMatchWarnings);
        Assert.Equal("IN0099-1", warning.StockInItemCode);
        Assert.Equal(16m, warning.Amount);
        Assert.Equal(20m, warning.InvoiceMatchDone);
    }

    [Fact]
    public async Task ApplyAsync_WhenPaymentDoneExceedsNewLineAmount_ShouldNotBlockAndWarn()
    {
        var item = NewPoItem(cost: 8m, convert: 8m, qty: 2m);
        _payments.Add(new FinancePayment { Id = "PAY-1", Status = 100 });
        _payments.Add(new FinancePayment { Id = "PAY-CANCEL", Status = -2 });
        _payItems.Add(new FinancePaymentItem
        {
            Id = "PI-1",
            FinancePaymentId = "PAY-1",
            PurchaseOrderItemId = item.Id,
            VerificationDone = 20m
        });
        _payItems.Add(new FinancePaymentItem
        {
            Id = "PI-CANCEL",
            FinancePaymentId = "PAY-CANCEL",
            PurchaseOrderItemId = item.Id,
            VerificationDone = 50m
        });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.True(result.HasUpdates);
        var warning = Assert.Single(result.PaymentOverWarnings);
        Assert.Equal(16m, warning.LineAmount);
        Assert.Equal(20m, warning.PaymentDone);
        Assert.Equal(20m, _payItems[0].VerificationDone);
        Assert.Equal(50m, _payItems[1].VerificationDone);
    }

    [Fact]
    public async Task ApplyAsync_WhenCostsAlreadyMatch_ShouldNoOp()
    {
        var item = NewPoItem(cost: 10m, convert: 10m, qty: 2m);
        _notices.Add(new StockInNotify
        {
            Id = "N-OK",
            PurchaseOrderItemId = item.Id,
            ExpectQty = 2,
            Cost = 10m,
            ExpectTotal = 20m
        });
        _stockInItems.Add(new StockInItem
        {
            Id = "SII-OK",
            StockInId = "SI-OK",
            Quantity = 2,
            Price = 10m,
            Currency = 2,
            Amount = 20m
        });
        _stockInExtends.Add(new StockInItemExtend
        {
            Id = "SII-OK",
            StockInId = "SI-OK",
            PurchaseOrderItemId = item.Id,
            InvoiceMatchToBe = 20m
        });
        _stockIns.Add(new StockIn { Id = "SI-OK", TotalAmount = 20m });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.False(result.HasUpdates);
        Assert.Equal(0, result.ArrivalNoticesUpdated);
        Assert.Equal(0, result.StockInItemsUpdated);
    }

    [Fact]
    public async Task ApplyAsync_MixedStockIn_ShouldRecalcHeaderUsingEachLineAmount()
    {
        var item = NewPoItem(cost: 30m, convert: 30m, qty: 2m);
        _stockInItems.Add(new StockInItem
        {
            Id = "SII-A",
            StockInId = "SI-MIX",
            Quantity = 2,
            Price = 10m,
            Currency = 2,
            Amount = 20m
        });
        _stockInItems.Add(new StockInItem
        {
            Id = "SII-B",
            StockInId = "SI-MIX",
            Quantity = 1,
            Price = 5m,
            Currency = 2,
            Amount = 5m
        });
        _stockInExtends.Add(new StockInItemExtend
        {
            Id = "SII-A",
            StockInId = "SI-MIX",
            PurchaseOrderItemId = item.Id
        });
        _stockInExtends.Add(new StockInItemExtend
        {
            Id = "SII-B",
            StockInId = "SI-MIX",
            PurchaseOrderItemId = "OTHER-LINE"
        });
        _stockIns.Add(new StockIn { Id = "SI-MIX", TotalAmount = 0m });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.Equal(1, result.StockInItemsUpdated);
        Assert.Equal(1, result.StockInHeadersUpdated);
        Assert.Equal(30m, _stockInItems[0].Price);
        Assert.Equal(60m, _stockInItems[0].Amount);
        Assert.Equal(5m, _stockInItems[1].Price);
        Assert.Equal(65m, _stockIns[0].TotalAmount);
    }

    private static PurchaseOrderItem NewPoItem(decimal cost, decimal convert, decimal qty) => new()
    {
        Id = "POI-LINE-1",
        PurchaseOrderItemCode = "PO001-1",
        PurchaseOrderId = "PO-1",
        Cost = cost,
        Currency = 2,
        ConvertPrice = convert,
        Qty = qty
    };

    private static void Bind<T>(IRepository<T> repo, List<T> store) where T : CRM.Core.Models.BaseEntity
    {
        repo.FindAsync(Arg.Any<Expression<Func<T, bool>>>())
            .Returns(call =>
            {
                var pred = call.Arg<Expression<Func<T, bool>>>().Compile();
                return store.Where(pred).ToList();
            });
        repo.UpdateAsync(Arg.Any<T>()).Returns(Task.CompletedTask);
    }
}
