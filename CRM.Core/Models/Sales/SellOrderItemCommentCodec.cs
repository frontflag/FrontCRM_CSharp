namespace CRM.Core.Models.Sales;

/// <summary>
/// 销售明细 <see cref="SellOrderItem.Comment"/> 与前端 <c>buildItemComment</c>/<c>parseItemCommentForDraft</c> 对齐的解析（客户物料型号前缀行）。
/// </summary>
public static class SellOrderItemCommentCodec
{
    /// <summary>与 Vue 中 <c>t('salesOrderCreate.comment.customerMpn') + '：'</c> 一致的前缀（含全角冒号）。</summary>
    private static readonly string[] CustomerMaterialModelPrefixes =
    [
        "客户物料型号：",
        "客户物料型号:",
        "Customer part no.：",
        "Customer part no.:",
        "Customer part no：",
        "Customer part no:"
    ];

    /// <summary>从行备注首段解析客户物料型号；无匹配前缀则返回 null。</summary>
    public static string? TryParseCustomerMaterialModelFromComment(string? comment, int maxLen = 200)
    {
        if (string.IsNullOrWhiteSpace(comment)) return null;
        var s = comment.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        foreach (var prefix in CustomerMaterialModelPrefixes)
        {
            if (!s.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var rest = s[prefix.Length..].Trim();
            var nl = rest.IndexOf('\n');
            var value = nl >= 0 ? rest[..nl].Trim() : rest;
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > maxLen ? value[..maxLen] : value;
        }

        return null;
    }
}
