using System.Linq.Expressions;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Services;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services;

public class CustomsAgencyRateInboundCostRefreshServiceTests
{
    private readonly IRepository<CustomsBroker> _brokerRepo = Substitute.For<IRepository<CustomsBroker>>();
    private readonly IRepository<CustomsDeclaration> _decRepo = Substitute.For<IRepository<CustomsDeclaration>>();
    private readonly IRepository<CustomsDeclarationItem> _itemRepo = Substitute.For<IRepository<CustomsDeclarationItem>>();
    private readonly IRepository<StockInNotify> _notifyRepo = Substitute.For<IRepository<StockInNotify>>();
    private readonly IRepository<StockIn> _stockInRepo = Substitute.For<IRepository<StockIn>>();
    private readonly IRepository<StockInItem> _stockInItemRepo = Substitute.For<IRepository<StockInItem>>();
    private readonly IRepository<StockItem> _stockItemRepo = Substitute.For<IRepository<StockItem>>();
    private readonly IFinanceExchangeRateService _fx = Substitute.For<IFinanceExchangeRateService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CustomsAgencyRateInboundCostRefreshService _service;

    private readonly List<CustomsDeclaration> _declarations = new();
    private readonly List<CustomsDeclarationItem> _items = new();
    private readonly List<StockInNotify> _notifies = new();
    private readonly List<StockIn> _stockIns = new();
    private readonly List<StockInItem> _stockInItems = new();
    private readonly List<StockItem> _stockItems = new();

    public CustomsAgencyRateInboundCostRefreshServiceTests()
    {
        _uow.SaveChangesAsync().Returns(1);
        _fx.GetCurrentAsync(Arg.Any<CancellationToken>()).Returns(new FinanceExchangeRateDto
        {
            UsdToCny = 7m,
            UsdToHkd = 7.8m,
            UsdToEur = 0.9m
        });
        BindFind(_decRepo, _declarations);
        BindFind(_itemRepo, _items);
        BindFind(_notifyRepo, _notifies);
        BindFind(_stockInRepo, _stockIns);
        BindFind(_stockInItemRepo, _stockInItems);
        BindFind(_stockItemRepo, _stockItems);

        _service = new CustomsAgencyRateInboundCostRefreshService(
            _brokerRepo,
            _decRepo,
            _itemRepo,
            _notifyRepo,
            _stockInRepo,
            _stockInItemRepo,
            _stockItemRepo,
            new CustomsFeeCalculator(),
            _fx,
            _uow);
    }

