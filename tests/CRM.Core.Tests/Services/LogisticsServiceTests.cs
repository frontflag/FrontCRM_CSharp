using System.Linq;
using System.Linq.Expressions;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;
using CRM.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public class LogisticsServiceTests
{
    [Fact]
    public async Task GetQcsAsync_ModelFilter_ShouldMatchPnFromPurchaseOrderItems()
    {
        var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
        var stockInRepo = Substitute.For<IRepository<StockIn>>();
        var qcRepo = Substitute.For<IRepository<QCInfo>>();
        var qcItemRepo = Substitute.For<IRepository<QCItem>>();
        var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
        var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
        var poItemExtendRepo = Substitute.For<IRepository<PurchaseOrderItemExtend>>();
        var sellOrderItemRepo = Substitute.For<IRepository<SellOrderItem>>();
        var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
        var serial = Substitute.For<ISerialNumberService>();
        var uow = Substitute.For<IUnitOfWork>();

        qcRepo.GetAllAsync().Returns(new[]
        {
            new QCInfo
            {
                Id = "qc-1",
                QcCode = "QC0001",
                StockInNotifyId = "notice-1",
                CreateTime = DateTime.UtcNow
            }
        });
        qcItemRepo.GetAllAsync().Returns(Array.Empty<QCItem>());
        notifyRepo.GetAllAsync().Returns(new[]
        {
            new StockInNotify
            {
                Id = "notice-1",
                PurchaseOrderId = "po-1",
                PurchaseOrderCode = "PO0001",
                VendorName = "Vendor A",
                PurchaseOrderItemId = "poi-1",
                Pn = null
            }
        });
        poItemRepo.GetAllAsync().Returns(new[]
        {
            new PurchaseOrderItem
            {
                Id = "poi-1",
                PurchaseOrderId = "po-1",
                SellOrderItemId = "soi-1",
                PN = "UG-MPN-455565"
            }
        });
        sellOrderItemRepo.GetAllAsync().Returns(Array.Empty<SellOrderItem>());
        sellOrderRepo.GetAllAsync().Returns(Array.Empty<SellOrder>());

        var poExtendSync = Substitute.For<IPurchaseOrderItemExtendSyncService>();
        var userService = Substitute.For<IUserService>();
        userService.GetAllAsync().Returns(Array.Empty<User>());
        var log = Substitute.For<ILogger<LogisticsService>>();
        var stockInItemExtendRepo = Substitute.For<IRepository<StockInItemExtend>>();
        stockInItemExtendRepo.FindAsync(Arg.Any<Expression<Func<StockInItemExtend, bool>>>())
            .Returns(Task.FromResult(Enumerable.Empty<StockInItemExtend>()));
        var svc = new LogisticsService(
            notifyRepo, stockInRepo, stockInItemExtendRepo, qcRepo, qcItemRepo, poRepo, poItemRepo, poItemExtendRepo,
            sellOrderItemRepo, sellOrderRepo, serial, poExtendSync, uow, userService,
            Substitute.For<ILogOperationAppendService>(),
            log,
            Substitute.For<IQcListQuery>(),
            Substitute.For<IRepository<VendorInfo>>(),
            Substitute.For<IRepository<WarehouseInfo>>(),
            Substitute.For<ICustomsTraceQuery>());

        var result = await svc.GetQcsAsync(new QcQueryRequest { Model = "UG-MPN-455565" });

        Assert.Single(result);
        Assert.Contains("UG-MPN-455565", result[0].Model ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetQcsAsync_Brand_ShouldPreferPurchaseOrderItemOverStaleNoticeSnapshot()
    {
        var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
        var stockInRepo = Substitute.For<IRepository<StockIn>>();
        var qcRepo = Substitute.For<IRepository<QCInfo>>();
        var qcItemRepo = Substitute.For<IRepository<QCItem>>();
        var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
        var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
        var poItemExtendRepo = Substitute.For<IRepository<PurchaseOrderItemExtend>>();
        var sellOrderItemRepo = Substitute.For<IRepository<SellOrderItem>>();
        var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
        var serial = Substitute.For<ISerialNumberService>();
        var uow = Substitute.For<IUnitOfWork>();

        qcRepo.GetAllAsync().Returns(new[]
        {
            new QCInfo
            {
                Id = "qc-1",
                QcCode = "QC0002E",
                StockInNotifyId = "notice-1",
                CreateTime = DateTime.UtcNow
            }
        });
        qcItemRepo.GetAllAsync().Returns(Array.Empty<QCItem>());
        notifyRepo.GetAllAsync().Returns(new[]
        {
            new StockInNotify
            {
                Id = "notice-1",
                PurchaseOrderId = "po-1",
                PurchaseOrderCode = "PO0002P",
                PurchaseOrderItemId = "poi-1",
                Pn = "MG10ACA20TE",
                Brand = "TOSHIBA"
            }
        });
        poItemRepo.GetAllAsync().Returns(new[]
        {
            new PurchaseOrderItem
            {
                Id = "poi-1",
                PurchaseOrderId = "po-1",
                PN = "MG10ACA20TE",
                Brand = "TOSHIBA/东芝"
            }
        });
        sellOrderItemRepo.GetAllAsync().Returns(Array.Empty<SellOrderItem>());
        sellOrderRepo.GetAllAsync().Returns(Array.Empty<SellOrder>());
        poRepo.FindAsync(Arg.Any<Expression<Func<PurchaseOrder, bool>>>())
            .Returns(Task.FromResult(Enumerable.Empty<PurchaseOrder>()));

        var userService = Substitute.For<IUserService>();
        userService.GetAllAsync().Returns(Array.Empty<User>());
        var stockInItemExtendRepo = Substitute.For<IRepository<StockInItemExtend>>();
        stockInItemExtendRepo.FindAsync(Arg.Any<Expression<Func<StockInItemExtend, bool>>>())
            .Returns(Task.FromResult(Enumerable.Empty<StockInItemExtend>()));
        var svc = new LogisticsService(
            notifyRepo, stockInRepo, stockInItemExtendRepo, qcRepo, qcItemRepo, poRepo, poItemRepo, poItemExtendRepo,
            sellOrderItemRepo, sellOrderRepo, serial, Substitute.For<IPurchaseOrderItemExtendSyncService>(), uow, userService,
            Substitute.For<ILogOperationAppendService>(),
            Substitute.For<ILogger<LogisticsService>>(),
            Substitute.For<IQcListQuery>(),
            Substitute.For<IRepository<VendorInfo>>(),
            Substitute.For<IRepository<WarehouseInfo>>(),
            Substitute.For<ICustomsTraceQuery>());

        var result = await svc.GetQcsAsync();

        Assert.Single(result);
        Assert.Equal("TOSHIBA/东芝", result[0].Brand);
        Assert.DoesNotContain("TOSHIBA, ", result[0].Brand ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetArrivalNoticeOpsAggregatesAsync_CustomsNotify_ShouldResolveOriginalPurchaseFromTrace()
    {
        var noticeId = "notice-customs-1";
        var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
        var stockInRepo = Substitute.For<IRepository<StockIn>>();
        var qcRepo = Substitute.For<IRepository<QCInfo>>();
        var qcItemRepo = Substitute.For<IRepository<QCItem>>();
        var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
        var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
        var poItemExtendRepo = Substitute.For<IRepository<PurchaseOrderItemExtend>>();
        var sellOrderItemRepo = Substitute.For<IRepository<SellOrderItem>>();
        var sellOrderRepo = Substitute.For<IRepository<SellOrder>>();
        var serial = Substitute.For<ISerialNumberService>();
        var uow = Substitute.For<IUnitOfWork>();
        var customsTrace = Substitute.For<ICustomsTraceQuery>();

        notifyRepo.GetByIdAsync(noticeId).Returns(new StockInNotify
        {
            Id = noticeId,
            StockInType = StockInTypeCode.Customs,
            CustomsDeclarationItemId = "cdi-1",
            PurchaseOrderItemId = string.Empty
        });
        qcRepo.FindAsync(Arg.Any<Expression<Func<QCInfo, bool>>>())
            .Returns(Task.FromResult(Enumerable.Empty<QCInfo>()));
        stockInRepo.FindAsync(Arg.Any<Expression<Func<StockIn, bool>>>())
            .Returns(Task.FromResult(Enumerable.Empty<StockIn>()));

        customsTrace.ResolveOriginalPurchaseByArrivalNotifyAsync("cdi-1", StockInTypeCode.Customs, Arg.Any<CancellationToken>())
            .Returns(new CustomsOriginalPurchaseLinkDto
            {
                PurchaseOrderItemId = "poi-orig-1",
                PurchaseOrderItemCode = "POI0009",
                PurchaseOrderId = "po-orig-1",
                PurchaseUserName = "buyer-a",
                PurchaseOrderCreateTime = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Qty = 500m
            });

        var svc = new LogisticsService(
            notifyRepo, stockInRepo, Substitute.For<IRepository<StockInItemExtend>>(), qcRepo, qcItemRepo, poRepo, poItemRepo, poItemExtendRepo,
            sellOrderItemRepo, sellOrderRepo, serial, Substitute.For<IPurchaseOrderItemExtendSyncService>(), uow,
            Substitute.For<IUserService>(),
            Substitute.For<ILogOperationAppendService>(),
            Substitute.For<ILogger<LogisticsService>>(),
            Substitute.For<IQcListQuery>(),
            Substitute.For<IRepository<VendorInfo>>(),
            Substitute.For<IRepository<WarehouseInfo>>(),
            customsTrace);

        var result = await svc.GetArrivalNoticeOpsAggregatesAsync(noticeId);

        Assert.NotNull(result.Purchase);
        Assert.Equal("POI0009", result.Purchase!.PurchaseOrderItemCode);
        Assert.Equal("po-orig-1", result.Purchase.PurchaseOrderId);
        Assert.Equal("buyer-a", result.Purchase.PurchaseUserName);
        Assert.Equal(500m, result.Purchase.Qty);
    }
}
