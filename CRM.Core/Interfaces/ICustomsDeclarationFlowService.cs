namespace CRM.Core.Interfaces;

/// <summary>报关单列表/详情右侧「流程」页签聚合（以报关单为锚）。</summary>
public interface ICustomsDeclarationFlowService
{
    Task<CustomsDeclarationFlowAggregatesDto> GetFlowAggregatesAsync(
        string declarationId,
        CancellationToken cancellationToken = default);

    /// <summary>报关明细列表流程：站序与报关单相同，上下游只挂本行；装箱与报关仍是单头。</summary>
    Task<CustomsDeclarationFlowAggregatesDto> GetFlowAggregatesForItemAsync(
        string declarationItemId,
        CancellationToken cancellationToken = default);
}

public sealed class CustomsDeclarationFlowAggregatesDto
{
    public string DeclarationId { get; set; } = string.Empty;
    public List<CustomsDeclarationFlowDocDto> SellOrderItems { get; set; } = new();
    public List<CustomsDeclarationFlowDocDto> SalesStockOutNotifies { get; set; } = new();
    public List<CustomsDeclarationFlowDocDto> Pendlists { get; set; } = new();
    public List<CustomsDeclarationFlowDocDto> CustomsStockOutNotifies { get; set; } = new();
    public CustomsDeclarationFlowDocDto? Packing { get; set; }
    public CustomsDeclarationFlowDocDto Declaration { get; set; } = new();
    public List<CustomsDeclarationFlowDocDto> StockOuts { get; set; } = new();
    public List<CustomsDeclarationFlowDocDto> Arrivals { get; set; } = new();
    public List<CustomsDeclarationFlowDocDto> Qcs { get; set; } = new();
    public List<CustomsDeclarationFlowDocDto> StockIns { get; set; } = new();
}

public sealed class CustomsDeclarationFlowDocDto
{
    public string Id { get; set; } = string.Empty;
    public string? DocCode { get; set; }
    public short? Status { get; set; }
    public DateTime? CreateTime { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerCode { get; set; }
    public string? PersonName { get; set; }
    public decimal? UnitPrice { get; set; }
    public short? Currency { get; set; }
    public decimal? Qty { get; set; }
    public bool IsDeleted { get; set; }
    public string? SalesOrderId { get; set; }
    /// <summary>销售明细收款进度：0 待收款，1 部分收款，2 收款完成。</summary>
    public short? ReceiptProgressStatus { get; set; }
    /// <summary>销售明细开票进度：0 待开票，1 部分开票，2 开票完成。</summary>
    public short? InvoiceProgressStatus { get; set; }
    /// <summary>仅报关当前站：报关公司名称（非 vendorId）。</summary>
    public string? BrokerName { get; set; }
    public short? StockOutType { get; set; }
    public short? StockInType { get; set; }
    public string? CustomsDeclarationId { get; set; }
    public string? CustomsDeclarationCode { get; set; }
}
