namespace CRM.Core.Interfaces;

/// <summary>
/// 出库明细列表右侧「流程」聚合：销售明细 → 出库通知 → 本行库存明细 → 本行装箱 → 本出库行（当前）→ 应收款 → 收款核销。
/// </summary>
public interface IStockOutItemFlowService
{
    Task<StockOutItemFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockOutItemId,
        string? currentUserId,
        CancellationToken cancellationToken = default);
}

public sealed class StockOutItemFlowAggregatesDto
{
    public string StockOutItemId { get; set; } = string.Empty;
    public StockItemFlowDocDto? SellOrderItem { get; set; }
    public StockItemFlowDocDto? StockOutNotify { get; set; }
    public List<StockItemFlowDocDto> StockItems { get; set; } = new();
    public List<StockItemFlowDocDto> Packings { get; set; } = new();
    public List<StockItemFlowDocDto> StockOuts { get; set; } = new();
    public List<StockOutItemFlowReceivableDto> Receivables { get; set; } = new();
    public List<StockOutItemFlowReceiptWriteOffDto> ReceiptWriteOffs { get; set; } = new();
}

/// <summary>出库明细流程 · 应收站（本出库单 + 销售行级）。</summary>
public sealed class StockOutItemFlowReceivableDto
{
    public string Id { get; set; } = string.Empty;
    public string? ReceivableCode { get; set; }
    public short VerificationStatus { get; set; }
    public decimal Amount { get; set; }
    public decimal VerifiedToBe { get; set; }
    public short Currency { get; set; }
    public DateTime? StockOutDate { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCode { get; set; }
    public int StockOutItemLineCount { get; set; }
    public List<string> StockOutItemCodes { get; set; } = new();
}

/// <summary>出库明细流程 · 收款核销站（本出库单 + 销售行下流水）。</summary>
public sealed class StockOutItemFlowReceiptWriteOffDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public short Currency { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? FinanceReceiptId { get; set; }
    public string? FinanceReceiptCode { get; set; }
    public string? ReceivableCode { get; set; }
    public string? CustomerName { get; set; }
    public string? OperatorUserName { get; set; }
}
