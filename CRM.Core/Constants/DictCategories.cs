namespace CRM.Core.Constants
{
    /// <summary>
    /// 数据字典 Category 常量（与 sys_dict_item.Category 一致）
    /// </summary>
    public static class DictCategories
    {
        public const string VendorIndustry = "VendorIndustry";
        public const string VendorLevel = "VendorLevel";
        public const string VendorIdentity = "VendorIdentity";
        public const string VendorPaymentMethod = "VendorPaymentMethod";

        public const string CustomerType = "CustomerType";
        public const string CustomerLevel = "CustomerLevel";
        public const string CustomerIndustry = "CustomerIndustry";
        public const string CustomerTaxRate = "CustomerTaxRate";
        public const string CustomerInvoiceType = "CustomerInvoiceType";

        /// <summary>物料/需求/报价/销售订单等「生产日期」下拉（ItemCode 存库；NameZh 为展示文案）</summary>
        public const string MaterialProductionDate = "MaterialProductionDate";

        /// <summary>到货通知等「来货方式」：送货、自提、快递、物流</summary>
        public const string LogisticsArrivalMethod = "LogisticsArrivalMethod";

        /// <summary>到货通知等「快递方式」：UPS、FEDEX、DHL、顺丰、跨越</summary>
        public const string LogisticsExpressMethod = "LogisticsExpressMethod";

        /// <summary>字典管理筛选：客户相关 Category（顺序用于前端下拉）</summary>
        public static readonly string[] CustomerDictionaryCategories =
        {
            CustomerType, CustomerLevel, CustomerIndustry, CustomerTaxRate, CustomerInvoiceType
        };

        /// <summary>字典管理筛选：供应商相关 Category</summary>
        public static readonly string[] VendorDictionaryCategories =
        {
            VendorIndustry, VendorLevel, VendorIdentity, VendorPaymentMethod
        };

        /// <summary>字典管理筛选：物料相关 Category</summary>
        public static readonly string[] MaterialDictionaryCategories =
        {
            MaterialProductionDate
        };

        /// <summary>字典管理筛选：物流相关 Category</summary>
        public static readonly string[] LogisticsDictionaryCategories =
        {
            LogisticsArrivalMethod,
            LogisticsExpressMethod
        };
    }

    /// <summary>字典管理 API：业务类型分段（query: bizSegment）</summary>
    public static class DictBizSegment
    {
        public const string Customer = "customer";
        public const string Vendor = "vendor";
        public const string Material = "material";
        public const string Logistics = "logistics";
    }
}
