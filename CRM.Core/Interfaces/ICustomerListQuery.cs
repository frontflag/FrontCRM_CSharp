using CRM.Core.Models.Customer;

namespace CRM.Core.Interfaces;

/// <summary>客户主表及回收站/黑名单/冻结列表：EF 数据库分页（与内存全表再 Skip 方案分离）。</summary>
public interface ICustomerListQuery
{
    Task<PagedResult<CustomerInfo>> GetCustomersPagedAsync(CustomerQueryRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerInfo>> GetDeletedCustomersPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerInfo>> GetBlackListCustomersPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CustomerInfo>> GetFrozenCustomersPagedAsync(
        int page,
        int pageSize,
        string? keyword,
        string? currentUserId,
        CancellationToken cancellationToken = default);
}
