namespace CRM.Core.Utilities;

/// <summary>报关代理费率：存 1+纯费率，与报关公司主数据、报关单头快照共用。</summary>
public static class CustomsAgencyRateRules
{
    public const decimal MinInclusive = 1m;

    public static void EnsureValid(decimal agencyRate)
    {
        if (agencyRate < MinInclusive)
            throw new ArgumentException("代理费率须为 1+纯费率形式，不能小于 1。", nameof(agencyRate));
    }

    /// <summary>
    /// 系统模式取报关公司主数据；手工模式用单头快照（须 ≥ 1）。
    /// </summary>
    public static decimal ResolveForCalculation(bool agencyRateManual, decimal snapshotRate, decimal brokerMasterRate)
    {
        if (agencyRateManual)
        {
            if (snapshotRate < MinInclusive)
                throw new InvalidOperationException("代理费率须为 1+纯费率形式，不能小于 1。");
            return snapshotRate;
        }

        return brokerMasterRate > 0m ? brokerMasterRate : 1m;
    }
}
