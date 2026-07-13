namespace CRM.API.Models.DTOs;

public class PurchaseParamsAssigneeCountDto
{
    public int Count { get; set; }
}

public class PurchaseParamsDemandProtectionMinutesDto
{
    public int Minutes { get; set; }
}

public class SetPurchaseParamsAssigneeCountRequest
{
    public int Count { get; set; }
}

public class SetPurchaseParamsDemandProtectionMinutesRequest
{
    public int Minutes { get; set; }
}

public class PurchaseQuoterPoolMemberResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
}

public class PurchaseQuoterPoolListResponse
{
    public int SelectedCount { get; set; }
    public List<PurchaseQuoterPoolMemberResponse> Items { get; set; } = new();
}

public class SavePurchaseQuoterPoolRequest
{
    public List<string>? UserIds { get; set; }
}
