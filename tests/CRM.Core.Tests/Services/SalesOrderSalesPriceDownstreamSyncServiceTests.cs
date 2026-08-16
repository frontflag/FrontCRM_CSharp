using System.Linq.Expressions;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public class SalesOrderSalesPriceDownstreamSyncServiceTests
{
    private readonly List<PackingItemExtend> _packingExtends = new();
    private readonly List<StockItem> _stockItems = new();
    private readonly List<StockOut> _stockOuts = new();
    private readonly List<StockOutItem> _stockOutItems = new();
    private readonly List<StockOutItemExtend> _stockOutExtends = new();
    private readonly List<FinanceReceivable> _receivables = new();
    private readonly SalesOrderSalesPriceDownstreamSyncService _service;

    public SalesOrderSalesPriceDownstreamSyncServiceTests()
    {
        var packingRepo = Substitute.For<IRepository<PackingItemExtend>>();
        var stockItemRepo = Substitute.For<IRepository<StockItem>>();
        var stockOutRepo = Substitute.For<IRepository<StockOut>>();
        var stockOutItemRepo = Substitute.For<IRepository<StockOutItem>>();
        var stockOutExtendRepo = Substitute.For<IRepository<StockOutItemExtend>>();
        var receivableRepo = Substitute.For<IRepository<FinanceReceivable>>();

        Bind(packingRepo, _packingExtends);
        Bind(stockItemRepo, _stockItems);
        Bind(stockOutRepo, _stockOuts);
        Bind(stockOutItemRepo, _stockOutItems);
        Bind(stockOutExtendRepo, _stockOutExtends);
        Bind(receivableRepo, _receivables);

        _service = new SalesOrderSalesPriceDownstreamSyncService(
            packingRepo,
            stockItemRepo,
            stockOutRepo,
            stockOutItemRepo,
            stockOutExtendRepo,
            receivableRepo,
            NullLogger<SalesOrderSalesPriceDownstreamSyncService>.Instance);
    }

    [Fact]
    public async Task ApplyAsync_WhenPriceChanged_ShouldOverwriteDownstreamSnapshots()
    {
        var item = NewSoItem(price: 20m, convert: 20m);
        _packingExtends.Add(new PackingItemExtend
        {
            Id = "PIE-1",
            PackingItemId = "PI-1",
            SellOrderItemId = item.Id,
            Price = 10m,
            PriceCurrency = 2,
            PriceConvertPrice = 10m
        });
        var layer = new StockItem
        {
            Id = "STK-1",
            SellOrderItemId = item.Id,
            SalesPrice = 10m,
            SalesCurrency = 2,
            SalesPriceUsd = 10m,
            PurchasePriceUsd = 6m,
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
            SellOrderItemId = item.Id,
            QtyStockOut = 2,
            SalesPrice = 10m,
            SalesCurrency = 2,
            SalesPriceUsd = 10m,
            PurchasePriceUsd = 6m,
            ProfitOutBizUsd = 8m
        });
        _stockOuts.Add(new StockOut { Id = "SOUT-1", StockOutCode = "OUT001", TotalAmount = 20m });
        _receivables.Add(new FinanceReceivable
        {
            Id = "AR-1",
            ReceivableCode = "AR0001",
            SellOrderItemId = item.Id,
            OutboundQty = 2m,
            UnitPrice = 10m,
            Currency = 2,
            Amount = 20m,
            VerifiedDone = 0m,
            VerifiedToBe = 20m,
            InvoiceMatchDone = 0m,
            InvoiceMatchToBe = 20m
        });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.Equal(1, result.PackingItemExtendsUpdated);
        Assert.Equal(1, result.StockItemsUpdated);
        Assert.Equal(1, result.StockOutItemExtendsUpdated);
        Assert.Equal(1, result.StockOutHeadersUpdated);
        Assert.Equal(1, result.ReceivablesUpdated);
        Assert.Equal(20m, _packingExtends[0].Price);
        Assert.Equal(20m, _stockItems[0].SalesPrice);
        Assert.Equal(20m, _stockItems[0].SalesPriceUsd);
        Assert.Equal(56m, _stockItems[0].ProfitOutBizUsd);
        Assert.Equal(20m, _stockOutExtends[0].SalesPrice);
        Assert.Equal(28m, _stockOutExtends[0].ProfitOutBizUsd);
        Assert.Equal(40m, _stockOuts[0].TotalAmount);
        Assert.Equal(20m, _receivables[0].UnitPrice);
        Assert.Equal(40m, _receivables[0].Amount);
        Assert.Equal(40m, _receivables[0].VerifiedToBe);
        Assert.Equal(FinanceVerificationStatusCode.Pending, _receivables[0].VerificationStatus);
        var line = Assert.Single(result.LineChanges);
        Assert.Equal(10m, line.OldPrice);
        Assert.Equal(20m, line.NewPrice);
        Assert.Empty(result.ReceivableWarnings);
    }

    [Fact]
    public async Task ApplyAsync_WhenVerifiedDoneExceedsNewAmount_ShouldNotBlockAndWarn()
    {
        var item = NewSoItem(price: 8m, convert: 8m);
        _receivables.Add(new FinanceReceivable
        {
            Id = "AR-OVER",
            ReceivableCode = "AR0099",
            SellOrderItemId = item.Id,
            OutboundQty = 2m,
            UnitPrice = 10m,
            Currency = 2,
            Amount = 20m,
            VerifiedDone = 20m,
            VerifiedToBe = 0m,
            VerificationStatus = FinanceVerificationStatusCode.Complete,
            InvoiceMatchDone = 20m,
            InvoiceMatchToBe = 0m,
            InvoiceMatchStatus = 2
        });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.Equal(1, result.ReceivablesUpdated);
        Assert.Equal(16m, _receivables[0].Amount);
        Assert.Equal(20m, _receivables[0].VerifiedDone);
        Assert.Equal(-4m, _receivables[0].VerifiedToBe);
        Assert.Equal(20m, _receivables[0].InvoiceMatchDone);
        Assert.Equal(-4m, _receivables[0].InvoiceMatchToBe);
        Assert.Equal(FinanceVerificationStatusCode.Complete, _receivables[0].VerificationStatus);
        var warning = Assert.Single(result.ReceivableWarnings);
        Assert.True(warning.VerifiedOverAmount);
        Assert.True(warning.InvoiceMatchOverAmount);
        Assert.Equal("AR0099", warning.ReceivableCode);
    }

    [Fact]
    public async Task ApplyAsync_WhenPricesAlreadyMatch_ShouldNoOp()
    {
        var item = NewSoItem(price: 10m, convert: 10m);
        _packingExtends.Add(new PackingItemExtend
        {
            Id = "PIE-OK",
            PackingItemId = "PI-OK",
            SellOrderItemId = item.Id,
            Price = 10m,
            PriceCurrency = 2,
            PriceConvertPrice = 10m
        });
        _receivables.Add(new FinanceReceivable
        {
            Id = "AR-OK",
            SellOrderItemId = item.Id,
            OutboundQty = 1m,
            UnitPrice = 10m,
            Currency = 2,
            Amount = 10m,
            VerifiedToBe = 10m,
            InvoiceMatchToBe = 10m
        });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.False(result.HasUpdates);
        Assert.Equal(0, result.PackingItemExtendsUpdated);
        Assert.Equal(0, result.ReceivablesUpdated);
    }

    [Fact]
    public async Task ApplyAsync_MixedStockOut_ShouldRecalcHeaderUsingEachLinePrice()
    {
        var item = NewSoItem(price: 30m, convert: 30m);
        _stockOutItems.Add(new StockOutItem { Id = "SOI-A", StockOutId = "SOUT-MIX", ActualQty = 2, Quantity = 2 });
        _stockOutItems.Add(new StockOutItem { Id = "SOI-B", StockOutId = "SOUT-MIX", ActualQty = 1, Quantity = 1 });
        _stockOutExtends.Add(new StockOutItemExtend
        {
            Id = "SOI-A",
            SellOrderItemId = item.Id,
            QtyStockOut = 2,
            SalesPrice = 10m,
            SalesCurrency = 2,
            SalesPriceUsd = 10m
        });
        _stockOutExtends.Add(new StockOutItemExtend
        {
            Id = "SOI-B",
            SellOrderItemId = "OTHER-LINE",
            QtyStockOut = 1,
            SalesPrice = 5m,
            SalesCurrency = 2,
            SalesPriceUsd = 5m
        });
        _stockOuts.Add(new StockOut { Id = "SOUT-MIX", TotalAmount = 0m });

        var result = await _service.ApplyAsync(new[] { item });

        Assert.Equal(1, result.StockOutItemExtendsUpdated);
        Assert.Equal(1, result.StockOutHeadersUpdated);
        Assert.Equal(30m, _stockOutExtends[0].SalesPrice);
        Assert.Equal(5m, _stockOutExtends[1].SalesPrice);
        Assert.Equal(65m, _stockOuts[0].TotalAmount);
    }

    private static SellOrderItem NewSoItem(decimal price, decimal convert) => new()
    {
        Id = "SOI-LINE-1",
        SellOrderItemCode = "SO001-1",
        SellOrderId = "SO-1",
        Price = price,
        Currency = 2,
        ConvertPrice = convert
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
