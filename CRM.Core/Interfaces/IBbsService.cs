using CRM.Core.Models.Bbs;

namespace CRM.Core.Interfaces;

public interface IBbsService
{
    Task<IReadOnlyList<BbsSubjectListItemDto>> GetTopSubjectsAsync(
        string userId,
        bool isModerator,
        CancellationToken ct = default);

    Task<BbsSubjectPagedDto> QuerySubjectsAsync(
        BbsSubjectQuery query,
        string userId,
        bool isModerator,
        CancellationToken ct = default);

    Task<BbsSubjectDetailDto?> GetSubjectDetailAsync(
        string subjectId,
        string userId,
        bool isModerator,
        CancellationToken ct = default);

    Task<BbsSubjectDetailDto> CreateSubjectAsync(
        BbsSubjectCreateRequest request,
        string userId,
        bool isModerator,
        CancellationToken ct = default);

    Task<BbsSubjectDetailDto> UpdateSubjectAsync(
        string subjectId,
        BbsSubjectUpdateRequest request,
        string userId,
        bool isModerator,
        CancellationToken ct = default);

    Task CloseSubjectAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default);
    Task OpenSubjectAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default);
    Task SetTopAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default);
    Task CancelTopAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default);
    Task DeleteSubjectAsync(string subjectId, string userId, bool isModerator, CancellationToken ct = default);

    Task<BbsReplyPagedDto> GetRepliesAsync(
        string subjectId,
        int page,
        int pageSize,
        string userId,
        bool isModerator,
        CancellationToken ct = default);

    Task<BbsReplyDto> AddReplyAsync(
        string subjectId,
        BbsReplyCreateRequest request,
        string userId,
        bool isModerator,
        CancellationToken ct = default);

    Task DeleteReplyAsync(string replyId, string userId, bool isModerator, CancellationToken ct = default);
}
