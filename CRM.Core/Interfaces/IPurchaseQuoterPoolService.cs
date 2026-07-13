namespace CRM.Core.Interfaces;

public class PurchaseQuoterPoolMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? RealName { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSelected { get; set; }
}

public class PurchaseQuoterPoolListResult
{
    public IReadOnlyList<PurchaseQuoterPoolMemberDto> Items { get; set; } = Array.Empty<PurchaseQuoterPoolMemberDto>();
    public int SelectedCount { get; set; }
}

public interface IPurchaseQuoterPoolService
{
    Task<int> GetAssigneeCountAsync(CancellationToken cancellationToken = default);

    Task SetAssigneeCountAsync(int count, CancellationToken cancellationToken = default);

    /// <param name="filter">all 或 selected</param>
    Task<PurchaseQuoterPoolListResult> ListMembersAsync(string? filter, CancellationToken cancellationToken = default);

    Task<PurchaseQuoterPoolListResult> SavePoolAsync(IReadOnlyList<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>轮询用：按 sort_order 排序，且仅返回在职用户。</summary>
    Task<IReadOnlyList<string>> GetOrderedActivePoolUserIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>需求明细保护时长（分钟）；0 表示关闭。</summary>
    Task<int> GetDemandProtectionMinutesAsync(CancellationToken cancellationToken = default);

    Task SetDemandProtectionMinutesAsync(int minutes, CancellationToken cancellationToken = default);
}
