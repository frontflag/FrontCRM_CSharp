using CRM.Core.Constants;

namespace CRM.Core.Utilities;

/// <summary>
/// 报关单界面「申报日期」：报关装箱对应报关出库单（类型 20）状态变为出库完成(4)时的实际出库日期。
/// </summary>
public static class CustomsDeclarationDeclareDate
{
    /// <summary>
    /// 取已出库完成且有实际出库日期的记录中最晚的 <c>StockOutDate</c>；否则空。
    /// </summary>
    public static DateTime? Resolve(IEnumerable<(short Status, DateTime? StockOutDate)> customsStockOuts)
    {
        DateTime? latest = null;
        foreach (var row in customsStockOuts)
        {
            if (row.Status != StockOutStatusCode.Completed)
                continue;
            if (row.StockOutDate is not { } d || d == default)
                continue;
            if (latest == null || d > latest.Value)
                latest = d;
        }

        return latest;
    }
}
