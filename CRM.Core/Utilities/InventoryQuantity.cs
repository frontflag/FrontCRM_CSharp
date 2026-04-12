namespace CRM.Core.Utilities;

/// <summary>订单/接口层 decimal 数量与库存 int 列对齐时的取整（与 PostgreSQL <c>round(numeric)</c> 迁移一致：远离零的一半）。</summary>
public static class InventoryQuantity
{
    public static int RoundFromDecimal(decimal value) =>
        (int)Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
