using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces;

public class FinanceReceivableWriteOffAllocation
{
    public string FinanceReceiptItemId { get; set; } = string.Empty;
    public string FinanceReceivableId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class FinanceReceivableWriteOffRequest
{
    public List<FinanceReceivableWriteOffAllocation> Allocations { get; set; } = new();
    public List<FinanceAdvancePoolAllocation> AdvancePoolAllocations { get; set; } = new();
    public bool ConfirmSoMismatch { get; set; }
}

public class FinanceReceiptItemWriteOffCandidate
{
    public FinanceReceiptItem Item { get; set; } = null!;
    public string FinanceReceiptCode { get; set; } = string.Empty;
    public short ReceiptStatus { get; set; }
    public decimal RemainingAmount { get; set; }
    public short ReceiptPurpose { get; set; }
    public string? AdvanceSellOrderId { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public decimal ReceiptAmount { get; set; }
    public byte ReceiptCurrency { get; set; } = 1;
    public short ReceiptMode { get; set; } = 1;
    public string? Remark { get; set; }
}

public class FinanceReceivableListItem
{
    public string Id { get; set; } = string.Empty;
    public string? ReceivableCode { get; set; }
    public string StockOutId { get; set; } = string.Empty;
    public string StockOutCode { get; set; } = string.Empty;
    public string SellOrderId { get; set; } = string.Empty;
    public string? SellOrderCode { get; set; }
    public string SellOrderItemId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public string? CustomerCode { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
    public string? PN { get; set; }
    public string? Brand { get; set; }
    public decimal OutboundQty { get; set; }
    public decimal UnitPrice { get; set; }
    public short Currency { get; set; } = 1;
    public decimal Amount { get; set; }
    public decimal VerifiedDone { get; set; }
    public decimal VerifiedToBe { get; set; }
    public short VerificationStatus { get; set; }
    public DateTime? StockOutDate { get; set; }
    public DateTime? CreateTime { get; set; }
}

public class FinanceReceivableWriteOffCandidateRow
{
    public string Id { get; set; } = string.Empty;
    public string? ReceivableCode { get; set; }
    public string StockOutId { get; set; } = string.Empty;
    public string StockOutCode { get; set; } = string.Empty;
    public string SellOrderId { get; set; } = string.Empty;
    public string? SellOrderCode { get; set; }
    public string SellOrderItemId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
    public string? PN { get; set; }
    public string? Brand { get; set; }
    public decimal OutboundQty { get; set; }
    public decimal UnitPrice { get; set; }
    public short Currency { get; set; } = 1;
    public decimal Amount { get; set; }
    public decimal VerifiedDone { get; set; }
    public decimal VerifiedToBe { get; set; }
    public short VerificationStatus { get; set; }
    public DateTime? StockOutDate { get; set; }
    public string? FreightForwarderOrderNo { get; set; }
    public string? StockInCode { get; set; }
}

public class FinanceReceivableWriteOffCandidates
{
    public List<FinanceReceiptItemWriteOffCandidate> ReceiptItems { get; set; } = new();
    public List<FinanceReceivableWriteOffCandidateRow> Receivables { get; set; } = new();
    public List<FinanceCustomerAdvanceBalanceDto> AdvanceBalances { get; set; } = new();
}

public class FinanceWriteOffCustomerCurrencyTotal
{
    public short Currency { get; set; }
    public decimal Amount { get; set; }
}

public class FinanceWriteOffCustomerSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public string? CustomerCode { get; set; }
    public string? SalesUserId { get; set; }
    public string? SalesUserName { get; set; }
    /// <summary>待核销收款剩余金额合计（不含应收待核销）。</summary>
    public decimal PendingWriteOffTotal { get; set; }
    public short? Currency { get; set; }
    public bool IsMultiCurrency { get; set; }
    public List<FinanceWriteOffCustomerCurrencyTotal> CurrencyTotals { get; set; } = new();
    /// <summary>待核销收款明细条数。</summary>
    public int PendingReceiptItemCount { get; set; }
    public DateTime? EarliestReceiptDate { get; set; }
    public DateTime? LatestReceiptDate { get; set; }
    /// <summary>该客户该币别是否存在未清应收（verified_to_be &gt; 0，与右栏口径一致）。</summary>
    public bool HasOpenReceivable { get; set; }
}

public interface IFinanceReceivableService
{
    /// <summary>销售出库进入「出库完成」(4) 时确保生成对应应收款；已存在则仅同步出库日期。</summary>
    Task TryEnsureFromStockOutAsync(string stockOutId, string? actingUserId = null, CancellationToken cancellationToken = default);

    /// <summary>出库单离开「出库完成」(4) 时软删对应应收。</summary>
    Task TrySoftDeleteForStockOutAsync(string stockOutId, string? actingUserId = null, CancellationToken cancellationToken = default);

    void AssertStockOutCanVoid(FinanceReceivable? receivable);

