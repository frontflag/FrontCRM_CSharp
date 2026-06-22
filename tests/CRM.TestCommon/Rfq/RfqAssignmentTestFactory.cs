using CRM.Core.Interfaces;
using CRM.Core.Interfaces.RfqAssignment;
using CRM.Core.Models.System;
using CRM.Core.Services.RfqAssignment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.TestCommon.Rfq;

public static class RfqAssignmentTestFactory
{
    public static IRfqPurchaserAssignmentOrchestrator CreateItemRoundRobinOrchestrator(
        IPurchaseQuoterPoolService pool,
        IRepository<SysParam> sysParamRepo,
        ILogger<RfqPurchaserAssignmentOrchestrator>? orchestratorLogger = null) =>
        CreateDefaultOrchestrator(pool, sysParamRepo, orchestratorLogger);

    public static IRfqPurchaserAssignmentOrchestrator CreateDefaultOrchestrator(
        IPurchaseQuoterPoolService pool,
        IRepository<SysParam> sysParamRepo,
        ILogger<RfqPurchaserAssignmentOrchestrator>? orchestratorLogger = null)
    {
        var cursorStore = new RfqPurchaserRoundRobinCursorStore(
            sysParamRepo,
            NullLogger<RfqPurchaserRoundRobinCursorStore>.Instance);
        IRfqPurchaserAssignStrategy[] strategies =
        [
            new ItemRoundRobinPurchaserAssignStrategy(
                pool,
                cursorStore,
                NullLogger<ItemRoundRobinPurchaserAssignStrategy>.Instance),
            new SameBrandPurchaserAssignStrategy(
                pool,
                cursorStore,
                NullLogger<SameBrandPurchaserAssignStrategy>.Instance)
        ];
        return new RfqPurchaserAssignmentOrchestrator(
            strategies,
            orchestratorLogger ?? NullLogger<RfqPurchaserAssignmentOrchestrator>.Instance);
    }

    public static IRfqPurchaserAssignmentOrchestrator CreateEmptyItemRoundRobinOrchestrator(
        IRepository<SysParam> sysParamRepo) =>
        CreateDefaultOrchestrator(CreateEmptyPoolService(), sysParamRepo);

    public static IPurchaseQuoterPoolService CreateEmptyPoolService()
    {
        var svc = Substitute.For<IPurchaseQuoterPoolService>();
        svc.GetOrderedActivePoolUserIdsAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<string>());
        svc.GetAssigneeCountAsync(Arg.Any<CancellationToken>()).Returns(2);
        return svc;
    }
}
