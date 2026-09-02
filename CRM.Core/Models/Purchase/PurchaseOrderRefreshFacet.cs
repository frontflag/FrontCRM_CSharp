namespace CRM.Core.Models.Purchase;

/// <summary>采购订单详情分面刷新。</summary>
public enum PurchaseOrderRefreshFacet
{
    Status = 0,
    Vendor = 1,
    Pn = 2,
    Brand = 3,
    Qty = 4,
    Price = 5
}

public static class PurchaseOrderRefreshFacetParser
{
    public static bool TryParse(string? raw, out PurchaseOrderRefreshFacet facet)
    {
        facet = PurchaseOrderRefreshFacet.Status;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "status":
                facet = PurchaseOrderRefreshFacet.Status;
                return true;
            case "vendor":
                facet = PurchaseOrderRefreshFacet.Vendor;
                return true;
            case "pn":
            case "partno":
            case "型号":
            case "物料型号":
                facet = PurchaseOrderRefreshFacet.Pn;
                return true;
            case "brand":
            case "品牌":
                facet = PurchaseOrderRefreshFacet.Brand;
                return true;
            case "qty":
            case "quantity":
            case "数量":
                facet = PurchaseOrderRefreshFacet.Qty;
                return true;
            case "price":
            case "cost":
            case "单价":
                facet = PurchaseOrderRefreshFacet.Price;
                return true;
            default:
                return false;
        }
    }

    public static string ToApiValue(this PurchaseOrderRefreshFacet facet) => facet switch
    {
        PurchaseOrderRefreshFacet.Vendor => "vendor",
        PurchaseOrderRefreshFacet.Pn => "pn",
        PurchaseOrderRefreshFacet.Brand => "brand",
        PurchaseOrderRefreshFacet.Qty => "qty",
        PurchaseOrderRefreshFacet.Price => "price",
        _ => "status"
    };
}

public class PurchaseOrderRefreshRequest
{
    /// <summary><c>status</c> / <c>vendor</c> / <c>pn</c> / <c>brand</c> / <c>qty</c> / <c>price</c></summary>
    public string? Facet { get; set; }
    /// <summary>预检发现已完结下游后，用户确认覆盖时为 true。</summary>
    public bool ConfirmCompleted { get; set; }
}
