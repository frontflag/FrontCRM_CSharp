using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionReceiptDateReplayTests
{
    [Fact]
    public void First_step_that_covers_receivable()
    {
        var steps = new[]
        {
            new CommissionWriteOffStep(40m, new DateOnly(2026, 3, 1), new DateTime(2026, 3, 1, 1, 0, 0, DateTimeKind.Utc)),
            new CommissionWriteOffStep(60m, new DateOnly(2026, 4, 10), new DateTime(2026, 4, 10, 2, 0, 0, DateTimeKind.Utc)),
            new CommissionWriteOffStep(10m, new DateOnly(2026, 4, 20), new DateTime(2026, 4, 20, 2, 0, 0, DateTimeKind.Utc))
        };
        Assert.Equal(new DateOnly(2026, 4, 10), CommissionReceiptDateReplay.Resolve(100m, steps));
    }

    [Fact]
    public void Fallback_to_max_date_when_never_covers()
    {
        var steps = new[]
        {
            new CommissionWriteOffStep(10m, new DateOnly(2026, 3, 1), new DateTime(2026, 3, 1, 1, 0, 0, DateTimeKind.Utc)),
            new CommissionWriteOffStep(20m, new DateOnly(2026, 4, 8), new DateTime(2026, 4, 8, 1, 0, 0, DateTimeKind.Utc))
        };
        Assert.Equal(new DateOnly(2026, 4, 8), CommissionReceiptDateReplay.Resolve(100m, steps));
    }

    [Fact]
    public void Empty_returns_null()
    {
        Assert.Null(CommissionReceiptDateReplay.Resolve(100m, []));
    }
}
