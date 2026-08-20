namespace CRM.Core.Models.Vendor
{
    /// <summary>
    /// 供应商等级（vendorinfo.Level，存 short；界面文案 S/A/B/C）
    /// </summary>
    public enum VendorLevelCode : short
    {
        S = 1,
        A = 2,
        B = 3,
        C = 4
    }

    public static class VendorLevelCodes
    {
        public const short Min = (short)VendorLevelCode.S;
        public const short Max = (short)VendorLevelCode.C;
        public const short Default = (short)VendorLevelCode.C;

        public static bool IsDefined(short? level) =>
            level is >= Min and <= Max;

        public static short NormalizeOrDefault(short? level) =>
            IsDefined(level) ? level!.Value : Default;

        /// <summary>看板/回退文案；有字典时优先用字典名称。</summary>
        public static string DisplayLabel(short? level) => level switch
        {
            (short)VendorLevelCode.S => "S",
            (short)VendorLevelCode.A => "A",
            (short)VendorLevelCode.B => "B",
            (short)VendorLevelCode.C => "C",
            _ => "未设置"
        };
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
