namespace CRM.Core.Constants;

/// <summary>需求主表 assign_method（与前端 rfqFormEnums / 历史数据一致）。</summary>
public static class RfqAssignMethodCodes
{
    public const short SamePurchaser = 1;
    public const short ItemRoundRobin = 2;
    public const short SameBrandSamePurchaser = 3;
    public const short DesignatedPurchaser = 4;
}
