using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Utilities;

/// <summary>
/// 库存明细与出库单的下游引用校验：移库虚拟出库为内核自动生成，不视为需人工清理的业务节点。
/// </summary>
public static class StockOutDownstreamGuard
{
    public static async Task<List<StockOutItem>> FilterBlockingItemsAsync(
        IReadOnlyList<StockOutItem> linkedItems,
        IRepository<StockOut> stockOutRepository)
    {
        if (linkedItems.Count == 0)
            return new List<StockOutItem>();

        var stockOutIds = linkedItems
            .Select(x => x.StockOutId?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (stockOutIds.Count == 0)
            return linkedItems.ToList();

        var transferStockOutIds = (await stockOutRepository.FindAsync(x => stockOutIds.Contains(x.Id)))
            .Where(x => !x.IsDeleted && x.StockOutType == StockOutTypeCode.Transfer)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return linkedItems
            .Where(x =>
            {
                var sid = x.StockOutId?.Trim();
                return string.IsNullOrWhiteSpace(sid) || !transferStockOutIds.Contains(sid);
            })
            .ToList();
    }
}
