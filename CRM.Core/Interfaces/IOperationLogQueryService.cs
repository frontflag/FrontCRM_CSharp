using CRM.Core.Models.Dtos;

namespace CRM.Core.Interfaces;

public interface IOperationLogQueryService
{
    Task<OperationLogPagedResult> QueryAsync(OperationLogQuery query, CancellationToken cancellationToken = default);
}
