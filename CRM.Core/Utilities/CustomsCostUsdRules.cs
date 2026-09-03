namespace CRM.Core.Utilities;

/// <summary>报关采购美金价（cost_usd）校验。</summary>
public static class CustomsCostUsdRules
{
    public static void EnsureValid(decimal costUsd)
    {
        if (costUsd <= 0m)
            throw new ArgumentException("采购美金价须大于 0。", nameof(costUsd));
    }
}
