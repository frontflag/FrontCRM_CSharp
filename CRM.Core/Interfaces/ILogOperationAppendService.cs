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
        CancellationToken cancellationToken = default);
}
