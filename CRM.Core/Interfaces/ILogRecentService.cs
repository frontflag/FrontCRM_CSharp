using CRM.Core.Models.Logs;

namespace CRM.Core.Interfaces
{
    public interface ILogRecentService
    {
        Task RecordAsync(string userId, string bizType, string recordId, string? recordCode, string openKind);
        Task<IReadOnlyList<LogRecentItem>> ListAsync(string userId, string bizType, int take);
    }
}
