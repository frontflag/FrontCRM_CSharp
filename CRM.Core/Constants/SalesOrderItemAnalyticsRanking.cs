namespace CRM.Core.Constants;

/// <summary>销售订单明细排行排序维度。</summary>
public static class SalesOrderItemAnalyticsRankingSort
{
    public const string Amount = "amount";
    public const string Count = "count";

    public static string Normalize(string? value) =>
        string.Equals(value, Count, StringComparison.OrdinalIgnoreCase) ? Count : Amount;
}

/// <summary>明细维排行 count 口径（仅 line 类 Top10）。</summary>
public static class SalesOrderItemAnalyticsRankingLineMetric
{
    /// <summary>明细行数（列表看板默认）。</summary>
    public const string Lines = "lines";

    /// <summary>去重销售订单张数（报表订单 Tab）。</summary>
    public const string Transactions = "transactions";

    public static string Normalize(string? value) =>
        string.Equals(value, Transactions, StringComparison.OrdinalIgnoreCase) ? Transactions : Lines;
}
