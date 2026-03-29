using CRM.Core.Models.Purchase;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Utilities
{
    /// <summary>
    /// 出库/拣货侧传的物料键多为 PN；库存 <see cref="StockInfo.MaterialId"/> 常见形态：
    /// 物料 ProductId、采购明细 Id（与质检入库 materialCode 一致）、或与 PN 相同。
    /// 上述任一与库存行 MaterialId 一致即视为命中。
    /// </summary>
    public static class StockMaterialMatch
    {
        public static bool Matches(StockInfo stock, string? outboundPnOrCode, string? productIdFromOrderLine)
            => Matches(stock, outboundPnOrCode, productIdFromOrderLine, null);

        public static bool Matches(
            StockInfo stock,
            string? outboundPnOrCode,
            string? productIdFromOrderLine,
            IEnumerable<string>? additionalMaterialIds)
        {
            var sid = stock.MaterialId?.Trim() ?? "";
            if (string.IsNullOrEmpty(sid)) return false;
            var code = outboundPnOrCode?.Trim() ?? "";
            if (!string.IsNullOrEmpty(code) && string.Equals(sid, code, StringComparison.OrdinalIgnoreCase))
                return true;
            var pid = productIdFromOrderLine?.Trim();
            if (!string.IsNullOrEmpty(pid) && string.Equals(sid, pid, StringComparison.OrdinalIgnoreCase))
                return true;
            if (additionalMaterialIds != null)
            {
                foreach (var ex in additionalMaterialIds)
                {
                    var k = ex?.Trim() ?? "";
                    if (k.Length > 0 && string.Equals(sid, k, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 以销定采下，入库常以采购明细主键为 MaterialId；将其与销售明细关联的采购行 Id、ProductId 纳入出库匹配。
        /// </summary>
        public static List<string> LinkedPurchaseMaterialKeys(IEnumerable<PurchaseOrderItem>? poItemsForSellLine)
        {
            var keys = new List<string>();
            if (poItemsForSellLine == null) return keys;
            foreach (var poi in poItemsForSellLine)
            {
                if (!string.IsNullOrWhiteSpace(poi.Id)) keys.Add(poi.Id.Trim());
                if (!string.IsNullOrWhiteSpace(poi.ProductId)) keys.Add(poi.ProductId.Trim());
            }

            return keys;
        }
    }
}
