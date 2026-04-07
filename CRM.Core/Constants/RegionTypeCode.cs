namespace CRM.Core.Constants;

/// <summary>
/// 地域类型 RegionType（境内/境外），与数据库存储一致：10=境内、20=境外。
/// 仓库（warehouseinfo）、到货通知（stockinnotify）、入库单（stockin）、库存（stock）、出库单（stockout）等共用。
/// </summary>
public static class RegionTypeCode
{
    public const short Domestic = 10;
    public const short Overseas = 20;

    public static bool IsDefined(short v) => v is Domestic or Overseas;

    public static short Normalize(short v) => IsDefined(v) ? v : Domestic;
}
