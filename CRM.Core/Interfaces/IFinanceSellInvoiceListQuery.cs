using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

/// <summary>销项发票主表列表：数据库侧 <c>Count</c> + <c>Skip</c>/<c>Take</c>（与 <see cref="FinanceSellInvoiceQueryRequest"/> 语义对齐）。</summary>
public interface IFinanceSellInvoiceListQuery
{
    Task<PagedResult<FinanceSellInvoice>> GetPagedAsync(
        FinanceSellInvoiceQueryRequest request,
        CancellationToken cancellationToken = default);
}
