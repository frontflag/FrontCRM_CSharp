using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public interface IFinanceReceivableStatementQuery
{
    Task<PagedResult<FinanceReceivableStatementListItem>> GetPagedAsync(
        FinanceReceivableStatementListQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 客户不存在，或当前用户对该客户下任何应收均不可见时返回 null（调用方 404）。
    /// 该币别无应收时仍返回空对账单（仅期初行）。
    /// </summary>
    Task<FinanceReceivableStatementDetailDto?> GetDetailAsync(
        string customerId,
        short currency,
        DateOnly periodFrom,
        DateOnly periodTo,
        DateOnly agingCutoff,
        string? currentUserId,
        CancellationToken cancellationToken = default);
}
