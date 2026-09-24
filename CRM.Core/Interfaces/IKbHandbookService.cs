using CRM.Core.Knowledge;

namespace CRM.Core.Interfaces;

public interface IKbHandbookService
{
    Task<KbImportResultDto> EnqueueDocxAsync(
        string storagePath,
        string fileName,
        string documentCode,
        string title,
        CancellationToken cancellationToken = default);

    Task ProcessPendingAsync(CancellationToken cancellationToken = default);

    Task<KbAskResultDto> AskAsync(string userId, string question, CancellationToken cancellationToken = default);

    Task<KbHandbookReaderDto> GetReaderAsync(
        string userId,
        string? versionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KbDocumentVersionDto>> ListVersionsAsync(
        string documentCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KbChunkListItemDto>> ListChunksAsync(
        string versionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task ActivateAsync(string versionId, CancellationToken cancellationToken = default);

    Task EnsurePermissionAsync(string? userId, string permissionCode, CancellationToken cancellationToken = default);
}
