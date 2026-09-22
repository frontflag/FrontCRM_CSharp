namespace CRM.Core.Utilities;

/// <summary>
/// 按申报日把报关总费用（人民币）折成美元。
/// 汇率取财务变更记录里、申报日（北京时间日历日）当天及之前最后一次保存的美元兑人民币。
/// </summary>
public sealed class CustomsDeclareDateFx
{
    private readonly List<RatePoint> _newestFirst;

    public CustomsDeclareDateFx(IEnumerable<(DateTime ChangedAtUtc, decimal UsdToCny)> logs)
    {
        _newestFirst = logs
            .Where(x => x.UsdToCny > 0m)
            .Select(x =>
            {
                var at = PostgreSqlDateTime.ToUtc(x.ChangedAtUtc);
                return new RatePoint(CompanyCalendarDate.ToCompanyDate(at), at, x.UsdToCny);
            })
            .OrderByDescending(x => x.ChangedAtUtc)
            .ToList();
    }

    /// <summary>
    /// 无申报日期、或申报日早于任何正汇率记录时返回 null。人民币总额为 0 时返回 0。
    /// </summary>
    public decimal? TotalTaxUsd(decimal totalTaxRmb, DateTime? declareDate)
    {
        if (!declareDate.HasValue)
            return null;
        if (totalTaxRmb == 0m)
            return 0m;

        var day = CompanyCalendarDate.ToCompanyDate(PostgreSqlDateTime.ToUtc(declareDate.Value));
        foreach (var point in _newestFirst)
        {
            if (point.Day <= day)
                return Math.Round(totalTaxRmb / point.UsdToCny, 2, MidpointRounding.AwayFromZero);
        }

        return null;
    }

    private readonly record struct RatePoint(DateOnly Day, DateTime ChangedAtUtc, decimal UsdToCny);
}
