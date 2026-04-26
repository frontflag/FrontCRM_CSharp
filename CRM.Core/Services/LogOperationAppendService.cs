using CRM.Core.Interfaces;

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
}
