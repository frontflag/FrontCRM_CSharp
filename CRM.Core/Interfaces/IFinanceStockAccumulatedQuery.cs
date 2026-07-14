using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public interface IFinanceStockAccumulatedQuery
{
    Task<FinanceStockAccumulatedSearchOptionsDto> GetSearchOptionsAsync(CancellationToken cancellationToken = default);

    Task<FinanceStockAccumulatedListDto> GetStockSummaryAsync(
        int year,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceStockAccumulatedItemRowDto>> GetStockItemPageAsync(
        FinanceStockAccumulatedItemQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
