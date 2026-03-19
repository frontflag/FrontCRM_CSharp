using CRM.Core.Models.Draft;

namespace CRM.Core.Interfaces
{
    public interface IDraftService
    {
        Task<DraftDto> SaveDraftAsync(long userId, SaveDraftRequest request);
        Task<IReadOnlyList<DraftDto>> GetDraftsAsync(long userId, GetDraftsRequest request);
        Task<DraftDto?> GetDraftByIdAsync(long userId, string draftId);
        Task DeleteDraftAsync(long userId, string draftId);
        Task<DraftConvertResultDto> ConvertDraftAsync(long userId, string draftId);
    }

    public class SaveDraftRequest
    {
        public string? DraftId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? DraftName { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public string? Remark { get; set; }
    }

    public class GetDraftsRequest
    {
        public string? EntityType { get; set; }
        public short? Status { get; set; }
        public string? Keyword { get; set; }
    }

    public class DraftDto
    {
        public string DraftId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? DraftName { get; set; }
        public string PayloadJson { get; set; } = "{}";
        public short Status { get; set; }
        public string? Remark { get; set; }
        public string? ConvertedEntityId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? ModifyTime { get; set; }
    }

    public class DraftConvertResultDto
    {
        public string DraftId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
    }
}
