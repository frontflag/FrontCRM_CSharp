namespace CRM.Core.Constants;

/// <summary>
/// <c>sys_relation_map.type</c> 关系类型。枚举从 100 起：
/// 100 段为人员关系，200 段为业务关系；同一段内按业务递增分配新值。
/// </summary>
public static class SysRelationMapTypeCode
{
    // ----- 100：人员关系 -----

    /// <summary>销售助理 → 销售员（1 个助理负责多名销售）。</summary>
    public const short SalesAssistantToSalesperson = 100;

    /// <summary>采购助理 → 采购员（1 个助理负责多名采购）。</summary>
    public const short PurchaseAssistantToPurchaser = 101;

    // ----- 200：业务关系 -----

    /// <summary>采购员 → 销售员（该采购员对哪些销售员的需求进行报价）。</summary>
    public const short PurchaserQuotesSalespersonRfq = 200;

    /// <summary>人员关系类型下界（含）。</summary>
    public const short PersonnelRangeMin = 100;

    /// <summary>人员关系类型上界（含，199 预留本段扩展）。</summary>
    public const short PersonnelRangeMax = 199;

    /// <summary>业务关系类型下界（含）。</summary>
    public const short BusinessRangeMin = 200;

    /// <summary>业务关系类型上界（含，299 预留本段扩展）。</summary>
    public const short BusinessRangeMax = 299;

    public static bool IsPersonnelRelation(short type) =>
        type >= PersonnelRangeMin && type <= PersonnelRangeMax;

    public static bool IsBusinessRelation(short type) =>
        type >= BusinessRangeMin && type <= BusinessRangeMax;
}
