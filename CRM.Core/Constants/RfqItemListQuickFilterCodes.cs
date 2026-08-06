namespace CRM.Core.Constants;

/// <summary>需求明细列表左栏快捷检索业务项（<c>quickFilter</c>）。时间类 preset 由前端展开为 item/quote 创建时间窗。</summary>
public static class RfqItemListQuickFilterCodes
{
    public const string Important = "important";
    public const string Converted = "converted";
    public const string PendingQuote = "pending_quote";
    public const string NoQuote = "no_quote";
    public const string MultiQuote = "multi_quote";

    public static bool IsKnown(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var c = code.Trim();
        return c is Important or Converted or PendingQuote or NoQuote or MultiQuote;
    }
}
