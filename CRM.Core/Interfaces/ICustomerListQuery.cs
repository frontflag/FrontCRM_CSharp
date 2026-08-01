using CRM.Core.Models.Analytics;
using CRM.Core.Models.Customer;

namespace CRM.Core.Interfaces;

/// <summary>客户主表及回收站/黑名单/冻结列表：EF 数据库分页（与内存全表再 Skip 方案分离）。</summary>
public interface ICustomerListQuery
{
    Task<PagedResult<CustomerInfo>> GetCustomersPagedAsync(CustomerQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 客户列表看板：与列表同源 listFilter；在筛选客户集合内统计全部已审核销售订单（不限订单日期）。
    /// </summary>
    Task<SalesAnalyticsCustomerDto> GetListAnalyticsCustomerAsync(
        CustomerQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

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
