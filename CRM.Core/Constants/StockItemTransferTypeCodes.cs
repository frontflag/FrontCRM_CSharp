namespace CRM.Core.Constants;

/// <summary>
/// <c>stock_item.TransferType</c>：列表展示用标记；与数量公式无关。
/// </summary>
public static class StockItemTransferTypeCodes
{
    /// <summary>手工移库源行（整行出清后写入）；全库在库明细列表与分桶下钻列表默认排除。</summary>
    public const short ManualTransferSource = 10;
}
