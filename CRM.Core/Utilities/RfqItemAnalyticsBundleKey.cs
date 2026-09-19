using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 需求明细看板四接口并行时共用内存包的键：同一筛选只打一次库。
/// </summary>
public static class RfqItemAnalyticsBundleKey
{
    public static string From(RFQItemQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return string.Join(
            '\u001f',
            Norm(request.CurrentUserId),
            DateKey(request.StartDate),
            DateKey(request.EndDate),
            DateKey(request.ItemCreateStartUtc),
            DateKey(request.ItemCreateEndExclusiveUtc),
            DateKey(request.QuoteCreateStartUtc),
            DateKey(request.QuoteCreateEndExclusiveUtc),
            Norm(request.QuickFilter),
            Norm(request.CustomerId),
            Norm(request.CustomerKeyword),
            Norm(request.MaterialModel),
            request.BrandId?.ToString() ?? string.Empty,
            Norm(request.SalesUserId),
            Norm(request.SalesUserKeyword),
            Norm(request.PurchaserUserId),
            request.HasQuotesOnly == true ? "1" : "0",
            request.Status?.ToString() ?? string.Empty,
            Norm(request.RfqCode),
            Norm(request.AnalyticsDataset),
            Norm(request.AnalyticsViewLevel),
            Norm(request.AnalyticsDepartmentId),
            request.CanViewCustomerInList ? "1" : "0",
            request.QuotableByMeOnly ? "1" : "0",
            request.ForRfqItemReference ? "1" : "0");
    }

    private static string Norm(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string DateKey(DateTime? value) =>
        value.HasValue ? value.Value.ToUniversalTime().ToString("o") : string.Empty;
}
