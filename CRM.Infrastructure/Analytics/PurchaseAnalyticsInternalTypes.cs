using CRM.Core.Models.Purchase;
using CRM.Core.Models.Quote;

namespace CRM.Infrastructure.Analytics;

internal sealed class QuoteItemJoinRow
{
    public QuoteItem Item { get; init; } = null!;
    public Quote Quote { get; init; } = null!;
}
