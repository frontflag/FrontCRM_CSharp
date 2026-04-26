namespace CRM.Core.Services.InternalTransfer;

/// <summary>移库共享内核上下文：仅影响 Remark 模板等，不参与数量公式。</summary>
public enum InternalTransferKind : short
{
    Manual = 0,
    Customs = 1
}

/// <summary>内核入参：无报关/手工业务名，仅数据（与《移库共享内核_虚拟出入库实施方案》一致）。</summary>
public sealed class InternalTransferPostingRequest
{
    public InternalTransferKind Kind { get; init; }

    /// <summary>编排层已算好的正整数；内核禁止再读用户输入数量。</summary>
    public int MoveQty { get; init; }

    public string SourceStockItemId { get; init; } = string.Empty;

    /// <summary>须与源在库行仓库一致（冗余校验）。</summary>
    public string FromWarehouseId { get; init; } = string.Empty;

    public string ToWarehouseId { get; init; } = string.Empty;

    public string? ToLocationId { get; init; }

    /// <summary>移库主单主键；写入虚拟出/入 <c>SourceId</c>、流水 <c>BizId</c>。</summary>
    public string TransferHeaderId { get; init; } = string.Empty;

    /// <summary>展示单号；写入 <c>SourceCode</c>（超长截断）、Remark。</summary>
    public string TransferBusinessCode { get; init; } = string.Empty;

    /// <summary>流水行键（如 <c>stocktransfer_item_manual</c> 主键）。</summary>
    public string TransferItemLineId { get; init; } = string.Empty;

    /// <summary>库存流水业务类型（如 MANUAL_TRANS）。</summary>
    public string LedgerBizType { get; init; } = string.Empty;

    public string? ActingUserId { get; init; }
}

/// <summary>内核出参：供编排层写移库明细与前端展示。</summary>
public sealed class InternalTransferPostingResult
{
    public int MoveQty { get; init; }
    public string TargetStockItemId { get; init; } = string.Empty;
    public string TargetStockAggregateId { get; init; } = string.Empty;
    public string VirtualStockOutId { get; init; } = string.Empty;
    public string VirtualStockInId { get; init; } = string.Empty;
    public string StockOutCode { get; init; } = string.Empty;
    public string StockInCode { get; init; } = string.Empty;
}
