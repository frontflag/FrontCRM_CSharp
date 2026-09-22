using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class CustomsDeclareDateFxTests
{
    private static DateTime Utc(int year, int month, int day, int hour, int minute = 0) =>
        new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    [Fact]
    public void DividesRmbByRateInEffectOnDeclareDay()
    {
        var fx = new CustomsDeclareDateFx(
        [
            (Utc(2026, 9, 20, 2, 0), 7.0000m),
            (Utc(2026, 9, 22, 1, 0), 7.2000m),
            (Utc(2026, 9, 23, 2, 0), 8.0000m)
        ]);

        var declareDate = Utc(2026, 9, 22, 0, 0);
        Assert.Equal(100.00m, fx.TotalTaxUsd(720m, declareDate));
    }

    [Fact]
    public void UsesPreviousDay_WhenDeclareDayHasNoChange()
    {
        var fx = new CustomsDeclareDateFx([(Utc(2026, 9, 18, 3, 0), 7.1000m)]);
        var declareDate = Utc(2026, 9, 22, 0, 0);
        Assert.Equal(10.00m, fx.TotalTaxUsd(71m, declareDate));
    }

    [Fact]
    public void Null_WhenDeclareDateMissing()
    {
        var fx = new CustomsDeclareDateFx([(Utc(2026, 9, 22, 1, 0), 7.2m)]);
        Assert.Null(fx.TotalTaxUsd(720m, null));
    }

    [Fact]
    public void Null_WhenDeclareDayBeforeFirstRate()
    {
        var fx = new CustomsDeclareDateFx([(Utc(2026, 9, 23, 1, 0), 7.2m)]);
        Assert.Null(fx.TotalTaxUsd(720m, Utc(2026, 9, 22, 0, 0)));
    }

    [Fact]
    public void ZeroRmb_WhenDeclareDatePresent()
    {
        var fx = new CustomsDeclareDateFx(Array.Empty<(DateTime, decimal)>());
        Assert.Equal(0m, fx.TotalTaxUsd(0m, Utc(2026, 9, 22, 0, 0)));
    }

    [Fact]
    public void SameShanghaiDay_LastSaveWins()
    {
        // 2026-09-22 02:00 UTC = 北京时间 10:00；18:00 UTC = 北京时间次日 02:00，不算当天。
        var fx = new CustomsDeclareDateFx(
        [
            (Utc(2026, 9, 22, 2, 0), 7.0000m),
            (Utc(2026, 9, 22, 9, 0), 7.2000m),
            (Utc(2026, 9, 22, 18, 0), 9.0000m)
        ]);

        Assert.Equal(100.00m, fx.TotalTaxUsd(720m, Utc(2026, 9, 22, 0, 0)));
    }
}
