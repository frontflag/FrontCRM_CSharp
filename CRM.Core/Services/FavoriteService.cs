using CRM.Core.Interfaces;
using CRM.Core.Models.Favorite;

namespace CRM.Core.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IRepository<UserFavorite> _favoriteRepository;
        private readonly IUnitOfWork _unitOfWork;

        public FavoriteService(IRepository<UserFavorite> favoriteRepository, IUnitOfWork unitOfWork)
        {
            _favoriteRepository = favoriteRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task AddFavoriteAsync(long userId, string entityType, string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("实体类型不能为空", nameof(entityType));
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentException("实体ID不能为空", nameof(entityId));

            var upperEntityType = entityType.Trim().ToUpperInvariant();
            var normalizedEntityId = entityId.Trim();

            var exists = await _favoriteRepository.FindAsync(f =>
                f.UserId == userId &&
                f.EntityType == upperEntityType &&
                f.EntityId == normalizedEntityId);

            if (exists.Any())
                return;

            await _favoriteRepository.AddAsync(new UserFavorite
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                EntityType = upperEntityType,
                EntityId = normalizedEntityId,
                CreateUserId = userId,
                CreateTime = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RemoveFavoriteAsync(long userId, string entityType, string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
                return;

            var upperEntityType = entityType.Trim().ToUpperInvariant();
            var normalizedEntityId = entityId.Trim();

            var exists = await _favoriteRepository.FindAsync(f =>
                f.UserId == userId &&
                f.EntityType == upperEntityType &&
                f.EntityId == normalizedEntityId);

            foreach (var favorite in exists)
            {
                await _favoriteRepository.DeleteAsync(favorite.Id);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<string>> GetFavoriteEntityIdsAsync(long userId, string entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return Array.Empty<string>();

            var upperEntityType = entityType.Trim().ToUpperInvariant();
            var list = await _favoriteRepository.FindAsync(f => f.UserId == userId && f.EntityType == upperEntityType);
            return list.Select(f => f.EntityId).Distinct().ToList();
        }

        public async Task<bool> IsFavoriteAsync(long userId, string entityType, string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
                return false;

            var upperEntityType = entityType.Trim().ToUpperInvariant();
            var normalizedEntityId = entityId.Trim();
            var exists = await _favoriteRepository.FindAsync(f =>
                f.UserId == userId &&
                f.EntityType == upperEntityType &&
                f.EntityId == normalizedEntityId);
            return exists.Any();
        }
    }
}
