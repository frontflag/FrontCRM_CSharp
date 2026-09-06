using CRM.Core.Models.Quote;

namespace CRM.Core.Utilities;

/// <summary>客户报价单邮件发送状态机（对照 QA CQ-MAIL-003 / CQ-MAIL-005 / 作废拒绝）。</summary>
public static class CustomerQuoteSendRules
{
    public const string VoidCannotSendMessage = "作废报价单不可发送邮件";
    public const string InvalidToMessage = "请填写有效的收件人邮箱";
    public const string OnlyUnsentCanDeleteMessage = "仅待发送的报价单可删除";
    public const string OnlyUnsentCanMarkSentMessage = "仅待发送的报价单可设置已发送";

    public static bool CanSendEmail(short status) => status != CustomerQuoteStatus.Void;

    public static bool CanDelete(short status) => status == CustomerQuoteStatus.Unsent;

    public static bool CanMarkSent(short status) => status == CustomerQuoteStatus.Unsent;

    public static bool LooksLikeEmail(string? to)
    {
        var s = to?.Trim() ?? string.Empty;
        var at = s.IndexOf('@');
        return at > 0 && at < s.Length - 1 && s.IndexOf('.', at) > at + 1 && !s.Contains(' ');
    }

    /// <summary>
    /// 未发送 → 已发送并写首次 <see cref="CustomerQuote.SentAt"/>；
    /// 已发送再发只把 <see cref="CustomerQuote.SentByEmail"/> 置真，不改 <see cref="CustomerQuote.SentAt"/>。
    /// </summary>
    public static void ApplySentByEmail(CustomerQuote header, DateTime utcNow)
    {
        if (header.Status == CustomerQuoteStatus.Void)
            throw new InvalidOperationException(VoidCannotSendMessage);

        if (header.Status == CustomerQuoteStatus.Unsent)
        {
            header.Status = CustomerQuoteStatus.Sent;
            header.SentAt = utcNow;
        }

        header.SentByEmail = true;
    }

    /// <summary>列表「设置已发送」：仅待发送可点；写首次 <see cref="CustomerQuote.SentAt"/>，不改 <see cref="CustomerQuote.SentByEmail"/>。</summary>
    public static void ApplyMarkSent(CustomerQuote header, DateTime utcNow)
    {
        if (header.Status != CustomerQuoteStatus.Unsent)
            throw new InvalidOperationException(OnlyUnsentCanMarkSentMessage);

        header.Status = CustomerQuoteStatus.Sent;
        header.SentAt = utcNow;
    }
}
