using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Utilities;

/// <summary>
/// 入库单列表/详情「物料型号 / 品牌」展示与型号筛选：
/// 物料主数据 → 采购行 → 入库明细快照（<c>PurchasePn</c>/<c>PurchaseBrand</c>）→ 非 GUID 的 MaterialId。
/// 报关入库常无采购头、MaterialId 也不是物料主键，须走快照，否则列表显示「—」。
/// </summary>
public static class StockInLineDisplayRules
{
    public static void ResolveDisplayModelBrand(
        string? materialId,
        string? purchasePn,
        string? purchaseBrand,
        string? detailModel,
        string? detailBrand,
        IReadOnlyDictionary<string, MaterialInfo>? materials,
        IReadOnlyList<PurchaseOrderItem>? poLines,
        out string? model,
        out string? brand)
    {
        var mid = materialId?.Trim();
        ResolveFromMaterialAndPo(mid, materials, poLines, out model, out brand);

        if (string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(detailModel))
            model = detailModel.Trim();
        if (string.IsNullOrWhiteSpace(brand) && !string.IsNullOrWhiteSpace(detailBrand))
            brand = detailBrand.Trim();
        if (string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(purchasePn))
            model = purchasePn.Trim();
        if (string.IsNullOrWhiteSpace(brand) && !string.IsNullOrWhiteSpace(purchaseBrand))
            brand = purchaseBrand.Trim();
        if (string.IsNullOrWhiteSpace(model) && IsPnLikeMaterialId(mid))
            model = mid;
    }

    /// <summary>
    /// 列表「物料型号」筛选：命中明细 MaterialId / 采购快照，或已拼好的列表摘要。
    /// </summary>
    public static bool MatchesModelKeyword(
        string? keyword,
        IEnumerable<StockInItem>? lines,
        string? modelSummary,
        string? brandSummary)
    {
        var k = keyword?.Trim();
        if (string.IsNullOrEmpty(k))
            return true;
        if (Contains(modelSummary, k) || Contains(brandSummary, k))
            return true;
        if (lines == null)
            return false;
        foreach (var line in lines)
        {
            if (Contains(line.MaterialId, k)
                || Contains(line.PurchasePn, k)
                || Contains(line.PurchaseBrand, k)
                || Contains(line.DetailMaterialModel, k)
                || Contains(line.DetailMaterialBrand, k))
                return true;
        }

        return false;
    }

    public static bool IsPnLikeMaterialId(string? materialId)
    {
        var mid = materialId?.Trim();
        if (string.IsNullOrEmpty(mid))
            return false;
        if (mid.StartsWith("MAT-", StringComparison.OrdinalIgnoreCase))
            return false;
        return !Guid.TryParse(mid, out _);
    }

    private static void ResolveFromMaterialAndPo(
        string? materialIdTrimmed,
        IReadOnlyDictionary<string, MaterialInfo>? materials,
        IReadOnlyList<PurchaseOrderItem>? poLines,
        out string? model,
        out string? brand)
    {
        model = null;
        brand = null;
        if (!string.IsNullOrEmpty(materialIdTrimmed) && materials != null
            && materials.TryGetValue(materialIdTrimmed, out var mat))
        {
            if (!string.IsNullOrWhiteSpace(mat.MaterialModel))
                model = mat.MaterialModel.Trim();
            if (!string.IsNullOrWhiteSpace(mat.MaterialName))
                brand = mat.MaterialName.Trim();
        }

        PurchaseOrderItem? hit = null;
        if (poLines is { Count: > 0 })
        {
            if (!string.IsNullOrEmpty(materialIdTrimmed))
            {
                hit = poLines.FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.ProductId) &&
                    string.Equals(p.ProductId.Trim(), materialIdTrimmed, StringComparison.OrdinalIgnoreCase));
                if (hit == null)
                    hit = poLines.FirstOrDefault(p =>
                        string.Equals(p.Id, materialIdTrimmed, StringComparison.OrdinalIgnoreCase));
            }

            if (hit == null && poLines.Count == 1)
                hit = poLines[0];
        }

        if (hit == null)
            return;
        if (string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(hit.PN))
            model = hit.PN!.Trim();
        if (string.IsNullOrWhiteSpace(brand) && !string.IsNullOrWhiteSpace(hit.Brand))
            brand = hit.Brand!.Trim();
    }

    private static bool Contains(string? haystack, string keyword) =>
        !string.IsNullOrWhiteSpace(haystack)
        && haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase);
}
