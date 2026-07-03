using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Core.Models.Sales;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services;

/// <inheritdoc />
public sealed class QuoteStatusSyncService : IQuoteStatusSyncService
{
    private readonly IRepository<Quote> _quoteRepo;
    private readonly IRepository<SellOrder> _soRepo;
    private readonly IRepository<SellOrderItem> _soItemRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuoteStatusSyncService> _logger;

    public QuoteStatusSyncService(
        IRepository<Quote> quoteRepo,
        IRepository<SellOrder> soRepo,
        IRepository<SellOrderItem> soItemRepo,
        IUnitOfWork unitOfWork,
        ILogger<QuoteStatusSyncService> logger)
    {
        _quoteRepo = quoteRepo;
        _soRepo = soRepo;
        _soItemRepo = soItemRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task MarkQuotesWonAsync(IEnumerable<string?> quoteIds, CancellationToken cancellationToken = default)
    {
        var ids = NormalizeQuoteIds(quoteIds);
        if (ids.Count == 0) return;

        var quotes = (await _quoteRepo.FindAsync(q => ids.Contains(q.Id))).ToList();
        var changed = false;
        foreach (var quote in quotes)
        {
            if (quote.Status == (short)QuoteMainStatus.Closed) continue;
            if (quote.Status == (short)QuoteMainStatus.Won) continue;
            quote.Status = (short)QuoteMainStatus.Won;
            quote.ModifyTime = DateTime.UtcNow;
            await _quoteRepo.UpdateAsync(quote);
            changed = true;
            _logger.LogInformation(
                "报价状态→成单 QuoteId={QuoteId} QuoteCode={QuoteCode}",
                quote.Id,
                quote.QuoteCode);
        }

        if (changed)
            await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ReconcileQuotesAfterSalesOrderChangeAsync(
        IEnumerable<string?> quoteIds,
        CancellationToken cancellationToken = default)
    {
        var ids = NormalizeQuoteIds(quoteIds);
        if (ids.Count == 0) return;

        var changed = false;
        foreach (var id in ids)
        {
            var quote = await _quoteRepo.GetByIdAsync(id);
            if (quote == null) continue;
            if (quote.Status == (short)QuoteMainStatus.Closed) continue;

            var hasActiveRef = await HasActiveSalesOrderReferenceAsync(id, cancellationToken);
            var target = hasActiveRef ? QuoteMainStatus.Won : QuoteMainStatus.New;
            if (quote.Status == (short)target) continue;

            var prev = quote.Status;
            quote.Status = (short)target;
            quote.ModifyTime = DateTime.UtcNow;
            await _quoteRepo.UpdateAsync(quote);
            changed = true;
            _logger.LogInformation(
                "报价状态回写 QuoteId={QuoteId} {PrevStatus}→{NewStatus}",
                quote.Id,
                prev,
                quote.Status);
        }

        if (changed)
            await _unitOfWork.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task CloseNewQuotesForRfqAsync(string rfqId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rfqId)) return;
        var rid = rfqId.Trim();

        var quotes = (await _quoteRepo.FindAsync(q =>
                q.RFQId != null &&
                q.RFQId == rid &&
                q.Status == (short)QuoteMainStatus.New))
            .ToList();
        if (quotes.Count == 0) return;

        foreach (var quote in quotes)
        {
            quote.Status = (short)QuoteMainStatus.Closed;
            quote.ModifyTime = DateTime.UtcNow;
            await _quoteRepo.UpdateAsync(quote);
            _logger.LogInformation(
                "需求关闭：报价状态→关闭 QuoteId={QuoteId} RfqId={RfqId}",
                quote.Id,
                rid);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<bool> HasActiveSalesOrderReferenceAsync(string quoteId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var qid = quoteId.Trim();
        var lines = (await _soItemRepo.FindAsync(si =>
                si.QuoteId != null &&
                si.QuoteId == qid &&
                !si.IsDeleted))
            .ToList();
        if (lines.Count == 0) return false;

        var orderIds = lines
            .Select(l => l.SellOrderId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (orderIds.Count == 0) return false;

        var orders = (await _soRepo.FindAsync(o => orderIds.Contains(o.Id) && !o.IsDeleted)).ToList();
        return orders.Any(o => o.Status != SellOrderMainStatus.Cancelled);
    }

    private static List<string> NormalizeQuoteIds(IEnumerable<string?> quoteIds)
    {
        if (quoteIds == null) return new List<string>();
        return quoteIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
