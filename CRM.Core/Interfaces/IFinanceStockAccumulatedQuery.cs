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

    Task<FinanceVendorAccumulatedListDto> GetVendorPageAsync(
        FinanceVendorAccumulatedQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceStockAccumulatedItemRowDto>> GetVendorItemPageAsync(
        FinanceVendorAccumulatedItemQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<FinanceCustomerAccumulatedListDto> GetCustomerPageAsync(
        FinanceCustomerAccumulatedQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceStockAccumulatedItemRowDto>> GetCustomerItemPageAsync(
        FinanceCustomerAccumulatedItemQueryRequest request,
        int page,
        int pageSize,
        bool maskAmounts,
        CancellationToken cancellationToken = default);
}
