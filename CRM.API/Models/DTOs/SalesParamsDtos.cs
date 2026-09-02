namespace CRM.API.Models.DTOs;

public class SalesParamsAllowRefreshCompletedBizNodesDto
{
    public bool Allow { get; set; }
}

public class SetSalesParamsAllowRefreshCompletedBizNodesRequest
{
    public bool Allow { get; set; }
}

public class SalesParamsRefreshCompletedFacetsDto
{
    public bool Customer { get; set; }
    public bool Pn { get; set; } = true;
    public bool Brand { get; set; } = true;
    public bool Qty { get; set; } = true;
    public bool Price { get; set; } = true;
}

public class SetSalesParamsRefreshCompletedFacetsRequest
{
    public bool Customer { get; set; }
    public bool Pn { get; set; } = true;
    public bool Brand { get; set; } = true;
    public bool Qty { get; set; } = true;
    public bool Price { get; set; } = true;
}
