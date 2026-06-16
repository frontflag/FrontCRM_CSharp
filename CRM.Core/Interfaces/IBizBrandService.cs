using CRM.Core.Models.Dtos;

namespace CRM.Core.Interfaces;

public interface IBizBrandService
{
    Task<BizBrandPagedDto> ListAsync(BizBrandQuery query, CancellationToken cancellationToken = default);
    Task<List<BizBrandOptionDto>> ListOptionsAsync(BizBrandOptionsQuery query, CancellationToken cancellationToken = default);
    Task<BizBrandRowDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<BizBrandRowDto> CreateAsync(UpsertBizBrandRequest request, string? actingUserId, CancellationToken cancellationToken = default);
    Task<BizBrandRowDto> UpdateAsync(long id, UpsertBizBrandRequest request, CancellationToken cancellationToken = default);
    Task<BizBrandRowDto> ApproveAsync(long id, string? actingUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, string? actingUserId, CancellationToken cancellationToken = default);
}
