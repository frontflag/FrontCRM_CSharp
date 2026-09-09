using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class UserLevelAsOfTests
{
    static DateTime Utc(int y, int m, int d, int h = 8) =>
        new(y, m, d, h, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void No_history_uses_current()
    {
        Assert.Equal((short)7, UserLevelAsOf.Resolve(7, [], new DateOnly(2026, 3, 1)));
        Assert.Equal((short)1, UserLevelAsOf.Resolve(0, [], new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void Change_after_as_of_uses_old_level()
    {
        var history = new[] { new UserLevelChangePoint(Utc(2026, 4, 1, 1), 3, 8) };
        Assert.Equal((short)3, UserLevelAsOf.Resolve(8, history, new DateOnly(2026, 3, 31)));
    }

    [Fact]
    public void Change_on_as_of_uses_new_level()
    {
        // 01:00 UTC = 09:00 上海，落在 4/1
        var history = new[] { new UserLevelChangePoint(Utc(2026, 4, 1, 1), 3, 8) };
        Assert.Equal((short)8, UserLevelAsOf.Resolve(8, history, new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void Multiple_changes_take_last_on_or_before()
    {
        var history = new[]
        {
            new UserLevelChangePoint(Utc(2025, 12, 1), 1, 2),
            new UserLevelChangePoint(Utc(2026, 2, 1), 2, 5),
            new UserLevelChangePoint(Utc(2026, 6, 1), 5, 9)
        };
        Assert.Equal((short)5, UserLevelAsOf.Resolve(9, history, new DateOnly(2026, 4, 10)));
    }
}
