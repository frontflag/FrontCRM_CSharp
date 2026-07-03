using CRM.Core.Models.System;

namespace CRM.Core.Interfaces;

/// <summary>向统一表 log_operation 追加一条记录（与供应商/客户等写入口径一致）。</summary>
public interface ILogOperationAppendService
{
    Task AppendAsync(
        string bizType,
        string recordId,
        string? recordCode,
        string actionType,
        string? operatorUserId,
        string? operatorUserName,
        string? operationDesc,
        string? reason = null,
        string? extraInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>业务删除统一写 log_operation（凡删除必记）。</summary>
    Task AppendDeleteAsync(
        DeleteOperationLogEntry entry,
        CancellationToken cancellationToken = default);
}
