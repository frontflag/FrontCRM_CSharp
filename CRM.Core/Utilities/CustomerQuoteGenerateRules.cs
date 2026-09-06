namespace CRM.Core.Utilities;

/// <summary>
/// 草稿生成客户报价单：所选行须同一客户、同一 RFQ 业务员（空 ID 不参与冲突）。
/// 未发送单加行时与主表对齐同一口径。
/// </summary>
public static class CustomerQuoteGenerateRules
{
    public const string DifferentCustomerMessage = "请选择同一家客户的草稿";
    public const string DifferentSalesUserMessage = "请选择同一业务员的草稿";
    public const string LineDifferentCustomerMessage = "明细客户与报价单客户不一致";
    public const string LineDifferentSalesUserMessage = "明细业务员与报价单业务员不一致";

    /// <summary>非空值去空白后必须全部相同；空值忽略。</summary>
    public static bool NonEmptyIdsAgree(IEnumerable<string?> ids)
    {
        string? first = null;
        foreach (var id in ids)
        {
            var t = id?.Trim();
            if (string.IsNullOrEmpty(t))
                continue;
            if (first == null)
            {
                first = t;
                continue;
            }

            if (!string.Equals(first, t, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    public static string? FirstNonEmpty(IEnumerable<string?> ids)
    {
        foreach (var id in ids)
        {
            var t = id?.Trim();
            if (!string.IsNullOrEmpty(t))
                return t;
        }

        return null;
    }

    /// <summary>空值与任意值兼容；两个非空必须相等（忽略大小写）。</summary>
    public static bool IdsCompatible(string? a, string? b)
    {
        var x = a?.Trim();
        var y = b?.Trim();
        if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
            return true;
        return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertDraftsSameCustomerAndSalesUser(
        IEnumerable<string?> customerIds,
        IEnumerable<string?> salesUserIds)
    {
        if (!NonEmptyIdsAgree(customerIds))
            throw new InvalidOperationException(DifferentCustomerMessage);
        if (!NonEmptyIdsAgree(salesUserIds))
            throw new InvalidOperationException(DifferentSalesUserMessage);
    }

    public static void AssertLineMatchesHeader(
        string? headerCustomerId,
        string? headerSalesUserId,
        string? lineCustomerId,
        string? lineSalesUserId)
    {
        if (!IdsCompatible(headerCustomerId, lineCustomerId))
            throw new InvalidOperationException(LineDifferentCustomerMessage);
        if (!IdsCompatible(headerSalesUserId, lineSalesUserId))
            throw new InvalidOperationException(LineDifferentSalesUserMessage);
    }

    /// <summary>对外编号：v1 仅编码；v2 起为 {编码}-{版本}。</summary>
    public static string FormatDisplayCode(string? customerQuoteCode, int versionNo)
    {
        var code = (customerQuoteCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
            return string.Empty;
        if (versionNo <= 1)
            return code;
        return $"{code}-{versionNo}";
    }
}
