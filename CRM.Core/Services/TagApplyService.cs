using CRM.Core.Interfaces;
using CRM.Core.Models.Tag;

namespace CRM.Core.Services
{
    /// <summary>
    /// 标签应用服务实现
    /// </summary>
    public class TagApplyService : ITagApplyService
    {
        private readonly IRepository<TagDefinition> _tagRepository;
        private readonly IRepository<TagRelation> _relationRepository;
        private readonly IRepository<UserTagPreference> _preferenceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TagApplyService(
            IRepository<TagDefinition> tagRepository,
            IRepository<TagRelation> relationRepository,
            IRepository<UserTagPreference> preferenceRepository,
            IUnitOfWork unitOfWork)
        {
            _tagRepository = tagRepository;
            _relationRepository = relationRepository;
            _preferenceRepository = preferenceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task AddTagsToEntityAsync(AddTagsToEntityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EntityType))
                throw new ArgumentException("实体类型不能为空", nameof(request.EntityType));
            if (request.EntityIds == null || request.EntityIds.Count == 0)
                return;
            if (request.TagIds == null || request.TagIds.Count == 0)
                return;

            var entityType = request.EntityType.Trim().ToUpperInvariant();
            var tagIds = request.TagIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            var entityIds = request.EntityIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

            if (!tagIds.Any() || !entityIds.Any())
                return;

            var existingRelations = await _relationRepository.FindAsync(r =>
                r.EntityType == entityType &&
                entityIds.Contains(r.EntityId) &&
                tagIds.Contains(r.TagId));

            var existingSet = existingRelations
                .Select(r => (r.TagId, r.EntityId))
                .ToHashSet();

            foreach (var entityId in entityIds)
            {
                foreach (var tagId in tagIds)
                {
                    if (existingSet.Contains((tagId, entityId)))
                        continue;

                    var relation = new TagRelation
                    {
                        Id = Guid.NewGuid().ToString(),
                        TagId = tagId,
                        EntityType = entityType,
                        EntityId = entityId,
                        AppliedByUserId = request.AppliedByUserId,
                        AppliedTime = DateTime.UtcNow,
                        Source = request.Source,
                        CreateTime = DateTime.UtcNow,
                        CreateUserId = request.AppliedByUserId
                    };

                    await _relationRepository.AddAsync(relation);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // 更新用户偏好
            await UpdatePreferencesAsync(request.AppliedByUserId, tagIds);
        }

        public async Task RemoveTagsFromEntityAsync(RemoveTagsFromEntityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EntityType))
                throw new ArgumentException("实体类型不能为空", nameof(request.EntityType));
            if (request.EntityIds == null || request.EntityIds.Count == 0)
                return;
            if (request.TagIds == null || request.TagIds.Count == 0)
                return;

            var entityType = request.EntityType.Trim().ToUpperInvariant();
            var tagIds = request.TagIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            var entityIds = request.EntityIds.Where(id => !string.IsNullOrWhiteSpace(id)).ToList();

            var relations = await _relationRepository.FindAsync(r =>
                r.EntityType == entityType &&
                entityIds.Contains(r.EntityId) &&
                tagIds.Contains(r.TagId));

            foreach (var rel in relations)
            {
                await _relationRepository.DeleteAsync(rel.Id);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<TagDefinition>> GetTagsForEntityAsync(string entityType, string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
                return Array.Empty<TagDefinition>();

            var upperType = entityType.Trim().ToUpperInvariant();
            var relations = await _relationRepository.FindAsync(r =>
                r.EntityType == upperType &&
                r.EntityId == entityId);

            var tagIds = relations.Select(r => r.TagId).Distinct().ToList();
            if (!tagIds.Any())
                return Array.Empty<TagDefinition>();

            var tags = await _tagRepository.FindAsync(t => tagIds.Contains(t.Id) && t.Status == 1);
            return tags.OrderByDescending(t => t.SortOrder).ThenBy(t => t.Name).ToList();
        }

        public async Task<IReadOnlyList<TagDefinition>> GetUserCommonTagsAsync(long userId, string? entityType, int topN = 10)
        {
            if (userId <= 0)
                return Array.Empty<TagDefinition>();

            var prefs = await _preferenceRepository.FindAsync(p => p.UserId == userId);
            var orderedPrefs = prefs
                .OrderByDescending(p => p.IsFavorite)
                .ThenByDescending(p => p.UseCount)
                .ThenByDescending(p => p.LastUsedTime)
                .Take(topN)
                .ToList();

            if (!orderedPrefs.Any())
                return Array.Empty<TagDefinition>();

            var tagIds = orderedPrefs.Select(p => p.TagId).Distinct().ToList();
            var tags = await _tagRepository.FindAsync(t => tagIds.Contains(t.Id) && t.Status == 1);

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                var upperType = entityType.Trim().ToUpperInvariant();
                tags = tags.Where(t => t.Scope == null || t.Scope.ToUpper().Contains(upperType));
            }

            return tags.OrderByDescending(t => t.SortOrder).ThenBy(t => t.Name).ToList();
        }

        public async Task<IReadOnlyList<TagDefinition>> GetUserRecentTagsAsync(long userId, string? entityType, TimeSpan? range = null)
        {
            if (userId <= 0)
                return Array.Empty<TagDefinition>();

            var since = DateTime.UtcNow - (range ?? TimeSpan.FromDays(30));
            var prefs = await _preferenceRepository.FindAsync(p => p.UserId == userId && p.LastUsedTime >= since);
            var orderedPrefs = prefs
                .OrderByDescending(p => p.LastUsedTime)
                .Take(20)
                .ToList();

            if (!orderedPrefs.Any())
                return Array.Empty<TagDefinition>();

            var tagIds = orderedPrefs.Select(p => p.TagId).Distinct().ToList();
            var tags = await _tagRepository.FindAsync(t => tagIds.Contains(t.Id) && t.Status == 1);

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                var upperType = entityType.Trim().ToUpperInvariant();
                tags = tags.Where(t => t.Scope == null || t.Scope.ToUpper().Contains(upperType));
            }

            return tags.OrderByDescending(t => t.SortOrder).ThenBy(t => t.Name).ToList();
        }

        private async Task UpdatePreferencesAsync(long userId, List<string> tagIds)
        {
            if (userId <= 0 || tagIds.Count == 0)
                return;

            var prefs = await _preferenceRepository.FindAsync(p => p.UserId == userId && tagIds.Contains(p.TagId));
            var prefDict = prefs.ToDictionary(p => p.TagId, p => p);
            var now = DateTime.UtcNow;

            foreach (var tagId in tagIds)
            {
                if (prefDict.TryGetValue(tagId, out var pref))
                {
                    pref.UseCount += 1;
                    pref.LastUsedTime = now;
                    pref.ModifyTime = now;
                    await _preferenceRepository.UpdateAsync(pref);
                }
                else
                {
                    var newPref = new UserTagPreference
                    {
                        UserId = userId,
                        TagId = tagId,
                        UseCount = 1,
                        LastUsedTime = now,
                        IsFavorite = false,
                        CreateTime = now,
                        CreateUserId = userId
                    };
                    await _preferenceRepository.AddAsync(newPref);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}

