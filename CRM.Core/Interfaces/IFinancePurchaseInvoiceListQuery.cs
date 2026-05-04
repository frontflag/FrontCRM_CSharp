using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

/// <summary>进项发票列表：数据库侧 <c>Count</c> + <c>Skip</c>/<c>Take</c>（与 <see cref="FinancePurchaseInvoiceQueryRequest"/> 语义对齐）。</summary>
public interface IFinancePurchaseInvoiceListQuery
{
    Task<PagedResult<FinancePurchaseInvoice>> GetPagedAsync(
        FinancePurchaseInvoiceQueryRequest request,
        CancellationToken cancellationToken = default);
}
