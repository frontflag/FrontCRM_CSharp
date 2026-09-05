using CRM.Core.Models.Quote;

namespace CRM.Core.Interfaces;

public interface ICustomerQuoteService
{
    Task<(IReadOnlyList<CustomerQuoteDraft> Items, int Total)> GetDraftsPagedAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CustomerQuoteDraft> AddDraftFromQuoteItemAsync(
        string userId,
        string quoteItemId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerQuoteDraft>> AddDraftsFromQuoteAsync(
        string userId,
        string quoteId,
        CancellationToken cancellationToken = default);

    Task DeleteDraftAsync(string userId, string draftId, CancellationToken cancellationToken = default);

    Task<CustomerQuote> GenerateFromDraftsAsync(
        string userId,
        IReadOnlyList<string> draftIds,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerQuote> Items, int Total)> GetQuotesPagedAsync(
        string? userId,
        int page,
        int pageSize,
        short? status,
        string? keyword,
        CancellationToken cancellationToken = default);

    Task<CustomerQuote?> GetQuoteByIdAsync(string? userId, string id, CancellationToken cancellationToken = default);

    Task<CustomerQuote> UpdateQuoteAsync(
        string userId,
        string id,
        UpdateCustomerQuoteRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerQuote> ApplyProfitFactorAsync(
        string userId,
        string id,
        CancellationToken cancellationToken = default);
}

public class UpdateCustomerQuoteRequest
{
    public string? CustomerContactId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public decimal? ProfitFactor { get; set; }
    public List<UpdateCustomerQuoteItemRequest>? Items { get; set; }
}

public class UpdateCustomerQuoteItemRequest
{
    public string Id { get; set; } = string.Empty;
    public decimal? SendPrice { get; set; }
    public short? SendCurrency { get; set; }
    public bool? IsLocked { get; set; }
    public string? LeadTime { get; set; }
    public string? DateCode { get; set; }
    public string? Remark { get; set; }
}
