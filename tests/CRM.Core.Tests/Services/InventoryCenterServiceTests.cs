using System.Reflection;
using CRM.Core.Models.Inventory;
using CRM.Core.Services;

namespace CRM.Core.Tests.Services;

public class InventoryCenterServiceTests
{
    [Fact]
    public void CalculateNetMonthlyOutCost_ShouldUseNetRuleForStockOutAndReverse()
    {
        var monthStart = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var ledgers = new List<InventoryLedger>
        {
            new()
            {
                Id = "l1",
                BizType = "STOCK_OUT",
                Amount = -100m,
                CreateTime = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = "l2",
                BizType = "STOCK_OUT_REVERSE",
                Amount = 30m,
                CreateTime = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = "l3",
                BizType = "STOCK_IN",
                Amount = 999m,
                CreateTime = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                Id = "l4",
                BizType = "STOCK_OUT",
                Amount = -200m,
                CreateTime = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc)
            }
        };

        var method = typeof(InventoryCenterService).GetMethod(
            "CalculateNetMonthlyOutCost",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = (decimal)method!.Invoke(null, new object[] { ledgers, monthStart })!;

        Assert.Equal(70m, result);
    }
}
