using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

/// <summary>付款单列表：数据库侧 <c>Count</c> + <c>Skip</c>/<c>Take</c>（与 <see cref="FinancePaymentQueryRequest"/> 语义对齐）。</summary>
public interface IFinancePaymentListQuery
{
    Task<PagedResult<FinancePayment>> GetPagedAsync(
        FinancePaymentQueryRequest request,
        CancellationToken cancellationToken = default);
}
