namespace CRM.Core.Utilities;

/// <summary>出库运维检查：金额/数量对账容差。</summary>
public static class StockOutOpsCheckAmounts
{
    public const decimal AmountTolerance = 0.009m;

    public static decimal RoundAmount(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static bool AmountsMatch(decimal expected, decimal actual) =>
        Math.Abs(RoundAmount(expected) - RoundAmount(actual)) <= AmountTolerance;

    public static bool QuantitiesMatch(decimal expected, decimal actual) =>
        Math.Abs(expected - actual) < 0.0001m;
}
