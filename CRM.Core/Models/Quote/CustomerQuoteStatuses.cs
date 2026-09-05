namespace CRM.Core.Models.Quote;

public static class CustomerQuoteDraftStatus
{
    public const short Draft = 0;
    public const short Converted = 1;
}

public static class CustomerQuoteStatus
{
    public const short Unsent = 0;
    public const short Sent = 1;
    public const short Void = 2;
}
