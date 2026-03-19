using CRM.Core.Interfaces;
using CRM.Core.Models.Tag;

namespace CRM.Core.Services
{
    /// <summary>
    /// 标签管理服务实现
    /// </summary>
    public class TagService : ITagService
    {
        private readonly IRepository<TagDefinition> _tagRepository;
        private readonly IUnitOfWork _unitOfWork;

        public TagService(IRepository<TagDefinition> tagRepository, IUnitOfWork unitOfWork)
        {
            _tagRepository = tagRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResult<TagDefinition>> SearchTagsAsync(TagSearchRequest request)
        {
            var all = await _tagRepository.GetAllAsync();
            var query = all.AsQueryable();

            if (request.Type.HasValue)
            {
                query = query.Where(t => t.Type == request.Type.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                var category = request.Category.Trim();
                query = query.Where(t => t.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(request.EntityType))
            {
                var entityType = request.EntityType.Trim().ToUpperInvariant();
                query = query.Where(t => t.Scope != null && t.Scope.ToUpper().Contains(entityType));
            }

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim().ToLowerInvariant();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(keyword) ||
                    (t.Code != null && t.Code.ToLower().Contains(keyword)));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status.Value);
            }

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<TagDefinition>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        public async Task<TagDefinition> CreateTagAsync(CreateTagRequest request, long currentUserId)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("标签名称不能为空", nameof(request.Name));

            var tag = new TagDefinition
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name.Trim(),
                Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
                Color = request.Color?.Trim(),
                Icon = request.Icon?.Trim(),
                Type = request.Type,
                Category = request.Category?.Trim(),
                Scope = request.Scope?.Trim(),
                Status = 1,
                SortOrder = 0,
                UsageCount = 0,
                OwnerUserId = request.Type == 1 ? null : currentUserId,
                Visibility = request.Visibility,
                CreateTime = DateTime.UtcNow,
                CreateUserId = currentUserId
            };

            await _tagRepository.AddAsync(tag);
            await _unitOfWork.SaveChangesAsync();
            return tag;
        }

        public async Task<TagDefinition> UpdateTagAsync(string id, UpdateTagRequest request, long currentUserId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("标签ID不能为空", nameof(id));

            var tags = await _tagRepository.FindAsync(t => t.Id == id);
            var tag = tags.FirstOrDefault();
            if (tag == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的标签");

            if (request.Name != null)
                tag.Name = request.Name.Trim();
            if (request.Color != null)
                tag.Color = request.Color.Trim();
            if (request.Icon != null)
                tag.Icon = request.Icon.Trim();
            if (request.Category != null)
                tag.Category = request.Category.Trim();
            if (request.Scope != null)
                tag.Scope = request.Scope.Trim();
            if (request.Visibility.HasValue)
                tag.Visibility = request.Visibility.Value;
            if (request.Status.HasValue)
                tag.Status = request.Status.Value;

            tag.ModifyTime = DateTime.UtcNow;
            tag.ModifyUserId = currentUserId;

            await _tagRepository.UpdateAsync(tag);
            await _unitOfWork.SaveChangesAsync();

            return tag;
        }

        public async Task DisableTagAsync(string id, long currentUserId)
        {
            await SetStatusAsync(id, 0, currentUserId);
        }

        public async Task EnableTagAsync(string id, long currentUserId)
        {
            await SetStatusAsync(id, 1, currentUserId);
        }

        private async Task SetStatusAsync(string id, short status, long currentUserId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("标签ID不能为空", nameof(id));

            var tags = await _tagRepository.FindAsync(t => t.Id == id);
            var tag = tags.FirstOrDefault();
            if (tag == null)
                throw new KeyNotFoundException($"找不到ID为 '{id}' 的标签");

            tag.Status = status;
            tag.ModifyTime = DateTime.UtcNow;
            tag.ModifyUserId = currentUserId;
            await _tagRepository.UpdateAsync(tag);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteTagAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("标签ID不能为空", nameof(id));

            await _tagRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<TagDefinition?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var tags = await _tagRepository.FindAsync(t => t.Id == id);
            return tags.FirstOrDefault();
        }
    }
}

