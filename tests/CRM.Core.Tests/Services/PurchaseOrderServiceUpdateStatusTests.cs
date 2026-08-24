using CRM.Core.Interfaces;
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
        MemoryRepository<PurchaseOrderItemExtend> extendRepo)
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
            NullLogger<PurchaseOrderService>.Instance);
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
}
