using CRM.Core.Models.Bbs;

namespace CRM.Core.Interfaces;

public interface IBbsService
{
    Task<IReadOnlySet<int>> GetModeratedTypesAsync(string userId, CancellationToken ct = default);

    Task<IReadOnlyList<BbsBoardModeratorDto>> ListBoardModeratorsAsync(CancellationToken ct = default);

    Task<BbsBoardModeratorDto> SetBoardModeratorAsync(
        int type,
        string? userId,
        string? displayName,
        int? sortOrder,
        string operatorUserId,
        CancellationToken ct = default);

    /// <summary>新建自定义板块（type≥100）。</summary>
    Task<BbsBoardModeratorDto> CreateBoardAsync(
        string displayName,
        string operatorUserId,
        CancellationToken ct = default);

    /// <summary>按列表顺序批量更新可拖放板块的显示顺序。</summary>
    Task ReorderBoardsAsync(
        IReadOnlyList<int> orderedTypes,
        string operatorUserId,
        CancellationToken ct = default);

    /// <summary>无帖时软删除板块（侧栏隐藏）。</summary>
    Task DeleteBoardAsync(int type, string operatorUserId, CancellationToken ct = default);

    Task<IReadOnlyList<BbsSubjectListItemDto>> GetTopSubjectsAsync(
        BbsActorContext actor,
        CancellationToken ct = default);

    Task<BbsBoardStatsDto> GetBoardStatsAsync(CancellationToken ct = default);

    Task<BbsSubjectPagedDto> QuerySubjectsAsync(
        BbsSubjectQuery query,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task<BbsSubjectDetailDto?> GetSubjectDetailAsync(
        string subjectId,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task<BbsSubjectDetailDto> CreateSubjectAsync(
        BbsSubjectCreateRequest request,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task<BbsSubjectDetailDto> UpdateSubjectAsync(
        string subjectId,
        BbsSubjectUpdateRequest request,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task CloseSubjectAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default);
    Task OpenSubjectAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default);
    Task SetTopAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default);
    Task CancelTopAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default);
    Task DeleteSubjectAsync(string subjectId, BbsActorContext actor, CancellationToken ct = default);

    Task<BbsReplyPagedDto> GetRepliesAsync(
        string subjectId,
        int page,
        int pageSize,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task<BbsReplyDto> AddReplyAsync(
        string subjectId,
        BbsReplyCreateRequest request,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task DeleteReplyAsync(string replyId, BbsActorContext actor, CancellationToken ct = default);

    Task<IReadOnlyList<BbsMediaItemDto>> ListSubjectMediaAsync(
        string subjectId,
        CancellationToken ct = default);

    Task<IReadOnlyList<BbsMediaItemDto>> UploadSubjectMediaAsync(
        string subjectId,
        IReadOnlyList<BbsMediaUploadFile> files,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task DeleteSubjectMediaAsync(
        string documentId,
        BbsActorContext actor,
        CancellationToken ct = default);

    Task<BbsReactionResultDto> SetSubjectReactionAsync(
        string subjectId,
        int value,
        string userId,
        CancellationToken ct = default);
}
