namespace CRM.Core.Interfaces;

public interface IDashboardNoticeService
{
    Task<IReadOnlyList<DashboardNoticeItemDto>> ListAsync(string userId, CancellationToken ct = default);
    Task DismissBbsAsync(string subjectId, string userId, CancellationToken ct = default);
}

public class DashboardNoticeItemDto
{
    public string Kind { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime At { get; set; }
}
