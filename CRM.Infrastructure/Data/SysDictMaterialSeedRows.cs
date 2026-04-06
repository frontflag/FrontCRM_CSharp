namespace CRM.Infrastructure.Data
{
    /// <summary>
    /// 物料「生产日期」字典种子（与 RFQ/报价/销售订单 dateCode、productionDate 存 ItemCode 一致）
    /// </summary>
    internal static class SysDictMaterialSeedRows
    {
        internal readonly record struct Row(string Category, string ItemCode, string NameZh, string NameEn, int SortOrder);

        internal static readonly Row[] All =
        {
            new Row("MaterialProductionDate", "1", "2年内", "Within 2 years", 1),
            new Row("MaterialProductionDate", "2", "1年内", "Within 1 year", 2),
            new Row("MaterialProductionDate", "3", "无要求", "No requirement", 3),
            new Row("MaterialProductionDate", "4", "25+", "25+", 4)
        };
    }
}
