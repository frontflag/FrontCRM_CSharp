using CRM.Core.Models.Vendor;

namespace CRM.Core.Interfaces;

/// <summary>供应商主表及回收站/黑名单/冻结列表：EF 数据库分页。</summary>
public interface IVendorListQuery
{
    Task<PagedResult<VendorInfo>> GetVendorsPagedAsync(VendorQueryRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<VendorInfo>> GetDeletedVendorsPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VendorInfo>> GetBlacklistVendorsPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VendorInfo>> GetFrozenVendorsPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default);
}
