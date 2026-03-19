using CRM.Core.Interfaces;
using CRM.Core.Models.Tag;

namespace CRM.Core.Services
{
    /// <summary>
    /// 标签筛选服务实现
    /// </summary>
    public class TagFilterService : ITagFilterService
    {
        private readonly IRepository<TagRelation> _relationRepository;

        public TagFilterService(IRepository<TagRelation> relationRepository)
        {
            _relationRepository = relationRepository;
        }

        public async Task<IReadOnlyList<string>> QueryEntityIdsByTagsAsync(TagFilterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EntityType))
                throw new ArgumentException("实体类型不能为空", nameof(request.EntityType));

            var entityType = request.EntityType.Trim().ToUpperInvariant();
            var includeTagIds = request.IncludeTagIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? new List<string>();
            var excludeTagIds = request.ExcludeTagIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? new List<string>();

            var allRelations = await _relationRepository.FindAsync(r => r.EntityType == entityType);

            // 基础集合
            var grouped = allRelations.GroupBy(r => r.EntityId).ToDictionary(g => g.Key, g => g.Select(r => r.TagId).ToHashSet());

            IEnumerable<string> candidateIds = grouped.Keys;

            // 包含标签逻辑
            if (includeTagIds.Any())
            {
                if (string.Equals(request.IncludeLogic, "OR", StringComparison.OrdinalIgnoreCase))
                {
                    candidateIds = candidateIds.Where(id => grouped[id].Intersect(includeTagIds).Any());
                }
                else
                {
                    candidateIds = candidateIds.Where(id => includeTagIds.All(tagId => grouped[id].Contains(tagId)));
                }
            }

            // 排除标签
            if (excludeTagIds.Any())
            {
                candidateIds = candidateIds.Where(id => !grouped[id].Intersect(excludeTagIds).Any());
            }

            var result = candidateIds.ToList();

            if (request.PageIndex.HasValue && request.PageSize.HasValue && request.PageIndex > 0 && request.PageSize > 0)
            {
                result = result
                    .Skip((request.PageIndex.Value - 1) * request.PageSize.Value)
                    .Take(request.PageSize.Value)
                    .ToList();
            }

            return result;
        }
    }
}

