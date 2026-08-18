using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Utilities;

/// <summary>出库强制删除确认弹窗用的只读摘要。</summary>
public static class StockOutForceDeletePreviewFactory
{
    public static bool WillRollbackInventory(short status) => status is 2 or 4;

    public static StockOutForceDeletePreviewDto Create(
        StockOut stockOut,
        ForceDeleteGuardResult guard,
        IReadOnlyList<StockOutForceDeleteReceivableRow> receivables)
    {
        var can = guard.CanDelete;
        var rows = receivables ?? Array.Empty<StockOutForceDeleteReceivableRow>();
        return new StockOutForceDeletePreviewDto
        {
            StockOutId = stockOut.Id,
            StockOutCode = stockOut.StockOutCode,
            Status = stockOut.Status,
            CanForceDelete = can,
            BlockReason = can ? null : guard.Message,
            WillRollbackInventory = WillRollbackInventory(stockOut.Status),
            WillVoidReceivables = can && rows.Count > 0,
            Receivables = rows
        };
    }
}
