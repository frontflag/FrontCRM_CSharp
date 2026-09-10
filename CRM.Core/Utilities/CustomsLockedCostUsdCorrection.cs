namespace CRM.Core.Utilities;

/// <summary>
/// 结关锁定或内部已完成后更正采购美金价：仅 SYS_ADMIN / SYS_MANAGER。
/// </summary>
public static class CustomsLockedCostUsdCorrection
{
    public const string LockedForbiddenMessage = "报关费用已锁定，仅系统管理员或平台管理员可修改采购美金价。";
    public const string CompletedForbiddenMessage = "已完成报关单仅系统管理员或平台管理员可更正采购美金价。";
    public const string LockedRecalcForbiddenMessage = "报关费用已锁定，不能试算。";

    public static void EnsureCanChangeCostUsd(
        bool feesLocked,
        bool canCorrectLockedCostUsd,
        bool completed = false)
    {
        if (!feesLocked && !completed)
            return;
        if (canCorrectLockedCostUsd)
            return;
        throw new InvalidOperationException(
            completed ? CompletedForbiddenMessage : LockedForbiddenMessage);
    }

    public static void EnsureCanRecalculate(
        bool feesLocked,
        bool canCorrectLockedCostUsd,
        bool completed = false)
    {
        if (completed && !canCorrectLockedCostUsd)
            throw new InvalidOperationException(CompletedForbiddenMessage);
        if (feesLocked && !canCorrectLockedCostUsd)
            throw new InvalidOperationException(LockedRecalcForbiddenMessage);
    }
}
