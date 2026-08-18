using CRM.Core.Models.Inventory;

namespace CRM.Core.Utilities;

/// <summary>
/// 详情「作废应收」仅清理出库已删除（或记录缺失）后留下的未核销应收。
/// 出库单仍有效时禁止作废，避免完成出库丢失应收。
/// </summary>
public static class FinanceReceivableVoidRules
{
    public const string StockOutStillValidMessage = "出库单仍有效，不能作废应收";

    /// <summary>
    /// 仓储按未删除过滤时，查不到出库单即视为已删或缺失。
    /// </summary>
    public static bool IsOrphanStockOut(StockOut? liveStockOut) =>
        liveStockOut == null || liveStockOut.IsDeleted;

    public static void AssertOrphanStockOutForDetailVoid(StockOut? liveStockOut)
    {
        if (!IsOrphanStockOut(liveStockOut))
            throw new InvalidOperationException(StockOutStillValidMessage);
    }
}
