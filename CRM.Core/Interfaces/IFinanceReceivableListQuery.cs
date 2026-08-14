using CRM.Core.Models.Finance;
using CRM.Core.Models.Analytics;

namespace CRM.Core.Interfaces;

public class FinanceReceivableQueryRequest
{
    public string? Keyword { get; set; }
    public string? CustomerId { get; set; }
    public short? VerificationStatus { get; set; }
    /// <summary>收款待核销（verified_to_be &gt; 0）。与 VerificationStatus 互斥由调用方保证。</summary>
    public bool? OnlyOpen { get; set; }
    /// <summary>发票核销（开票匹配）状态 0未核销 1部分 2完成</summary>
    public short? InvoiceMatchStatus { get; set; }
    /// <summary>发票待核销（invoice_match_to_be &gt; 0）</summary>
    public bool? InvoiceMatchOnlyOpen { get; set; }
    public DateTime? StockOutDateFrom { get; set; }
    public DateTime? StockOutDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? CurrentUserId { get; set; }
}

public interface IFinanceReceivableListQuery
{
    Task<PagedResult<FinanceReceivable>> GetPagedAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<FinanceReceivable?> GetByIdScopedAsync(
        string id,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceReceivableWriteOffLedgerItem>> GetWriteOffLedgerPagedAsync(
        FinanceReceivableWriteOffLedgerQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<FinanceReceivableListAnalyticsDashboardDto> GetListAnalyticsDashboardAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReceivableListAnalyticsTrendPointDto>> GetListAnalyticsTrendsAsync(
        FinanceReceivableQueryRequest request,
        string groupBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReceivableListAnalyticsBreakdownGroupDto>> GetListAnalyticsBreakdownsAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<FinanceReceivableListAnalyticsRankingsDto> GetListAnalyticsRankingsAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default);
}
