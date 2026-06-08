namespace CRM.Core.Constants;

/// <summary>采购订单货代单号业务规则。</summary>
public static class PurchaseOrderFreightForwarderOrderNoRules
{
    public const int MaxLength = 64;

    /// <summary>允许录入/修改货代单号的主表状态。</summary>
    public static readonly HashSet<short> EditableStatuses = new() { 10, 20, 30, 50, 100 };

    public static bool IsEditableStatus(short status) => EditableStatuses.Contains(status);

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }
}
