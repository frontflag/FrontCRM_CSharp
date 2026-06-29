using CRM.Core.Models.Tag;

namespace CRM.Core.Interfaces;

/// <summary>需求主表标签应用（权限、审计、校验）。</summary>
public interface IRfqTagService
{
    Task<IReadOnlyList<EntityTagDto>> GetTagsForRfqAsync(string rfqId, string? viewerUserId);

    Task ApplyTagsAsync(string rfqId, IReadOnlyList<string> tagIds, string actorUserId, string? actorUserName);

    Task RemoveTagsAsync(string rfqId, IReadOnlyList<string> tagIds, string actorUserId, string? actorUserName);

    Task<IReadOnlyDictionary<string, IReadOnlyList<EntityTagDto>>> GetTagsForRfqIdsAsync(
        IEnumerable<string> rfqIds,
        string? viewerUserId,
        IEnumerable<(string RfqId, string? CreateByUserId, string? SalesUserId)> rfqRows);

    Task ValidateCustomTagCreateAsync(string name, string actorUserId);

    Task<TagDefinition> CreateUserRfqTagAsync(string name, string? color, string actorUserId);
}
