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

public sealed class SameBrandPurchaserAssignStrategyTests
{
    private static IPurchaseQuoterPoolService CreatePool(params string[] userIds)
    {
        var svc = Substitute.For<IPurchaseQuoterPoolService>();
        svc.GetOrderedActivePoolUserIdsAsync(Arg.Any<CancellationToken>())
            .Returns(userIds.ToList());
        svc.GetAssigneeCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        return svc;
    }

    private static SameBrandPurchaserAssignStrategy CreateStrategy(
        IPurchaseQuoterPoolService pool,
        MemoryRepository<SysParam> sysParamRepo)
    {
        var cursorStore = new RfqPurchaserRoundRobinCursorStore(
            sysParamRepo,
            NullLogger<RfqPurchaserRoundRobinCursorStore>.Instance);
        return new SameBrandPurchaserAssignStrategy(
            pool,
            cursorStore,
            NullLogger<SameBrandPurchaserAssignStrategy>.Instance);
    }

    [Fact]
    public async Task AssignAsync_SameBrand_ReusesAssignees_AndAdvancesCursorPerBrand()
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

        var strategy = CreateStrategy(CreatePool("U-A", "U-M", "U-Z"), sysParamRepo);
        var outcome = await strategy.AssignAsync(new RfqAssignmentContext
        {
            RfqId = "rfq-1",
            RfqCode = "RF001",
            Trigger = RfqAssignmentTrigger.Create,
            Items =
            [
                new RfqItemAssignmentInput { ItemKey = "1", LineNo = 1, BrandId = 1, Brand = "ST" },
                new RfqItemAssignmentInput { ItemKey = "2", LineNo = 2, BrandId = 1, Brand = "ST" },
                new RfqItemAssignmentInput { ItemKey = "3", LineNo = 3, BrandId = 2, Brand = "TI" }
            ]
        });

        Assert.Equal(RfqAssignMethodCodes.SameBrandSamePurchaser, outcome.AssignMethodCode);
        Assert.Equal(("U-A", "U-M"), (outcome.Assignments[0].PurchaserUserId1, outcome.Assignments[0].PurchaserUserId2));
        Assert.Equal(("U-A", "U-M"), (outcome.Assignments[1].PurchaserUserId1, outcome.Assignments[1].PurchaserUserId2));
        Assert.Equal(("U-Z", "U-A"), (outcome.Assignments[2].PurchaserUserId1, outcome.Assignments[2].PurchaserUserId2));

        var cursorRow = (await sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor)).First();
        Assert.Equal("4", cursorRow.ValueString);
    }

    [Fact]
    public async Task AssignAsync_AddItems_ReusesExistingBrandAssignees()
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

        var strategy = CreateStrategy(CreatePool("U-A", "U-M", "U-Z"), sysParamRepo);
        var outcome = await strategy.AssignAsync(new RfqAssignmentContext
        {
            RfqId = "rfq-1",
            Trigger = RfqAssignmentTrigger.AddItems,
            ExistingBrandAssignees = new Dictionary<string, (string?, string?)>
            {
                ["id:1"] = ("U-A", "U-M")
            },
            Items =
            [
                new RfqItemAssignmentInput { ItemKey = "2", LineNo = 2, BrandId = 1, Brand = "ST" },
                new RfqItemAssignmentInput { ItemKey = "3", LineNo = 3, BrandId = 3, Brand = "NX" }
            ]
        });

        Assert.Equal(("U-A", "U-M"), (outcome.Assignments[0].PurchaserUserId1, outcome.Assignments[0].PurchaserUserId2));
        Assert.Equal(("U-Z", "U-A"), (outcome.Assignments[1].PurchaserUserId1, outcome.Assignments[1].PurchaserUserId2));

        var cursorRow = (await sysParamRepo.FindAsync(p => p.ParamCode == SysParamCodes.RfqPurchaserRoundRobinCursor)).First();
        Assert.Equal("2", cursorRow.ValueString);
    }
}
