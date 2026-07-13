using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.RFQ;

namespace CRM.Infrastructure.RfqListQueries;

/// <summary>需求明细列表 join 行（列表分页与看板共用）。</summary>
internal sealed class RfqItemListJoin
{
    public RFQItem Item { get; init; } = null!;
    public RFQ Rfq { get; init; } = null!;
    public CustomerInfo? Customer { get; init; }
    public User? SalesUser { get; init; }
}
