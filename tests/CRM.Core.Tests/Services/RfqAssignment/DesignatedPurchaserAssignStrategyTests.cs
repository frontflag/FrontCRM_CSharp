using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Services.RfqAssignment;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services.RfqAssignment;

public sealed class DesignatedPurchaserAssignStrategyTests
{
    private static DesignatedPurchaserAssignStrategy CreateStrategy(params string[] poolUserIds)
    {
        var pool = Substitute.For<IPurchaseQuoterPoolService>();
        pool.GetOrderedActivePoolUserIdsAsync(Arg.Any<CancellationToken>()).Returns(poolUserIds.ToList());
        pool.GetAssigneeCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        return new DesignatedPurchaserAssignStrategy(pool, NullLogger<DesignatedPurchaserAssignStrategy>.Instance);
    }

    private static RfqAssignmentContext TwoLines(string? userId, bool allowOutside = false) =>
        new()
        {
            RfqId = "rfq-1",
            DesignatedPurchaserUserId = userId,
            AllowDesignatedPurchaserOutsidePool = allowOutside,
            Items =
            [
                new RfqItemAssignmentInput { ItemKey = "1", LineNo = 1, Mpn = "A" },
                new RfqItemAssignmentInput { ItemKey = "2", LineNo = 2, Mpn = "B" }
            ]
        };

    [Fact]
    public async Task AssignAsync_WritesSamePersonToSlot1_IgnoresAssigneeCount()
    {
        var strategy = CreateStrategy("U-1", "U-2");
        var outcome = await strategy.AssignAsync(TwoLines("U-2"));

        Assert.Equal(RfqAssignMethodCodes.DesignatedPurchaser, outcome.AssignMethodCode);
        Assert.Equal(2, outcome.Assignments.Count);
        Assert.All(outcome.Assignments, a =>
        {
            Assert.Equal("U-2", a.PurchaserUserId1);
            Assert.Null(a.PurchaserUserId2);
        });
    }

    [Fact]
    public async Task AssignAsync_MissingPerson_Throws()
    {
        var strategy = CreateStrategy("U-1");
        await Assert.ThrowsAsync<ArgumentException>(() => strategy.AssignAsync(TwoLines("  ")));
    }

    [Fact]
    public async Task AssignAsync_PersonNotInPool_Throws()
    {
        var strategy = CreateStrategy("U-1");
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => strategy.AssignAsync(TwoLines("U-OUT")));
        Assert.Contains("报价员池", ex.Message);
    }

    [Fact]
    public async Task AssignAsync_AllowOutsidePool_AcceptsFormerMember()
    {
        var strategy = CreateStrategy("U-1");
        var outcome = await strategy.AssignAsync(TwoLines("U-OLD", allowOutside: true));
        Assert.Equal("U-OLD", outcome.Assignments[0].PurchaserUserId1);
        Assert.Null(outcome.Assignments[0].PurchaserUserId2);
    }
}
