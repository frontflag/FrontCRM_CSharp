using CRM.Core.Constants;
using CRM.Core.Models.Inventory;
using CRM.Core.Tests.Fakes;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class StockOutDownstreamGuardTests
{
    [Fact]
    public async Task FilterBlockingItems_ShouldIgnoreTransferVirtualStockOut()
    {
        var stockOutRepo = new MemoryRepository<StockOut>();
        await stockOutRepo.AddAsync(new StockOut
        {
            Id = "sto-transfer-1",
            StockOutCode = "STO00054",
            StockOutType = StockOutTypeCode.Transfer,
            IsDeleted = false
        });
        await stockOutRepo.AddAsync(new StockOut
        {
            Id = "sto-sales-1",
            StockOutCode = "STO00100",
            StockOutType = StockOutTypeCode.Sales,
            IsDeleted = false
        });

        var linked = new List<StockOutItem>
        {
            new() { Id = "line-transfer", StockOutId = "sto-transfer-1", StockItemId = "si-1" },
            new() { Id = "line-sales", StockOutId = "sto-sales-1", StockItemId = "si-1" }
        };

        var blocking = await StockOutDownstreamGuard.FilterBlockingItemsAsync(linked, stockOutRepo);

        Assert.Single(blocking);
        Assert.Equal("line-sales", blocking[0].Id);
    }
}
