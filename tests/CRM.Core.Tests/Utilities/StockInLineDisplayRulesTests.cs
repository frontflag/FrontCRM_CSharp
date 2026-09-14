using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class StockInLineDisplayRulesTests
{
    [Fact]
    public void CustomsLine_UsesPurchaseSnapshot_WhenMaterialIdIsUnrelatedGuid()
    {
        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: Guid.NewGuid().ToString(),
            purchasePn: "SDINBDG4-8",
            purchaseBrand: "Samsung",
            detailModel: null,
            detailBrand: null,
            materials: EmptyMaterials(),
            poLines: null,
            out var model,
            out var brand);

        Assert.Equal("SDINBDG4-8", model);
        Assert.Equal("Samsung", brand);
    }

    [Fact]
    public void EmptyMaterialId_UsesPurchaseSnapshot()
    {
        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: "  ",
            purchasePn: " PN-A ",
            purchaseBrand: " Brand-A ",
            detailModel: null,
            detailBrand: null,
            materials: EmptyMaterials(),
            poLines: null,
            out var model,
            out var brand);

        Assert.Equal("PN-A", model);
        Assert.Equal("Brand-A", brand);
    }

    [Fact]
    public void DetailSnapshot_PrefersOverPurchase_WhenMasterMisses()
    {
        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: Guid.NewGuid().ToString(),
            purchasePn: "FROM-PO",
            purchaseBrand: "FROM-PO-BRAND",
            detailModel: "DETAIL-PN",
            detailBrand: "DETAIL-BRAND",
            materials: EmptyMaterials(),
            poLines: null,
            out var model,
            out var brand);

        Assert.Equal("DETAIL-PN", model);
        Assert.Equal("DETAIL-BRAND", brand);
    }

    [Fact]
    public void MaterialMaster_TakesPrecedenceOverSnapshot()
    {
        var matId = Guid.NewGuid().ToString();
        var materials = new Dictionary<string, MaterialInfo>(StringComparer.OrdinalIgnoreCase)
        {
            [matId] = new MaterialInfo
            {
                Id = matId,
                MaterialModel = "MASTER-PN",
                MaterialName = "MasterBrand"
            }
        };

        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: matId,
            purchasePn: "SNAP-PN",
            purchaseBrand: "SNAP-BRAND",
            detailModel: null,
            detailBrand: null,
            materials: materials,
            poLines: null,
            out var model,
            out var brand);

        Assert.Equal("MASTER-PN", model);
        Assert.Equal("MasterBrand", brand);
    }

    [Fact]
    public void PurchaseOrderLine_FillsWhenMaterialMisses()
    {
        var poLineId = Guid.NewGuid().ToString();
        var poLines = new List<PurchaseOrderItem>
        {
            new() { Id = poLineId, PN = "PO-PN", Brand = "PO-Brand" }
        };

        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: poLineId,
            purchasePn: null,
            purchaseBrand: null,
            detailModel: null,
            detailBrand: null,
            materials: EmptyMaterials(),
            poLines: poLines,
            out var model,
            out var brand);

        Assert.Equal("PO-PN", model);
        Assert.Equal("PO-Brand", brand);
    }

    [Fact]
    public void QcStuffedPn_UsedAsModel_WhenNotGuid()
    {
        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: "SDINBDG4-8",
            purchasePn: null,
            purchaseBrand: null,
            detailModel: null,
            detailBrand: null,
            materials: EmptyMaterials(),
            poLines: null,
            out var model,
            out var brand);

        Assert.Equal("SDINBDG4-8", model);
        Assert.Null(brand);
    }

    [Fact]
    public void MatPlaceholder_IsNotShownAsModel()
    {
        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: "MAT-1",
            purchasePn: null,
            purchaseBrand: null,
            detailModel: null,
            detailBrand: null,
            materials: EmptyMaterials(),
            poLines: null,
            out var model,
            out var brand);

        Assert.Null(model);
        Assert.Null(brand);
    }

    [Fact]
    public void GuidWithoutSnapshot_StaysEmpty()
    {
        StockInLineDisplayRules.ResolveDisplayModelBrand(
            materialId: Guid.NewGuid().ToString(),
            purchasePn: null,
            purchaseBrand: "  ",
            detailModel: null,
            detailBrand: null,
            materials: EmptyMaterials(),
            poLines: null,
            out var model,
            out var brand);

        Assert.Null(model);
        Assert.Null(brand);
    }

    [Fact]
    public void Filter_HitsPurchasePn_WhenSummaryEmpty()
    {
        var lines = new[]
        {
            new StockInItem { MaterialId = Guid.NewGuid().ToString(), PurchasePn = "SDINBDG4-8", PurchaseBrand = "Samsung" }
        };

        Assert.True(StockInLineDisplayRules.MatchesModelKeyword("sdinbdg", lines, null, null));
        Assert.True(StockInLineDisplayRules.MatchesModelKeyword("samsung", lines, null, null));
        Assert.False(StockInLineDisplayRules.MatchesModelKeyword("other", lines, null, null));
    }

    [Fact]
    public void Filter_HitsListSummary()
    {
        Assert.True(StockInLineDisplayRules.MatchesModelKeyword(
            "SDIN",
            Array.Empty<StockInItem>(),
            "SDINBDG4-8",
            "Samsung"));
    }

    private static Dictionary<string, MaterialInfo> EmptyMaterials() =>
        new(StringComparer.OrdinalIgnoreCase);
}
