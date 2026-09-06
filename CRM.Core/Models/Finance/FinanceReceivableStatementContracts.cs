namespace CRM.Core.Models.Finance;

public static class FinanceReceivableStatementLineTypes
{
    public const string Opening = "opening";
    public const string Increase = "increase";
    public const string Receipt = "receipt";
}

public sealed class FinanceReceivableStatementListQueryRequest
{
    public string? Keyword { get; set; }
    public bool OnlyOpen { get; set; }
    public short? Currency { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? CurrentUserId { get; set; }
}

public sealed class FinanceReceivableStatementListItem
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public short Currency { get; set; }
    public int ReceivableCount { get; set; }
    public decimal AmountTotal { get; set; }
    public decimal VerifiedDone { get; set; }
    public decimal VerifiedToBe { get; set; }
    public DateTime? LatestStockOutDate { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
}

public sealed class FinanceReceivableStatementCustomerDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public short? PaymentDays { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
    public bool CanViewFull { get; set; }
}

public sealed class FinanceReceivableStatementHeaderDto
{
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly GeneratedOn { get; set; }
    public DateOnly AgingCutoff { get; set; }
    public short Currency { get; set; }
    public decimal Opening { get; set; }
    public decimal PeriodIncrease { get; set; }
    public decimal PeriodReceived { get; set; }
    public decimal Ending { get; set; }
}

public sealed class FinanceReceivableStatementLineDto
{
    public string LineType { get; set; } = FinanceReceivableStatementLineTypes.Opening;
    public DateOnly Date { get; set; }
    public string? DocNo { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal? IncreaseAmount { get; set; }
    public decimal? ReceivedAmount { get; set; }
    public decimal Balance { get; set; }
    public string? ReceivableId { get; set; }
    public string? StockOutId { get; set; }
    public string? ReceiptId { get; set; }
    public string? WriteOffId { get; set; }
}

public sealed class FinanceReceivableStatementDetailDto
{
    public FinanceReceivableStatementCustomerDto Customer { get; set; } = new();
    public FinanceReceivableStatementHeaderDto Statement { get; set; } = new();
    public IReadOnlyList<FinanceReceivableStatementLineDto> Lines { get; set; } =
        Array.Empty<FinanceReceivableStatementLineDto>();
}

public sealed class FinanceReceivableStatementEvent
{
    public required string LineType { get; init; }
    public required DateOnly BusinessDate { get; init; }
    public required decimal Amount { get; init; }
    public string? DocNo { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string? ReceivableId { get; init; }
    public string? StockOutId { get; init; }
    public string? ReceiptId { get; init; }
    public string? WriteOffId { get; init; }
}
