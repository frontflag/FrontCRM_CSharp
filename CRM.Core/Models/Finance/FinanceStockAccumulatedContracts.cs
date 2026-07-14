namespace CRM.Core.Models.Finance;

public sealed class FinanceStockAccumulatedSearchOptionsDto
{
    public IReadOnlyList<string> Years { get; set; } = Array.Empty<string>();
}

public sealed class FinanceStockAccumulatedListDto
{
    public string Year { get; set; } = string.Empty;
    public bool MaskAmounts { get; set; }
    public IReadOnlyList<FinanceStockAccumulatedMonthRowDto> Items { get; set; } = Array.Empty<FinanceStockAccumulatedMonthRowDto>();
}

public sealed class FinanceStockAccumulatedMonthRowDto
{
    public string YearMonth { get; set; } = string.Empty;
    public decimal? PrvAmountTotal { get; set; }
    public decimal? CurrentStockInAmountTotal { get; set; }
    public decimal? CurrentStockOutAmountTotal { get; set; }
    public decimal? BalanceAmountTotal { get; set; }
    public int PrvStockQty { get; set; }
    public int StockInQty { get; set; }
    public int StockOutQty { get; set; }
    public int BalanceStockQty { get; set; }
}

public sealed class FinanceStockAccumulatedItemQueryRequest
{
    public string? Month { get; set; }
    public string? QueryKeywords { get; set; }
    public string? Pn { get; set; }
    public string? StockInCode { get; set; }
    public DateTime? StockInTimeStart { get; set; }
    public DateTime? StockInTimeEnd { get; set; }
}

public sealed class FinanceVendorAccumulatedQueryRequest
{
    public string? Month { get; set; }
    public string? QueryKeywords { get; set; }
}

public sealed class FinanceVendorAccumulatedItemQueryRequest
{
    public string? Month { get; set; }
    public string? VendorId { get; set; }
    public string? QueryKeywords { get; set; }
    public string? Pn { get; set; }
    public string? StockInCode { get; set; }
    public DateTime? StockInTimeStart { get; set; }
    public DateTime? StockInTimeEnd { get; set; }
}

public sealed class FinanceVendorAccumulatedRowDto
{
    public string VendorId { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public decimal? PrvAmountTotal { get; set; }
    public decimal? CurrentStockInAmountTotal { get; set; }
    public decimal? CurrentStockOutAmountTotal { get; set; }
    public decimal? BalanceAmountTotal { get; set; }
    public int PrvStockQty { get; set; }
    public int StockInQty { get; set; }
    public int StockOutQty { get; set; }
    public int BalanceStockQty { get; set; }
}

public sealed class FinanceVendorAccumulatedListDto
{
    public string Month { get; set; } = string.Empty;
    public bool MaskAmounts { get; set; }
    public IReadOnlyList<FinanceVendorAccumulatedRowDto> Items { get; set; } = Array.Empty<FinanceVendorAccumulatedRowDto>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

public sealed class FinanceCustomerAccumulatedQueryRequest
{
    public string? Month { get; set; }
    public string? QueryKeywords { get; set; }
}

public sealed class FinanceCustomerAccumulatedItemQueryRequest
{
    public string? Month { get; set; }
    public string? CustomerId { get; set; }
    public string? QueryKeywords { get; set; }
    public string? Pn { get; set; }
    public string? StockInCode { get; set; }
    public DateTime? StockInTimeStart { get; set; }
    public DateTime? StockInTimeEnd { get; set; }
}

public sealed class FinanceCustomerAccumulatedRowDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal? PrvAmountTotal { get; set; }
    public decimal? CurrentStockInAmountTotal { get; set; }
    public decimal? CurrentStockOutAmountTotal { get; set; }
    public decimal? BalanceAmountTotal { get; set; }
    public int PrvStockQty { get; set; }
    public int StockInQty { get; set; }
    public int StockOutQty { get; set; }
    public int BalanceStockQty { get; set; }
}

public sealed class FinanceCustomerAccumulatedListDto
{
    public string Month { get; set; } = string.Empty;
    public bool MaskAmounts { get; set; }
    public IReadOnlyList<FinanceCustomerAccumulatedRowDto> Items { get; set; } = Array.Empty<FinanceCustomerAccumulatedRowDto>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

public sealed class FinanceStockAccumulatedItemRowDto
{
    public string StockInItemId { get; set; } = string.Empty;
    public string StockInId { get; set; } = string.Empty;
    public string BillCode { get; set; } = string.Empty;
    public string? Pn { get; set; }
    public DateTime StockInTime { get; set; }
    public int StockInQty { get; set; }
    public int StockOutQty { get; set; }
    public int PrvQty { get; set; }
    public int BalanceQty { get; set; }
    public decimal? PrvAmountTotal { get; set; }
    public decimal? CurrentStockInAmountTotal { get; set; }
    public decimal? CurrentStockOutAmountTotal { get; set; }
    public decimal? BalanceAmountTotal { get; set; }
}
