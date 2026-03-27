namespace CRM.Core.Constants;

/// <summary>
/// Unified currency codes used across the whole project (1-based).
/// </summary>
public enum CurrencyCode : short
{
    RMB = 1,
    USD = 2,
    EUR = 3,
    HKD = 4,
    JPY = 5,
    GBP = 6,
}

public static class CurrencyCodeExtensions
{
    public static string ToIsoText(this CurrencyCode code) => code switch
    {
        CurrencyCode.RMB => "RMB",
        CurrencyCode.USD => "USD",
        CurrencyCode.EUR => "EUR",
        CurrencyCode.HKD => "HKD",
        CurrencyCode.JPY => "JPY",
        CurrencyCode.GBP => "GBP",
        _ => code.ToString()
    };
}

