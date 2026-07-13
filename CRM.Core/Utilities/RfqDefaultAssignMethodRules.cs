using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>新建需求时可选的分配方式默认值（仅 2/3/5）。</summary>
public static class RfqDefaultAssignMethodRules
{
    public static readonly short[] AllowedAssignMethods =
    [
        RfqAssignMethodCodes.ItemRoundRobin,
        RfqAssignMethodCodes.SameBrandSamePurchaser,
        RfqAssignMethodCodes.PurchaseQuotePriority
    ];

    public const short DefaultAssignMethod = RfqAssignMethodCodes.PurchaseQuotePriority;

    public static bool IsAllowed(short assignMethod) =>
        AllowedAssignMethods.Contains(assignMethod);

    public static short Normalize(short assignMethod) =>
        IsAllowed(assignMethod) ? assignMethod : DefaultAssignMethod;
}
