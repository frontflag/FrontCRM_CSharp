using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionTermTests
{
    [Theory]
    [InlineData(2026, 2, 2, 2025, 12, 1, 2026, 1, 31, "202601")]
    [InlineData(2026, 4, 2, 2026, 2, 1, 2026, 3, 31, "202603")]
    [InlineData(2026, 6, 2, 2026, 4, 1, 2026, 5, 31, "202605")]
    [InlineData(2026, 8, 2, 2026, 6, 1, 2026, 7, 31, "202607")]
    [InlineData(2026, 10, 2, 2026, 8, 1, 2026, 9, 30, "202609")]
    [InlineData(2026, 12, 2, 2026, 10, 1, 2026, 11, 30, "202611")]
    public void GetWindow_six_lock_dates(
        int ly, int lm, int ld,
        int fy, int fm, int fd,
        int ty, int tm, int td,
        string term)
    {
        var w = CommissionTerm.GetWindow(new DateOnly(ly, lm, ld));
        Assert.Equal(new DateOnly(fy, fm, fd), w.From);
        Assert.Equal(new DateOnly(ty, tm, td), w.To);
        Assert.Equal(term, w.Term);
    }

    [Fact]
    public void GetWindow_leap_year_feb_end()
    {
        var w = CommissionTerm.GetWindow(new DateOnly(2024, 4, 2));
        Assert.Equal(new DateOnly(2024, 2, 1), w.From);
        Assert.Equal(new DateOnly(2024, 3, 31), w.To);
        Assert.Equal("202403", w.Term);
    }

    [Fact]
    public void GetOpenWindow_before_and_on_lock_day()
    {
        var before = CommissionTerm.GetOpenWindow(new DateOnly(2026, 2, 1));
        Assert.Equal(new DateOnly(2025, 12, 1), before.From);
        Assert.Equal(new DateOnly(2026, 1, 31), before.To);

        var onLock = CommissionTerm.GetOpenWindow(new DateOnly(2026, 2, 2));
        Assert.Equal(new DateOnly(2026, 2, 1), onLock.From);
        Assert.Equal(new DateOnly(2026, 3, 31), onLock.To);
    }

    [Fact]
    public void GetOpenWindow_dec_after_lock_crosses_year()
    {
        var w = CommissionTerm.GetOpenWindow(new DateOnly(2026, 12, 2));
        Assert.Equal(new DateOnly(2026, 12, 1), w.From);
        Assert.Equal(new DateOnly(2027, 1, 31), w.To);
        Assert.Equal("202701", w.Term);
    }

    [Theory]
    [InlineData(2026, 4, 2, 2026, 4, 2)]
    [InlineData(2026, 4, 10, 2026, 4, 2)]
    [InlineData(2026, 4, 1, 2026, 2, 2)]
    [InlineData(2026, 5, 9, 2026, 4, 2)]
    [InlineData(2026, 1, 15, 2025, 12, 2)]
    public void ResolveLockDate(int y, int m, int d, int ey, int em, int ed)
    {
        Assert.Equal(new DateOnly(ey, em, ed), CommissionTerm.ResolveLockDate(new DateOnly(y, m, d)));
    }

    [Fact]
    public void FormatTermLabel()
    {
        Assert.Equal("2026-01（12–1月）", CommissionTerm.FormatTermLabel("202601"));
        Assert.Equal("2026-03（2–3月）", CommissionTerm.FormatTermLabel("202603"));
    }
}
