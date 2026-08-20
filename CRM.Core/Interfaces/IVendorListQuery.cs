using CRM.Core.Models.Analytics;
using CRM.Core.Models.Vendor;

namespace CRM.Core.Interfaces;

/// <summary>供应商主表及回收站/黑名单/冻结列表：EF 数据库分页。</summary>
public interface IVendorListQuery
{
    Task<PagedResult<VendorInfo>> GetVendorsPagedAsync(VendorQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 供应商列表看板：与列表同源 listFilter；在筛选供应商集合内统计全部已审核采购订单（不限订单日期）。
    /// </summary>
    Task<PurchaseAnalyticsVendorDto> GetListAnalyticsVendorAsync(
        VendorQueryRequest request,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// 查重候选：忽略软删过滤器，不套采购列表数据范围，只取比对所需列。
    /// </summary>
    Task<IReadOnlyList<VendorDuplicateCheckRow>> GetDuplicateCheckRowsAsync(
        CancellationToken cancellationToken = default);
}
