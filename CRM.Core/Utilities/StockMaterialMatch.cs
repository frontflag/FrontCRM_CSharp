using CRM.Core.Models.Inventory;

namespace CRM.Core.Utilities
{
    /// <summary>
    /// 出库/拣货侧传的物料键多为 PN；库存表 <see cref="StockInfo.MaterialId"/> 常与物料主键 ProductId（GUID）一致。
    /// 两者任一命中即视为同一物料行。
    /// </summary>
    public static class StockMaterialMatch
    {
        public static bool Matches(StockInfo stock, string? outboundPnOrCode, string? productIdFromOrderLine)
        {
            var sid = stock.MaterialId?.Trim() ?? "";
            if (string.IsNullOrEmpty(sid)) return false;
            var code = outboundPnOrCode?.Trim() ?? "";
            if (!string.IsNullOrEmpty(code) && string.Equals(sid, code, StringComparison.OrdinalIgnoreCase))
                return true;
            var pid = productIdFromOrderLine?.Trim();
            if (!string.IsNullOrEmpty(pid) && string.Equals(sid, pid, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
    }
}
