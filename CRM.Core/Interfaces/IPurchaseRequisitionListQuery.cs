using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces;

/// <summary>采购申请主列表：EF <c>Count</c> + <c>Skip</c>/<c>Take</c>，与《翻页查询规范》一致。</summary>
public interface IPurchaseRequisitionListQuery
{
    Task<PagedResult<PurchaseRequisitionListPageRow>> GetPagedAsync(
        PurchaseRequisitionListQueryRequest request,
        CancellationToken cancellationToken = default);
}
