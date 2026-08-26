namespace CRM.Core.Utilities;

/// <summary>指定采购（assign_method=4）是否对创建/改方式开放。</summary>
public static class RfqDesignatedPurchaserRules
{
    public const string NotEnabledMessage = "未开启指定采购";

    public static void EnsureEnabled(bool allow)
    {
        if (!allow)
            throw new ArgumentException(NotEnabledMessage);
    }
}
