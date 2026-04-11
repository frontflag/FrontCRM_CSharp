namespace CRM.Core.Constants;

/// <summary>与 <see cref="Models.Inventory.StockInfo.StockType"/> / 采购订单头 <c>Type</c> 一致：1=客单 2=备货 3=样品</summary>
public static class StockInventoryTypeCodes
{
    public const short CustomerOrder = 1;
    public const short Stocking = 2;
    public const short Sample = 3;

    public static short Normalize(short type) => type is >= 1 and <= 3 ? type : CustomerOrder;
}
