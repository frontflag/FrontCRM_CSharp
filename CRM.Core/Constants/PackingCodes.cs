namespace CRM.Core.Constants;

/// <summary>装箱单主表业务状态。</summary>
public static class PackingStatusCode
{
    public const short New = 10;
    public const short Confirmed = 20;
    public const short Picked = 30;
    public const short Ready = 40;
    public const short PendingStockOut = 50;
    public const short StockOutFinished = 100;
}

/// <summary>装箱单出库类型（与 <see cref="StockOutTypeCode"/> 一致）。</summary>
public static class PackingStockOutTypeCode
{
    public const short Sales = StockOutTypeCode.Sales;
    public const short Customs = StockOutTypeCode.Customs;
    public const short Return = StockOutTypeCode.Return;
    public const short Scrap = StockOutTypeCode.Scrap;
}

/// <summary>装箱单物料类型。</summary>
public static class PackingMaterialTypeCode
{
    public const short Normal = 10;
    public const short Test = 20;
    public const short Sample = 30;
}

/// <summary>装箱单送货方式。</summary>
public static class PackingDeliveryMethodCode
{
    /// <summary>送货</summary>
    public const short Delivery = 10;

    /// <summary>自提</summary>
    public const short SelfPickup = 20;
}
