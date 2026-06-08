using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public class FinanceReceivableQueryRequest
{
    public string? Keyword { get; set; }
    public string? CustomerId { get; set; }
    public short? VerificationStatus { get; set; }
    /// <summary>仅待核销（verified_to_be &gt; 0）</summary>
    public bool? OnlyOpen { get; set; }
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
}
