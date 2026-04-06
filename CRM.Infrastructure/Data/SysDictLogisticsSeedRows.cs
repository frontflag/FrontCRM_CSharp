namespace CRM.Infrastructure.Data
{
    /// <summary>物流类字典种子（sys_dict_item）：到货通知「来货方式」「快递方式」等。</summary>
    public static class SysDictLogisticsSeedRows
    {
        public sealed record Row(string Category, string ItemCode, string NameZh, string NameEn, int SortOrder);

        public static readonly Row[] All =
        {
            new("LogisticsArrivalMethod", "1", "送货", "Delivery", 1),
            new("LogisticsArrivalMethod", "2", "自提", "Self pickup", 2),
            new("LogisticsArrivalMethod", "3", "快递", "Express courier", 3),
            new("LogisticsArrivalMethod", "4", "物流", "Freight / logistics", 4),
            new("LogisticsExpressMethod", "1", "UPS", "UPS", 1),
            new("LogisticsExpressMethod", "2", "FEDEX", "FedEx", 2),
            new("LogisticsExpressMethod", "3", "DHL", "DHL", 3),
            new("LogisticsExpressMethod", "4", "顺丰", "SF Express", 4),
            new("LogisticsExpressMethod", "5", "跨越", "KYE Express", 5)
        };
    }
}
