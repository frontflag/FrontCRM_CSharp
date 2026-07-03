using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>向 <c>log_change_fldval</c> 写入字段变更记录。</summary>
public static class FieldChangeLogAppender
{
    private static string SqlQ(string? s) => (s ?? "").Replace("'", "''");

    public static string? NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool ValuesDiffer(string? oldVal, string? newVal)
    {
        var o = NormalizeValue(oldVal);
        var n = NormalizeValue(newVal);
        return !string.Equals(o, n, StringComparison.Ordinal);
    }

    public static async Task AppendIfChangedAsync(
        IUnitOfWork unitOfWork,
        string bizType,
        string recordId,
        string? recordCode,
        string fieldName,
        string fieldLabel,
        string? oldValue,
        string? newValue,
        string? changedByUserId,
        string changedByUserName)
    {
        if (!ValuesDiffer(oldValue, newValue))
            return;
        await AppendAsync(
            unitOfWork,
            bizType,
            recordId,
            recordCode,
            fieldName,
            fieldLabel,
            NormalizeValue(oldValue),
            NormalizeValue(newValue),
            changedByUserId,
            changedByUserName);
    }

    public static async Task AppendAsync(
        IUnitOfWork unitOfWork,
        string bizType,
        string recordId,
        string? recordCode,
        string fieldName,
        string fieldLabel,
        string? oldValue,
        string? newValue,
        string? changedByUserId,
        string changedByUserName)
    {
        var recordCodeSql = string.IsNullOrWhiteSpace(recordCode) ? "NULL" : $"'{SqlQ(recordCode)}'";
        var oldSql = oldValue == null ? "NULL" : $"'{SqlQ(oldValue)}'";
        var newSql = newValue == null ? "NULL" : $"'{SqlQ(newValue)}'";
        var userIdSql = string.IsNullOrWhiteSpace(changedByUserId) ? "NULL" : $"'{SqlQ(changedByUserId)}'";
        var sql = $@"
INSERT INTO log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
VALUES (gen_random_uuid()::text, '{SqlQ(bizType)}', '{SqlQ(recordId)}', {recordCodeSql}, '{SqlQ(fieldName)}', '{SqlQ(fieldLabel)}', {oldSql}, {newSql}, NOW(), {userIdSql}, '{SqlQ(changedByUserName)}', NULL, NULL)";
        await unitOfWork.ExecuteAsync(sql);
    }
}
