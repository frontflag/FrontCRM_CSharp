using CRM.Core.Interfaces;
using CRM.Core.Models.System;

namespace CRM.Core.Services;

public class LogOperationAppendService : ILogOperationAppendService
{
    private readonly IUnitOfWork _unitOfWork;

    public LogOperationAppendService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private static string SqlQ(string? s) => (s ?? "").Replace("'", "''", StringComparison.Ordinal);

    public async Task AppendAsync(
        string bizType,
        string recordId,
        string? recordCode,
        string actionType,
        string? operatorUserId,
        string? operatorUserName,
        string? operationDesc,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeBiz = SqlQ(bizType);
        var safeRecordId = SqlQ(recordId);
        var recordCodeSql = string.IsNullOrWhiteSpace(recordCode) ? "NULL" : $"'{SqlQ(recordCode)}'";
        var safeAction = SqlQ(actionType);
        var safeDesc = SqlQ(operationDesc);
        var safeUserName = SqlQ(operatorUserName);
        var opUserSql = string.IsNullOrWhiteSpace(operatorUserId) ? "NULL" : $"'{SqlQ(operatorUserId)}'";
        var reasonSql = string.IsNullOrWhiteSpace(reason) ? "NULL" : $"'{SqlQ(reason)}'";
        var sql = $@"
INSERT INTO log_operation (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""ActionType"", ""OperationTime"", ""OperatorUserId"", ""OperatorUserName"", ""Reason"", ""ExtraInfo"", ""SysRemark"", ""OperationDesc"")
VALUES (gen_random_uuid()::text, '{safeBiz}', '{safeRecordId}', {recordCodeSql}, '{safeAction}', NOW(), {opUserSql}, '{safeUserName}', {reasonSql}, NULL, NULL, '{safeDesc}')";
        await _unitOfWork.ExecuteAsync(sql);
    }

    public Task AppendDeleteAsync(DeleteOperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(entry.BizType))
            throw new ArgumentException("BizType 不能为空", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.RecordId))
            throw new ArgumentException("RecordId 不能为空", nameof(entry));

        var actionType = !string.IsNullOrWhiteSpace(entry.ActionTypeOverride)
            ? entry.ActionTypeOverride.Trim()
            : string.IsNullOrWhiteSpace(entry.EntityDisplayName)
                ? throw new ArgumentException("EntityDisplayName 与 ActionTypeOverride 至少填一项", nameof(entry))
                : entry.IsForceDelete
                    ? $"{entry.EntityDisplayName.Trim()}强制删除"
                    : $"{entry.EntityDisplayName.Trim()}删除";

        var operationDesc = !string.IsNullOrWhiteSpace(entry.OperationDescOverride)
            ? entry.OperationDescOverride
            : BuildDeleteOperationDesc(entry);

        return AppendAsync(
            entry.BizType.Trim(),
            entry.RecordId.Trim(),
            string.IsNullOrWhiteSpace(entry.RecordCode) ? null : entry.RecordCode.Trim(),
            actionType,
            entry.OperatorUserId,
            string.IsNullOrWhiteSpace(entry.OperatorUserName) ? null : entry.OperatorUserName.Trim(),
            operationDesc,
            string.IsNullOrWhiteSpace(entry.Reason) ? null : entry.Reason.Trim(),
            cancellationToken);
    }

    private static string BuildDeleteOperationDesc(DeleteOperationLogEntry entry)
    {
        var parts = new List<string>();
        parts.Add(entry.IsForceDelete ? "强制删除" : "删除");
        if (!string.IsNullOrWhiteSpace(entry.EntityDisplayName))
            parts.Add(entry.EntityDisplayName.Trim());
        if (!string.IsNullOrWhiteSpace(entry.RecordCode))
            parts.Add($"单号={entry.RecordCode.Trim()}");
        if (!string.IsNullOrWhiteSpace(entry.ForceConfirmBillCode))
            parts.Add($"确认单号={entry.ForceConfirmBillCode.Trim()}");
        if (!string.IsNullOrWhiteSpace(entry.Reason))
            parts.Add($"理由={entry.Reason.Trim()}");
        if (!string.IsNullOrWhiteSpace(entry.ExtraDetail))
            parts.Add(entry.ExtraDetail.Trim());
        parts.Add($"Id={entry.RecordId.Trim()}");
        return string.Join("，", parts);
    }
}
