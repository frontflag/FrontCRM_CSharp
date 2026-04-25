using CRM.Core.Models.Dtos;

namespace CRM.Core.Interfaces;

public interface ILoginLogQueryService
{
    Task<LoginLogPagedResult> QueryAsync(LoginLogQuery query, CancellationToken cancellationToken = default);
}
