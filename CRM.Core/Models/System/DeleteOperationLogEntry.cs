namespace CRM.Core.Models.System;

/// <summary>业务删除写入 log_operation 的统一入参。</summary>
public sealed class DeleteOperationLogEntry
{
    public required string BizType { get; init; }
    public required string RecordId { get; init; }
    public string? RecordCode { get; init; }

    /// <summary>业务实体中文名，用于默认 ActionType：{name}删除 / {name}强制删除。</summary>
    public string? EntityDisplayName { get; init; }

    public bool IsForceDelete { get; init; }

    /// <summary>指定时覆盖由 <see cref="EntityDisplayName"/> 生成的 ActionType。</summary>
    public string? ActionTypeOverride { get; init; }

    public string? OperatorUserId { get; init; }
    public string? OperatorUserName { get; init; }
    public string? Reason { get; init; }

    /// <summary>强制删除二次确认单号。</summary>
    public string? ForceConfirmBillCode { get; init; }

    /// <summary>指定时覆盖默认 OperationDesc。</summary>
    public string? OperationDescOverride { get; init; }

    /// <summary>追加到默认 OperationDesc 的额外说明。</summary>
    public string? ExtraDetail { get; init; }

    /// <summary>写入 log_operation.ExtraInfo（如整单删除时的明细 ID 列表）。</summary>
    public string? ExtraInfo { get; init; }
}
