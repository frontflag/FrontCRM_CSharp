namespace CRM.Core.Constants;

/// <summary>收款单主状态。2026-08 起不再审核：新建 → 确认 / 取消。</summary>
public static class FinanceReceiptStatusCode
{
    /// <summary>新建（原草稿 0；历史待审核 1 迁入本值）。</summary>
    public const short New = 0;

    /// <summary>历史待审核。迁库后不应再出现；读侧仍视为新建。</summary>
    public const short LegacyPendingAudit = 1;

    /// <summary>历史已审核。迁库后并入确认(3)；读侧仍视为已确认。</summary>
    public const short LegacyApproved = 2;

    /// <summary>确认（原已收款 3）。可核销、可入预收池。</summary>
    public const short Confirmed = 3;

    /// <summary>取消。</summary>
    public const short Cancelled = 4;

    public static bool IsNew(short status) =>
        status == New || status == LegacyPendingAudit;

    public static bool IsConfirmed(short status) =>
        status == Confirmed || status == LegacyApproved;
}
