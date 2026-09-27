using CRM.Core.Models.Ai;

namespace CRM.Core.Interfaces;

public interface IAiDataQueryService
{
    Task<AiDataQueryResponse> AskAsync(
        string userId,
        string question,
        AiDataQueryState? state,
        CancellationToken cancellationToken = default);
}
