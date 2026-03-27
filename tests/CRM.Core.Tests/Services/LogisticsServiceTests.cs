using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using NSubstitute;

namespace CRM.Core.Tests.Services;

public class LogisticsServiceTests
{
    [Fact]
    public async Task GetQcsAsync_ModelFilter_ShouldMatchPnFromPurchaseOrderItems()
    {
        var notifyRepo = Substitute.For<IRepository<StockInNotify>>();
        var notifyItemRepo = Substitute.For<IRepository<StockInNotifyItem>>();
        var qcRepo = Substitute.For<IRepository<QCInfo>>();
        var qcItemRepo = Substitute.For<IRepository<QCItem>>();
        var poRepo = Substitute.For<IRepository<PurchaseOrder>>();
        var poItemRepo = Substitute.For<IRepository<PurchaseOrderItem>>();
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
                VendorName = "Vendor A"
            }
        });
        // 到货通知明细里没有目标 PN，模拟历史问题场景
        notifyItemRepo.GetAllAsync().Returns(new[]
        {
            new StockInNotifyItem
            {
                Id = "ni-1",
                StockInNotifyId = "notice-1",
                PurchaseOrderItemId = "poi-1",
                Pn = "OTHER-PN"
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

        var svc = new LogisticsService(
            notifyRepo, notifyItemRepo, qcRepo, qcItemRepo, poRepo, poItemRepo,
            sellOrderItemRepo, sellOrderRepo, serial, uow);

        var result = await svc.GetQcsAsync(new QcQueryRequest { Model = "UG-MPN-455565" });

        Assert.Single(result);
        Assert.Contains("UG-MPN-455565", result[0].Model ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
