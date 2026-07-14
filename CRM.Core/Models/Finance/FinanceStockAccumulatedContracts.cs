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

public sealed class FinanceStockAccumulatedItemRowDto
{
    public string StockInItemId { get; set; } = string.Empty;
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
