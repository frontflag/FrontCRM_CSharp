namespace CRM.Infrastructure.Data
{
    /// <summary>
    /// 客户相关字典种子（与 customerinfo.Type / Level / Industry 及前端税率、发票类型一致）
    /// </summary>
    internal static class SysDictCustomerSeedRows
    {
        internal readonly record struct Row(string Category, string ItemCode, string NameZh, string NameEn, int SortOrder);

        internal static readonly Row[] All = BuildAll();

        private static Row[] BuildAll()
        {
            var list = new List<Row>();
            var so = 0;

            // CustomerType：ItemCode 与 Type 数值一致（字符串 "1".."11"）
            foreach (var (code, zh, en) in new (int Code, string Zh, string En)[]
                     {
                         (1, "OEM", "OEM"),
                         (2, "ODM", "ODM"),
                         (3, "终端", "End user"),
                         (4, "IDH", "IDH"),
                         (5, "贸易商", "Trader"),
                         (6, "代理商", "Agent"),
                         (7, "EMS", "EMS"),
                         (8, "非行业", "Non-industry"),
                         (9, "科研机构", "Research"),
                         (10, "供应链", "Supply chain"),
                         (11, "原厂", "Original factory")
                     })
            {
                so++;
                list.Add(new Row("CustomerType", code.ToString(), zh, en, so));
            }

            so = 0;
            foreach (var (code, zh, en) in new (string Code, string Zh, string En)[]
                     {
                         ("D", "D级", "Grade D"),
                         ("C", "C级", "Grade C"),
                         ("B", "B级", "Grade B"),
                         ("BPO", "BPO", "BPO"),
                         ("VIP", "VIP", "VIP"),
                         ("VPO", "VPO", "VPO")
                     })
            {
                so++;
                list.Add(new Row("CustomerLevel", code, zh, en, so));
            }

            so = 0;
            foreach (var (code, zh, en) in new (string Code, string Zh, string En)[]
                     {
                         ("FinanceEquipment", "金融设备", "Financial equipment"),
                         ("Telecom", "通讯", "Telecom"),
                         ("RailTransit", "轨道交通", "Rail transit"),
                         ("Aerospace", "航空航天", "Aerospace"),
                         ("CyberSecurity", "网络安全", "Cyber security"),
                         ("Esports", "电竞", "Esports"),
                         ("PowerSupply", "电源", "Power supply"),
                         ("ElectronicComponentsTrading", "电子元器件贸易", "EC trading"),
                         ("ElectronicComponentsManufacturing", "电子元器件制造", "EC manufacturing"),
                         ("PowerTools", "电动工具", "Power tools"),
                         ("PowerElectrical", "电力电气", "Power & electrical"),
                         ("IoT", "物联网", "IoT"),
                         ("ConsumerElectronics", "消费电子", "Consumer electronics"),
                         ("Robotics", "机器人", "Robotics"),
                         ("SmartSecurity", "智能安防", "Smart security"),
                         ("SmartCity", "智慧城市", "Smart city"),
                         ("UAV", "无人机", "UAV"),
                         ("NewEnergyVehicles", "新能源汽车", "NEV"),
                         ("NewEnergy", "新能源", "New energy"),
                         ("IndustrialControl", "工业控制", "Industrial control"),
                         ("MedicalEquipment", "医疗设备", "Medical equipment"),
                         ("DefenseMilitary", "军工", "Defense"),
                         ("TraditionalVehicles", "传统车辆", "Traditional vehicles"),
                         ("Instrumentation", "仪器仪表", "Instrumentation"),
                         ("ArtificialIntelligence", "人工智能", "AI"),
                         ("CloudComputingIDC", "云计算IDC", "Cloud / IDC"),
                         ("Manufacturing", "制造业", "Manufacturing"),
                         ("Trading", "贸易/零售", "Trading / retail"),
                         ("Technology", "科技/IT", "Technology / IT"),
                         ("Construction", "建筑/工程", "Construction"),
                         ("Healthcare", "医疗/健康", "Healthcare"),
                         ("Education", "教育", "Education"),
                         ("Finance", "金融", "Finance"),
                         ("Other", "其他", "Other")
                     })
            {
                so++;
                list.Add(new Row("CustomerIndustry", code, zh, en, so));
            }

            so = 0;
            foreach (var (code, zh, en) in new (string Code, string Zh, string En)[]
                     {
                         ("0", "0%", "0%"),
                         ("1", "1%", "1%"),
                         ("3", "3%", "3%"),
                         ("6", "6%", "6%"),
                         ("9", "9%", "9%"),
                         ("13", "13%", "13%")
                     })
            {
                so++;
                list.Add(new Row("CustomerTaxRate", code, zh, en, so));
            }

            so = 0;
            foreach (var (code, zh, en) in new (string Code, string Zh, string En)[]
                     {
                         ("0", "无需开票", "No invoice"),
                         ("1", "增值税专用发票", "VAT special invoice"),
                         ("2", "增值税普通发票", "VAT ordinary invoice"),
                         ("3", "电子发票", "E-invoice")
                     })
            {
                so++;
                list.Add(new Row("CustomerInvoiceType", code, zh, en, so));
            }

            return list.ToArray();
        }
    }
}
