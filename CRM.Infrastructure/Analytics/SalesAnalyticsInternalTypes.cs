using CRM.Core.Models.RFQ;

namespace CRM.Infrastructure.Analytics;

internal sealed class RfqItemJoinRow
{
    public RFQItem Item { get; init; } = null!;
    public RFQ Rfq { get; init; } = null!;
}