    Task<PagedResult<FinanceReceivable>> GetPagedAsync(FinanceReceivableQueryRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceReceivableListItem>> GetPagedListAsync(
        FinanceReceivableQueryRequest request,
        CancellationToken cancellationToken = default);

    Task<FinanceReceivable?> GetByIdAsync(string id, string? currentUserId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReceivableWriteOffDetailItem>> GetWriteOffsByReceivableIdAsync(
        string receivableId,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    Task<FinanceReceivableWriteOffCandidates> GetWriteOffCandidatesAsync(string customerId, string? currentUserId = null, CancellationToken cancellationToken = default);

    /// <summary>收款核销左栏：待核销收款客户汇总；附带 hasOpenReceivable 供前端过滤与灰显（见设计文档 §2.2.1）。</summary>
    Task<IReadOnlyList<FinanceWriteOffCustomerSummary>> GetWriteOffCustomerSummariesAsync(
        string? keyword,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    Task<FinanceReceivableWriteOffResult> ApplyWriteOffAsync(FinanceReceivableWriteOffRequest request, string? actingUserId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReceivableWriteOffListItem>> GetWriteOffsByReceiptIdAsync(string receiptId, CancellationToken cancellationToken = default);

    /// <summary>撤销收款单下全部收款明细来源的应收核销流水（物理删除流水并回滚应收/明细）。</summary>
    Task<FinanceReceiptReverseWriteOffResult> ReverseWriteOffsByReceiptAsync(
        string receiptId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinanceReceivableWriteOffLedgerItem>> GetWriteOffLedgerPagedAsync(
        FinanceReceivableWriteOffLedgerQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>销售订单明细详情「收款核销」页签：按销售明细 Id 列表取核销流水（不分页，上限 2000 条）。</summary>
    Task<IReadOnlyList<FinanceReceivableWriteOffLedgerItem>> GetWriteOffLedgerBySellOrderItemIdsAsync(
        IReadOnlyList<string> sellOrderItemIds,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>为出库完成(4)的销售出库补生成应收款；并清理非完成态误生成的历史应收（Debug 回填）。</summary>
    Task<FinanceReceivableBackfillResult> BackfillReceivablesFromCompletedStockOutsAsync(
        string? actingUserId = null,
        CancellationToken cancellationToken = default);
}

public class FinanceReceiptReverseWriteOffResult
{
    public int WriteOffCount { get; set; }
    public IReadOnlyList<string> ReceivableCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> StockOutCodes { get; set; } = Array.Empty<string>();
}

public class FinanceReceivableBackfillResult
{
    public int TotalCompletedSalesStockOuts { get; set; }
    public int AlreadyHasReceivableCount { get; set; }
    public int CandidateCount { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedIneligibleCount { get; set; }
    public int FailedCount { get; set; }
    public int StockOutDatesSyncedCount { get; set; }
    public int PrematureReceivablesRemovedCount { get; set; }
    public List<string> CreatedStockOutCodes { get; set; } = new();
    public List<string> SkippedIneligibleStockOutCodes { get; set; } = new();
    public List<string> StockOutDatesSyncedStockOutCodes { get; set; } = new();
    public List<string> PrematureReceivablesRemovedStockOutCodes { get; set; } = new();
    public List<string> FailedStockOutCodes { get; set; } = new();
    public List<string> FailedMessages { get; set; } = new();
}

public class FinanceReceivableWriteOffListItem
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public short WriteOffSource { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? FinanceReceiptItemId { get; set; }
    public string FinanceReceivableId { get; set; } = string.Empty;
    public string? ReceivableCode { get; set; }
    public string? StockOutCode { get; set; }
    public string? SellOrderCode { get; set; }
    public string? PN { get; set; }
    public string? Brand { get; set; }
    public short Currency { get; set; } = 1;
    public string? OperatorUserId { get; set; }
    public string? OperatorUserName { get; set; }
}

public class FinanceReceivableWriteOffDetailItem
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public short WriteOffSource { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? FinanceReceiptId { get; set; }
    public string? FinanceReceiptItemId { get; set; }
    public string? FinanceReceiptCode { get; set; }
    public string? OperatorUserId { get; set; }
    public string? OperatorUserName { get; set; }
    public string? Remark { get; set; }
}

public class FinanceReceivableWriteOffLedgerQueryRequest
{
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? CurrentUserId { get; set; }
    public IReadOnlyList<string>? SellOrderItemIds { get; set; }
}

public class FinanceReceivableWriteOffLedgerItem
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public short WriteOffSource { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? FinanceReceiptId { get; set; }
    public string? FinanceReceiptItemId { get; set; }
    public string? FinanceReceiptCode { get; set; }
    public string FinanceReceivableId { get; set; } = string.Empty;
    public string? ReceivableCode { get; set; }
    public string? StockOutId { get; set; }
    public string? StockOutCode { get; set; }
    public string? SellOrderId { get; set; }
    public string? SellOrderCode { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEnglishName { get; set; }
    public string? PN { get; set; }
    public string? Brand { get; set; }
    public short Currency { get; set; } = 1;
    public string? OperatorUserId { get; set; }
    public string? OperatorUserName { get; set; }
    public string? Remark { get; set; }
}
