namespace CRM.Core.Interfaces
{
    public interface IFavoriteService
    {
        Task AddFavoriteAsync(long userId, string entityType, string entityId);
        Task RemoveFavoriteAsync(long userId, string entityType, string entityId);
        Task<IReadOnlyList<string>> GetFavoriteEntityIdsAsync(long userId, string entityType);
        Task<bool> IsFavoriteAsync(long userId, string entityType, string entityId);
    }
}
