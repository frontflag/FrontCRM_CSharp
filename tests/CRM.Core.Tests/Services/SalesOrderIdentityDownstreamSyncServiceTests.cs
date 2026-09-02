using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Services;
using CRM.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CRM.Core.Tests.Services;

public class SalesOrderIdentityDownstreamSyncServiceTests
{
    [Fact]
    public async Task ApplyAsync_Pn_UpdatesNotifyPackingExtendAndReceivable()
    {
        const string lineId = "line-1";
        var notifyRepo = new MemoryRepository<StockOutRequest>();
        var packingItemRepo = new MemoryRepository<PackingItem>();
        var packingExtendRepo = new MemoryRepository<PackingItemExtend>();
        var receivableRepo = new MemoryRepository<FinanceReceivable>();

        await notifyRepo.AddAsync(new StockOutRequest
        {
            Id = "n-1",
            RequestCode = "STOR001",
            SalesOrderItemId = lineId,
            MaterialCode = "OLD-PN",
            MaterialName = "OLD-BRAND"
        });
        await packingItemRepo.AddAsync(new PackingItem
        {
            Id = "pi-1",
            ItemCode = "PK001-1",
            SellOrderItemId = lineId,
            Pn = "OLD-PN",
            Brand = "OLD-BRAND"
        });
        await packingExtendRepo.AddAsync(new PackingItemExtend
        {
            Id = "pie-1",
            PackingItemId = "pi-1",
            SellOrderItemId = lineId,
            CustomerPn = "OLD-CPN",
            CustomerBrand = "OLD-CBRAND"
        });
        await receivableRepo.AddAsync(new FinanceReceivable
        {
            Id = "ar-1",
            ReceivableCode = "AR001",
            SellOrderItemId = lineId,
            PN = "OLD-PN",
            Brand = "OLD-BRAND"
        });

        var service = new SalesOrderIdentityDownstreamSyncService(
            notifyRepo,
            packingItemRepo,
            packingExtendRepo,
            receivableRepo,
            NullLogger<SalesOrderIdentityDownstreamSyncService>.Instance);

        var result = await service.ApplyAsync(
            new[]
            {
                new SellOrderItem
                {
                    Id = lineId,
                    PN = "NEW-PN",
                    Brand = "NEW-BRAND",
                    CustomerPn = "NEW-CPN",
                    CustomerBrand = "NEW-CBRAND"
                }
            },
            SalesOrderIdentitySnapshotField.Pn);

        Assert.Equal(1, result.StockOutNotifiesUpdated);
        Assert.Equal(1, result.PackingItemsUpdated);
        Assert.Equal(1, result.PackingItemExtendsUpdated);
        Assert.Equal(1, result.ReceivablesUpdated);
        Assert.Equal("NEW-PN", (await notifyRepo.GetByIdAsync("n-1"))!.MaterialCode);
        Assert.Equal("OLD-BRAND", (await notifyRepo.GetByIdAsync("n-1"))!.MaterialName);
        Assert.Equal("NEW-PN", (await packingItemRepo.GetByIdAsync("pi-1"))!.Pn);
        Assert.Equal("NEW-CPN", (await packingExtendRepo.GetByIdAsync("pie-1"))!.CustomerPn);
        Assert.Equal("NEW-PN", (await receivableRepo.GetByIdAsync("ar-1"))!.PN);
    }

    [Fact]
    public async Task ApplyAsync_Brand_UpdatesMaterialNameAndSkipsUnchanged()
    {
        const string lineId = "line-1";
        var notifyRepo = new MemoryRepository<StockOutRequest>();
        await notifyRepo.AddAsync(new StockOutRequest
        {
            Id = "n-1",
            RequestCode = "STOR001",
            SalesOrderItemId = lineId,
            MaterialCode = "PN",
            MaterialName = "OLD"
        });

        var service = new SalesOrderIdentityDownstreamSyncService(
            notifyRepo,
            new MemoryRepository<PackingItem>(),
            new MemoryRepository<PackingItemExtend>(),
            new MemoryRepository<FinanceReceivable>(),
            NullLogger<SalesOrderIdentityDownstreamSyncService>.Instance);

        var first = await service.ApplyAsync(
            new[] { new SellOrderItem { Id = lineId, PN = "PN", Brand = "NEW" } },
            SalesOrderIdentitySnapshotField.Brand);
        var second = await service.ApplyAsync(
            new[] { new SellOrderItem { Id = lineId, PN = "PN", Brand = "NEW" } },
            SalesOrderIdentitySnapshotField.Brand);

        Assert.Equal(1, first.StockOutNotifiesUpdated);
        Assert.Equal(0, second.StockOutNotifiesUpdated);
        Assert.Equal("NEW", (await notifyRepo.GetByIdAsync("n-1"))!.MaterialName);
        Assert.Equal("PN", (await notifyRepo.GetByIdAsync("n-1"))!.MaterialCode);
    }
}
