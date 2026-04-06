namespace CRM.Infrastructure.Data
{
    /// <summary>
    /// 供应商相关字典种子（与前端原 vendorIndustry / vendorEnums 一致）
    /// </summary>
    internal static class SysDictVendorSeedRows
    {
        internal readonly record struct Row(string Category, string ItemCode, string NameZh, string NameEn, int SortOrder);

        internal static readonly Row[] All =
        {
            // VendorIndustry（与前端 VENDOR_INDUSTRY_VALUES 一致）
            new("VendorIndustry", "Semiconductors", "半导体", "Semiconductors", 1),
            new("VendorIndustry", "TestMeasurement", "测试和测量", "Test & measurement", 2),
            new("VendorIndustry", "CircuitProtection", "电路保护", "Circuit protection", 3),
            new("VendorIndustry", "WiresCables", "电线及电缆", "Wires & cables", 4),
            new("VendorIndustry", "CathodePower", "负极、电源", "Cathode / power", 5),
            new("VendorIndustry", "ToolsEquipment", "工具及设备", "Tools & equipment", 6),
            new("VendorIndustry", "IndustrialControl", "工控", "Industrial control", 7),
            new("VendorIndustry", "MechatronicEncoders", "机电编码器", "Electromechanical encoders", 8),
            new("VendorIndustry", "ComputerPeripheralsMech", "计算机外设、机电", "Computer peripherals & electromechanics", 9),
            new("VendorIndustry", "StructuralParts", "结构件", "Structural parts", 10),
            new("VendorIndustry", "DevKitsTools", "开发套件和工具", "Development kits & tools", 11),
            new("VendorIndustry", "ThermalManagement", "热管理", "Thermal management", 12),
            new("VendorIndustry", "NetworkCommDevices", "网络通讯器件", "Network communication devices", 13),
            new("VendorIndustry", "DisplayMarket", "显示市场", "Display market", 14),
            new("VendorIndustry", "IGUS", "IGUS", "IGUS", 15),
            new("VendorIndustry", "LedLightingOptoDisplay", "LED照明、光电设备及显示器", "LED lighting, optoelectronics & displays", 16),
            // VendorLevel
            new("VendorLevel", "1", "1-", "1-", 1),
            new("VendorLevel", "2", "1", "1", 2),
            new("VendorLevel", "3", "1+", "1+", 3),
            new("VendorLevel", "4", "2-", "2-", 4),
            new("VendorLevel", "5", "2", "2", 5),
            new("VendorLevel", "6", "2+", "2+", 6),
            new("VendorLevel", "7", "3-", "3-", 7),
            new("VendorLevel", "8", "3", "3", 8),
            new("VendorLevel", "9", "3+", "3+", 9),
            new("VendorLevel", "10", "A", "A", 10),
            new("VendorLevel", "11", "B", "B", 11),
            new("VendorLevel", "12", "C", "C", 12),
            new("VendorLevel", "13", "D", "D", 13),
            // VendorIdentity
            new("VendorIdentity", "1", "目录商", "Catalog vendor", 1),
            new("VendorIdentity", "2", "货代", "Freight forwarder", 2),
            new("VendorIdentity", "3", "原厂", "Original manufacturer", 3),
            new("VendorIdentity", "4", "EMS工厂", "EMS factory", 4),
            new("VendorIdentity", "5", "代理", "Agent", 5),
            new("VendorIdentity", "6", "IDH", "IDH", 6),
            new("VendorIdentity", "7", "渠道商", "Channel partner", 7),
            new("VendorIdentity", "8", "现货贸易商", "Spot trader", 8),
            new("VendorIdentity", "9", "电商", "E-commerce", 9),
            new("VendorIdentity", "10", "制造商", "Manufacturer", 10),
            // VendorPaymentMethod（与 vendorinfo.PaymentMethod 字符串一致）
            new("VendorPaymentMethod", "Prepaid", "预付款", "Prepaid", 1),
            new("VendorPaymentMethod", "COD", "货到付款", "COD", 2),
            new("VendorPaymentMethod", "Monthly", "月结", "Monthly", 3),
            new("VendorPaymentMethod", "Credit", "账期", "Credit", 4),
            new("VendorPaymentMethod", "TT", "电汇", "Wire T/T", 5),
            new("VendorPaymentMethod", "LC", "信用证", "L/C", 6)
        };
    }
}
