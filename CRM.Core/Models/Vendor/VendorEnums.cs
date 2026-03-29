namespace CRM.Core.Models.Vendor
{
    /// <summary>
    /// 供应商等级（vendorinfo.Level，存 short 枚举值）
    /// </summary>
    public enum VendorLevelCode : short
    {
        OneMinus = 1,
        One = 2,
        OnePlus = 3,
        TwoMinus = 4,
        Two = 5,
        TwoPlus = 6,
        ThreeMinus = 7,
        Three = 8,
        ThreePlus = 9,
        A = 10,
        B = 11,
        C = 12,
        D = 13
    }

    /// <summary>
    /// 供应商身份（vendorinfo.Credit 字段存此枚举值；与统一社会信用代码 CreditCode 无关）
    /// </summary>
    public enum VendorIdentityCode : short
    {
        /// <summary>目录商</summary>
        CatalogVendor = 1,
        /// <summary>货代</summary>
        FreightForwarder = 2,
        /// <summary>原厂</summary>
        OriginalFactory = 3,
        /// <summary>EMS工厂</summary>
        EmsFactory = 4,
        /// <summary>代理</summary>
        Agent = 5,
        /// <summary>IDH</summary>
        Idh = 6,
        /// <summary>渠道商</summary>
        ChannelPartner = 7,
        /// <summary>现货贸易商</summary>
        SpotTrader = 8,
        /// <summary>电商</summary>
        Ecommerce = 9,
        /// <summary>制造商</summary>
        Manufacturer = 10
    }
}
