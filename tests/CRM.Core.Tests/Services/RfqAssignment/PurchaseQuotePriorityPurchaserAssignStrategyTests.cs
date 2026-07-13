using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Models.System;
using CRM.Core.Services.RfqAssignment;
using CRM.Core.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Services.RfqAssignment;

public sealed class PurchaseQuotePriorityPurchaserAssignStrategyTests
{
    private static PurchaseQuotePriorityPurchaserAssignStrategy CreateStrategy(
        IPurchaseQuoterPoolService pool,
        IRfqMpnPurchaserAffinityLookup lookup,
        MemoryRepository<SysParam> sysParamRepo)
    {
        var picker = new RfqPurchaserRoundRobinPicker(
            pool,
            new RfqPurchaserRoundRobinCursorStore(sysParamRepo, NullLogger<RfqPurchaserRoundRobinCursorStore>.Instance),
            NullLogger<RfqPurchaserRoundRobinPicker>.Instance);
        return new PurchaseQuotePriorityPurchaserAssignStrategy(
            pool,
            lookup,
            picker,
            NullLogger<PurchaseQuotePriorityPurchaserAssignStrategy>.Instance);
    }

    private static IPurchaseQuoterPoolService CreatePool(params string[] userIds)
    {
        var svc = Substitute.For<IPurchaseQuoterPoolService>();
        svc.GetOrderedActivePoolUserIdsAsync(Arg.Any<CancellationToken>())
            .Returns(userIds.ToList());
        svc.GetAssigneeCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        return svc;
    }

    [Fact]
    public async Task AssignAsync_UsesPurchaseHistory_ForBothSlots()
    {
        var lookup = Substitute.For<IRfqMpnPurchaserAffinityLookup>();
        lookup.GetPurchasersFromPurchaseHistoryAsync("MPN-1", Arg.Any<IReadOnlySet<string>>(), 2, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "U-BUY-1", "U-BUY-2" });
        lookup.GetPurchasersFromQuoteHistoryAsync(Arg.Any<string>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var strategy = CreateStrategy(CreatePool("U-BUY-1", "U-BUY-2", "U-POOL"), lookup, new MemoryRepository<SysParam>());
        var outcome = await strategy.AssignAsync(new RfqAssignmentContext
        {
            RfqId = "rfq-1",
            Items = [new RfqItemAssignmentInput { ItemKey = "1", LineNo = 1, Mpn = "MPN-1" }]
        });

        Assert.Equal(RfqAssignMethodCodes.PurchaseQuotePriority, outcome.AssignMethodCode);
        Assert.Equal(("U-BUY-1", "U-BUY-2"), (outcome.Assignments[0].PurchaserUserId1, outcome.Assignments[0].PurchaserUserId2));
    }

    [Fact]
    public async Task AssignAsync_SinglePurchaseHistory_SecondSlotFromRoundRobin()
    {
        var sysParamRepo = new MemoryRepository<SysParam>();
        await sysParamRepo.AddAsync(new SysParam
        {
            Id = "sp-cursor",
            ParamCode = SysParamCodes.RfqPurchaserRoundRobinCursor,
            ParamName = "cursor",
            ValueString = "0",
            Status = 1
        });

        var lookup = Substitute.For<IRfqMpnPurchaserAffinityLookup>();
        lookup.GetPurchasersFromPurchaseHistoryAsync("MPN-1", Arg.Any<IReadOnlySet<string>>(), 2, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "U-BUY-1" });
        lookup.GetPurchasersFromQuoteHistoryAsync(Arg.Any<string>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var strategy = CreateStrategy(CreatePool("U-BUY-1", "U-POOL-A", "U-POOL-B"), lookup, sysParamRepo);
        var outcome = await strategy.AssignAsync(new RfqAssignmentContext
        {
            RfqId = "rfq-1",
            Items = [new RfqItemAssignmentInput { ItemKey = "1", LineNo = 1, Mpn = "MPN-1" }]
        });

        Assert.Equal("U-BUY-1", outcome.Assignments[0].PurchaserUserId1);
        Assert.Equal("U-POOL-A", outcome.Assignments[0].PurchaserUserId2);
    }

    [Fact]
    public async Task AssignAsync_NoHistory_FallsBackToRoundRobin()
    {
        var sysParamRepo = new MemoryRepository<SysParam>();
        await sysParamRepo.AddAsync(new SysParam
        {
            Id = "sp-cursor",
            ParamCode = SysParamCodes.RfqPurchaserRoundRobinCursor,
            ParamName = "cursor",
            ValueString = "0",
            Status = 1
        });

        var lookup = Substitute.For<IRfqMpnPurchaserAffinityLookup>();
        lookup.GetPurchasersFromPurchaseHistoryAsync(Arg.Any<string>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());
        lookup.GetPurchasersFromQuoteHistoryAsync(Arg.Any<string>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());

        var strategy = CreateStrategy(CreatePool("U-A", "U-M"), lookup, sysParamRepo);
        var outcome = await strategy.AssignAsync(new RfqAssignmentContext
        {
            RfqId = "rfq-1",
            Items = [new RfqItemAssignmentInput { ItemKey = "1", LineNo = 1, Mpn = "NEW-MPN" }]
        });

        Assert.Equal(("U-A", "U-M"), (outcome.Assignments[0].PurchaserUserId1, outcome.Assignments[0].PurchaserUserId2));
    }

    [Fact]
    public async Task AssignAsync_EmptyMpn_UsesRoundRobinWithoutLookup()
    {
        var sysParamRepo = new MemoryRepository<SysParam>();
        await sysParamRepo.AddAsync(new SysParam
        {
            Id = "sp-cursor",
            ParamCode = SysParamCodes.RfqPurchaserRoundRobinCursor,
            ParamName = "cursor",
            ValueString = "0",
            Status = 1
        });

        var lookup = Substitute.For<IRfqMpnPurchaserAffinityLookup>();
        var strategy = CreateStrategy(CreatePool("U-A", "U-M"), lookup, sysParamRepo);
        var outcome = await strategy.AssignAsync(new RfqAssignmentContext
        {
            RfqId = "rfq-1",
            Items = [new RfqItemAssignmentInput { ItemKey = "1", LineNo = 1, Mpn = "   " }]
        });

        Assert.Equal(("U-A", "U-M"), (outcome.Assignments[0].PurchaserUserId1, outcome.Assignments[0].PurchaserUserId2));
        await lookup.DidNotReceive().GetPurchasersFromPurchaseHistoryAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlySet<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