    [Fact]
    public async Task RefreshAsync_LockedSystemDeclaration_RecalculatesFeesAndInboundCost()
    {
        var brokerId = "broker-1";
        var broker = new CustomsBroker { Id = brokerId, AgencyRate = 1m };
        _brokerRepo.GetByIdAsync(brokerId).Returns(broker);

        var dec = new CustomsDeclaration
        {
            Id = "dec-1",
            DeclarationCode = "CDS0000A",
            CustomsBrokerId = brokerId,
            FeesLocked = true,
            AgencyRateManual = false,
            BrokerAgencyRate = 1m,
            InternalStatus = CustomsDeclarationInternalStatus.Processing
        };
        _declarations.Add(dec);

        var item = new CustomsDeclarationItem
        {
            Id = "cdi-1",
            DeclarationId = dec.Id,
            LineNo = 1,
            DeclareQty = 10,
            CustomsPaymentGoods = 8000m,
            DutyAmount = 800m,
            VatAmount = 1144m,
            OtherFee = 50m,
            CustomsAgencyFee = 0m,
            TotalValueTax = 9994m,
            TaxIncludedUnitPrice = 999.4m
        };
        _items.Add(item);

        var notify = new StockInNotify
        {
            Id = "an-1",
            StockInType = StockInTypeCode.Customs,
            CustomsDeclarationItemId = item.Id,
            ExpectQty = 10,
            Cost = 999.4m,
            ExpectTotal = 9994m
        };
        _notifies.Add(notify);

        var stockIn = new StockIn
        {
            Id = "si-1",
            StockInType = StockInTypeCode.Customs,
            SourceId = notify.Id
        };
        _stockIns.Add(stockIn);

        var line = new StockInItem
        {
            Id = "sii-1",
            StockInId = stockIn.Id,
            Quantity = 10,
            Price = 999.4m,
            Amount = 9994m
        };
        _stockInItems.Add(line);

        var layer = new StockItem
        {
            Id = "layer-1",
            StockInId = stockIn.Id,
            QtyInbound = 10,
            PurchasePrice = 999.4m,
            PurchaseAmount = 9994m,
            PurchasePriceUsd = 100m
        };
        _stockItems.Add(layer);

        var result = await _service.RefreshAsync(brokerId, 1.025m, "user-1");

        Assert.Equal(1, result.TotalDeclarations);
        Assert.Equal(0, result.SkippedManual);
        Assert.Equal(1, result.RefreshedDeclarations);
        Assert.Equal(1, result.FeesChangedDeclarations);
        Assert.Equal(1, result.StockItemLayersUpdated);
        Assert.Equal(248.60m, item.CustomsAgencyFee);
        Assert.Equal(1024.26m, item.TaxIncludedUnitPrice);
        Assert.Equal(1.025m, dec.BrokerAgencyRate);
        Assert.True(dec.FeesLocked);
        Assert.False(dec.AgencyRateManual);
        Assert.Equal(1024.26m, layer.PurchasePrice);
        Assert.Equal(10242.60m, layer.PurchaseAmount);
        Assert.Equal(1m, broker.AgencyRate);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RefreshAsync_LegacyAgencyRateManualDeclaration_StillRefreshes()
    {
        var brokerId = "broker-1";
        _brokerRepo.GetByIdAsync(brokerId).Returns(new CustomsBroker { Id = brokerId, AgencyRate = 1.03m });

        var dec = new CustomsDeclaration
        {
            Id = "dec-m",
            DeclarationCode = "CDS0000M",
            CustomsBrokerId = brokerId,
            AgencyRateManual = true,
            BrokerAgencyRate = 1.08m,
            InternalStatus = CustomsDeclarationInternalStatus.Processing
        };
        _declarations.Add(dec);
        _items.Add(new CustomsDeclarationItem
        {
            Id = "cdi-m",
            DeclarationId = dec.Id,
            DeclareQty = 1,
            CustomsPaymentGoods = 1000m,
            DutyAmount = 0m,
            VatAmount = 0m,
            OtherFee = 0m,
            CustomsAgencyFee = 80m,
            TotalValueTax = 1080m,
            TaxIncludedUnitPrice = 1080m
        });

        var result = await _service.RefreshAsync(brokerId, 1.025m, null);

        Assert.Equal(0, result.SkippedManual);
        Assert.Equal(1, result.RefreshedDeclarations);
        Assert.Equal(25m, _items[0].CustomsAgencyFee);
        Assert.False(dec.AgencyRateManual);
        await _uow.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RefreshAsync_DoesNotWriteBrokerMasterRate()
    {
        var brokerId = "broker-1";
        var broker = new CustomsBroker { Id = brokerId, AgencyRate = 1.000000m };
        _brokerRepo.GetByIdAsync(brokerId).Returns(broker);
        _declarations.Add(new CustomsDeclaration
        {
            Id = "dec-empty",
            CustomsBrokerId = brokerId,
            AgencyRateManual = false,
            InternalStatus = CustomsDeclarationInternalStatus.Voided
        });

        await _service.RefreshAsync(brokerId, 1.025m, null);

        Assert.Equal(1.000000m, broker.AgencyRate);
        await _brokerRepo.DidNotReceive().UpdateAsync(Arg.Any<CustomsBroker>());
    }

    private static void BindFind<T>(IRepository<T> repo, List<T> source) where T : BaseEntity
    {
        repo.FindAsync(Arg.Any<Expression<Func<T, bool>>>())
            .Returns(ci =>
            {
                var pred = ci.Arg<Expression<Func<T, bool>>>().Compile();
                return source.Where(pred).ToList();
            });
    }
}
