namespace CRM.Core.Models.Sales;

/// <summary>销售订单详情分面刷新。</summary>
public enum SalesOrderRefreshFacet
{
    Status = 0,
    Customer = 1,
    Pn = 2,
    Brand = 3,
    Qty = 4,
    Price = 5
}

public static class SalesOrderRefreshFacetParser
{
    public static bool TryParse(string? raw, out SalesOrderRefreshFacet facet)
    {
        facet = SalesOrderRefreshFacet.Status;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        switch (raw.Trim().ToLowerInvariant())
        {
            case "status":
                facet = SalesOrderRefreshFacet.Status;
                return true;
            case "customer":
            case "客户":
                facet = SalesOrderRefreshFacet.Customer;
                return true;
            case "pn":
            case "partno":
            case "型号":
            case "物料型号":
                facet = SalesOrderRefreshFacet.Pn;
                return true;
            case "brand":
            case "品牌":
                facet = SalesOrderRefreshFacet.Brand;
                return true;
            case "qty":
            case "quantity":
            case "数量":
                facet = SalesOrderRefreshFacet.Qty;
                return true;
            case "price":
            case "单价":
                facet = SalesOrderRefreshFacet.Price;
                return true;
            default:
                return false;
        }
    }

    public static string ToApiValue(this SalesOrderRefreshFacet facet) => facet switch
    {
        SalesOrderRefreshFacet.Customer => "customer",
        SalesOrderRefreshFacet.Pn => "pn",
        SalesOrderRefreshFacet.Brand => "brand",
        SalesOrderRefreshFacet.Qty => "qty",
        SalesOrderRefreshFacet.Price => "price",
        _ => "status"
    };
}

public class SalesOrderRefreshRequest
{
    /// <summary><c>status</c> / <c>customer</c> / <c>pn</c> / <c>brand</c> / <c>qty</c> / <c>price</c></summary>
    public string? Facet { get; set; }
    /// <summary>预检发现已完结下游后，用户确认覆盖时为 true。</summary>
    public bool ConfirmCompleted { get; set; }
}
