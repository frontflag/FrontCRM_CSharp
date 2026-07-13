namespace CRM.Core.Interfaces.RfqAssignment;

public enum RfqAssignmentTrigger
{
    Create = 1,
    AddItems = 2
}

public sealed class RfqItemAssignmentInput
{
    public string ItemKey { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string? Mpn { get; set; }
    public string? Brand { get; set; }
    public long? BrandId { get; set; }
}

public sealed class RfqAssignmentContext
{
    public string RfqId { get; set; } = string.Empty;
    public string? RfqCode { get; set; }
    public RfqAssignmentTrigger Trigger { get; set; }
    public IReadOnlyList<RfqItemAssignmentInput> Items { get; set; } = Array.Empty<RfqItemAssignmentInput>();

    /// <summary>编辑新增明细时：本需求单内已有品牌已分配的采购员（品牌键 → 采购员对）。</summary>
    public IReadOnlyDictionary<string, (string? PurchaserUserId1, string? PurchaserUserId2)>? ExistingBrandAssignees { get; set; }
}

public sealed class RfqItemAssigneePair
{
    public string ItemKey { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string? PurchaserUserId1 { get; set; }
    public string? PurchaserUserId2 { get; set; }
}

public sealed class RfqPurchaserAssignmentOutcome
{
    public short AssignMethodCode { get; set; }
    public IReadOnlyList<RfqItemAssigneePair> Assignments { get; set; } = Array.Empty<RfqItemAssigneePair>();

    public bool AnyAssigned =>
        Assignments.Any(a => !string.IsNullOrWhiteSpace(a.PurchaserUserId1));
}
