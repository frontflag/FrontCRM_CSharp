using CRM.Core.Models.Purchase;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Sales;

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

        /// <summary>
        /// 备货库存补充拣货：库存为备货类型，且物料主数据上的型号/品牌与销售订单明细 PN、Brand 一致（忽略大小写）。
        /// 销售明细未填 PN、Brand 时不视为可匹配的备货补充（避免放宽到全部备货批次）。
        /// </summary>
        public static bool StockingMatchesSellOrderPnBrand(StockInfo stock, SellOrderItem? line, MaterialInfo? material)
        {
            if (stock.StockType != 2 || line == null || material == null)
                return false;
            var soPn = line.PN?.Trim() ?? "";
            var soBrand = line.Brand?.Trim() ?? "";
            if (soPn.Length == 0 && soBrand.Length == 0)
                return false;

            var mModel = material.MaterialModel?.Trim() ?? "";
            var mCode = material.MaterialCode?.Trim() ?? "";
            var mName = material.MaterialName?.Trim() ?? "";

            if (soPn.Length > 0)
            {
                var pnOk = string.Equals(mModel, soPn, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(mCode, soPn, StringComparison.OrdinalIgnoreCase);
                if (!pnOk)
                    return false;
            }

            if (soBrand.Length > 0 && !string.Equals(mName, soBrand, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// 按物料编码、规格型号建立索引（忽略大小写），用于库存行 <see cref="StockInfo.MaterialId"/> 存 PN/编码而非主键时的解析。
        /// </summary>
        public static Dictionary<string, MaterialInfo> BuildMaterialCodeModelIndex(IEnumerable<MaterialInfo> materials)
        {
            var d = new Dictionary<string, MaterialInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in materials)
            {
                var code = m.MaterialCode?.Trim();
                if (!string.IsNullOrEmpty(code) && !d.ContainsKey(code))
                    d[code] = m;
                var model = m.MaterialModel?.Trim();
                if (!string.IsNullOrEmpty(model) && !d.ContainsKey(model))
                    d[model] = m;
            }

            return d;
        }

        /// <summary>
        /// 库存行关联的采购明细：MaterialId 或冗余的 PurchaseOrderItemId 常为采购行主键。
        /// </summary>
        public static PurchaseOrderItem? ResolvePurchaseOrderLineForStock(
            StockInfo stock,
            IReadOnlyDictionary<string, PurchaseOrderItem> poItemById)
        {
            var mid = stock.MaterialId?.Trim();
            if (!string.IsNullOrEmpty(mid) && poItemById.TryGetValue(mid, out var p))
                return p;
            var pk = stock.PurchaseOrderItemId?.Trim();
            if (!string.IsNullOrEmpty(pk) && poItemById.TryGetValue(pk, out p))
                return p;
            return null;
        }

        /// <summary>
        /// 解析库存行对应的物料主数据：先 MaterialId，再编码/型号，再经采购行 ProductId。
        /// </summary>
        public static MaterialInfo? ResolveMaterialForStockRow(
            StockInfo stock,
            IReadOnlyDictionary<string, MaterialInfo> materialById,
            IReadOnlyDictionary<string, MaterialInfo> materialByAltKey,
            IReadOnlyDictionary<string, PurchaseOrderItem> poItemById)
        {
            var mid = stock.MaterialId?.Trim() ?? "";
            if (mid.Length == 0)
                return null;
            if (materialById.TryGetValue(mid, out var m))
                return m;
            if (materialByAltKey.TryGetValue(mid, out m))
                return m;

            if (poItemById.TryGetValue(mid, out var poi))
            {
                var pid = poi.ProductId?.Trim();
                if (!string.IsNullOrEmpty(pid) && materialById.TryGetValue(pid, out m))
                    return m;
            }

            var poiId = stock.PurchaseOrderItemId?.Trim();
            if (!string.IsNullOrEmpty(poiId)
                && !string.Equals(poiId, mid, StringComparison.OrdinalIgnoreCase)
                && poItemById.TryGetValue(poiId, out poi))
            {
                var pid = poi.ProductId?.Trim();
                if (!string.IsNullOrEmpty(pid) && materialById.TryGetValue(pid, out m))
                    return m;
            }

            return null;
        }

        /// <summary>
        /// 备货库存（Type=2）是否可作为「来源2」补充拣货：仅依据销单行与物料主数据/关联采购行 PN·品牌，不要求
        /// <see cref="Matches"/> 成立（备货行 <see cref="StockInfo.MaterialId"/> 常为未关联本单的采购明细主键，不在 linkedKeys 中）。
        /// </summary>
        public static bool StockingSupplementEligible(
            StockInfo stock,
            SellOrderItem? sellLine,
            IReadOnlyDictionary<string, MaterialInfo> materialById,
            IReadOnlyDictionary<string, MaterialInfo> materialByAltKey,
            IReadOnlyDictionary<string, PurchaseOrderItem> poItemById)
        {
            if (stock.StockType != 2 || sellLine == null)
                return false;
            var mat = ResolveMaterialForStockRow(stock, materialById, materialByAltKey, poItemById);
            var poi = ResolvePurchaseOrderLineForStock(stock, poItemById);
            return StockingMatchesSellOrderForPicking(stock, sellLine, mat, poi);
        }

        /// <summary>
        /// 备货补充拣货是否可用：优先用物料主数据比对销单行 PN/品牌；无主数据时用库存关联采购行的 PN/品牌比对（与库存中心展示口径一致）。
        /// </summary>
        public static bool StockingMatchesSellOrderForPicking(
            StockInfo stock,
            SellOrderItem? sellLine,
            MaterialInfo? material,
            PurchaseOrderItem? poLineForStock)
        {
            if (stock.StockType != 2 || sellLine == null)
                return false;
            if (material != null)
                return StockingMatchesSellOrderPnBrand(stock, sellLine, material);
            if (poLineForStock != null)
                return SellOrderPnBrandMatchesPurchaseLine(sellLine, poLineForStock);
            return false;
        }

        private static bool SellOrderPnBrandMatchesPurchaseLine(SellOrderItem line, PurchaseOrderItem poLine)
        {
            var soPn = line.PN?.Trim() ?? "";
            var soBrand = line.Brand?.Trim() ?? "";
            if (soPn.Length == 0 && soBrand.Length == 0)
                return false;
            var poPn = poLine.PN?.Trim() ?? "";
            var poBrand = poLine.Brand?.Trim() ?? "";
            if (soPn.Length > 0 && !string.Equals(soPn, poPn, StringComparison.OrdinalIgnoreCase))
                return false;
            if (soBrand.Length > 0 && !string.Equals(soBrand, poBrand, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
    }
}
