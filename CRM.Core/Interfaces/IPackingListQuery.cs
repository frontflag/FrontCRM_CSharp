namespace CRM.Core.Interfaces;

public interface IPackingListQuery
{
    Task<PagedResult<string>> GetPagedPackingIdsAsync(
        PackingListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<string>> GetPagedPackingItemIdsAsync(
        string? keyword,
        string? packingCode,
        int page,
        int pageSize,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);
}
