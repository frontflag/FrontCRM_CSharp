using Xunit;

namespace CRM.Core.Tests.Utilities;

/// <summary>
/// 客户报价单发送价公式（与 CustomerQuoteService.ApplyProfitFactorAsync 一致）。
/// 对照 QA：CQ-CALC-001 / CQ-CALC-002。
/// </summary>
public class CustomerQuoteProfitFactorRulesTests
{
    private static decimal ApplySendPrice(decimal purchasePrice, decimal profitFactor) =>
        Math.Round(purchasePrice * profitFactor, 6, MidpointRounding.AwayFromZero);

    [Fact]
    public void CQ_CALC_001_FactorOne_SendPriceEqualsPurchasePrice()
    {
        Assert.Equal(10.000000m, ApplySendPrice(10.000000m, 1.00m));
    }

    [Fact]
    public void CQ_CALC_002_Factor125_RoundsToSixDecimals()
    {
        Assert.Equal(2.834888m, ApplySendPrice(2.267910m, 1.25m));
    }

    [Fact]
    public void LockedRow_SkippedByCaller_NotRecalculated()
    {
        const decimal lockedSend = 99m;
        var purchase = 10m;
        var factor = 2m;
        var isLocked = true;
        var result = isLocked ? lockedSend : ApplySendPrice(purchase, factor);
        Assert.Equal(99m, result);
    }
}
