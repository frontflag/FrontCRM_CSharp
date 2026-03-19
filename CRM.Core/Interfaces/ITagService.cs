using CRM.Core.Models.Tag;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 标签管理服务
    /// </summary>
    public interface ITagService
    {
        Task<PagedResult<TagDefinition>> SearchTagsAsync(TagSearchRequest request);
        Task<TagDefinition> CreateTagAsync(CreateTagRequest request, long currentUserId);
        Task<TagDefinition> UpdateTagAsync(string id, UpdateTagRequest request, long currentUserId);
        Task DisableTagAsync(string id, long currentUserId);
        Task EnableTagAsync(string id, long currentUserId);
        Task DeleteTagAsync(string id);
        Task<TagDefinition?> GetByIdAsync(string id);
    }

    /// <summary>
    /// 标签查询请求
    /// </summary>
    public class TagSearchRequest
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public short? Type { get; set; }
        public string? Category { get; set; }
        public string? EntityType { get; set; }
        public string? Keyword { get; set; }
        public short? Status { get; set; }
    }

    /// <summary>
    /// 创建标签请求
    /// </summary>
    public class CreateTagRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public short Type { get; set; } = 2;
        public string? Category { get; set; }
        public string? Scope { get; set; }
        public short Visibility { get; set; } = 3;
    }

    /// <summary>
    /// 更新标签请求
    /// </summary>
    public class UpdateTagRequest
    {
        public string? Name { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public string? Category { get; set; }
        public string? Scope { get; set; }
        public short? Visibility { get; set; }
        public short? Status { get; set; }
    }

    /// <summary>
    /// 标签应用服务
    /// </summary>
    public interface ITagApplyService
    {
        Task AddTagsToEntityAsync(AddTagsToEntityRequest request);
        Task RemoveTagsFromEntityAsync(RemoveTagsFromEntityRequest request);
        Task<IReadOnlyList<TagDefinition>> GetTagsForEntityAsync(string entityType, string entityId);
        Task<IReadOnlyList<TagDefinition>> GetUserCommonTagsAsync(long userId, string? entityType, int topN = 10);
        Task<IReadOnlyList<TagDefinition>> GetUserRecentTagsAsync(long userId, string? entityType, TimeSpan? range = null);
    }

    public class AddTagsToEntityRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public List<string> EntityIds { get; set; } = new();
        public List<string> TagIds { get; set; } = new();
        public long AppliedByUserId { get; set; }
        public string Source { get; set; } = "Manual";
    }

    public class RemoveTagsFromEntityRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public List<string> EntityIds { get; set; } = new();
        public List<string> TagIds { get; set; } = new();
        public long AppliedByUserId { get; set; }
        public string Source { get; set; } = "Manual";
    }

    /// <summary>
    /// 标签筛选服务
    /// </summary>
    public interface ITagFilterService
    {
        Task<IReadOnlyList<string>> QueryEntityIdsByTagsAsync(TagFilterRequest request);
    }

    public class TagFilterRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public List<string> IncludeTagIds { get; set; } = new();
        public string IncludeLogic { get; set; } = "AND"; // AND / OR
        public List<string> ExcludeTagIds { get; set; } = new();
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }
}

