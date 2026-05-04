using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

/// <summary>收款单主表列表：数据库侧 <c>Count</c> + <c>Skip</c>/<c>Take</c>（与 <see cref="FinanceReceiptQueryRequest"/> 语义对齐）。</summary>
public interface IFinanceReceiptListQuery
{
    Task<PagedResult<FinanceReceipt>> GetPagedAsync(
        FinanceReceiptQueryRequest request,
        CancellationToken cancellationToken = default);
}
