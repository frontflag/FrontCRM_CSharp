using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionBaseOtherRulesTests
{
    static readonly DateOnly LockApr = new(2026, 4, 2);

    [Fact]
    public void Base_stock_out_in_window_paid_before_lock()
    {
        Assert.Equal(
            CommissionEntryKind.Base,
            CommissionBaseOtherRules.Classify(new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 20), LockApr));
        Assert.Equal(
            "2026-02",
            CommissionBaseOtherRules.CalcMonth(CommissionEntryKind.Base, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 20)));
    }

    [Fact]
    public void Other_early_stock_out_paid_in_span()
    {
        Assert.Equal(
            CommissionEntryKind.Other,
            CommissionBaseOtherRules.Classify(new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 18), LockApr));
        Assert.Equal(
            "2026-03",
            CommissionBaseOtherRules.CalcMonth(CommissionEntryKind.Other, new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 18)));
    }

    [Fact]
    public void Paid_on_or_after_lock_not_this_term()
    {
        Assert.Equal(
            CommissionEntryKind.None,
            CommissionBaseOtherRules.Classify(new DateOnly(2026, 3, 31), new DateOnly(2026, 4, 2), LockApr));
    }

    [Fact]
    public void ResolveCalcMonth_backfills_from_dates_when_stored_empty()
    {
        Assert.Equal(
            "2026-02",
            CommissionBaseOtherRules.ResolveCalcMonth(
                "",
                CommissionEntryKind.Base,
                new DateOnly(2026, 2, 10),
                new DateOnly(2026, 2, 20)));
        Assert.Equal(
            "2026-03",
            CommissionBaseOtherRules.ResolveCalcMonth(
                "  ",
                CommissionEntryKind.Other,
                new DateOnly(2026, 1, 15),
                new DateOnly(2026, 3, 18)));
        Assert.Equal(
            "2026-02",
            CommissionBaseOtherRules.ResolveCalcMonth(
                "2026-02",
                CommissionEntryKind.None,
                null,
                null));
    }

    [Fact]
    public void GetNextLockDate()
    {
        Assert.Equal(new DateOnly(2026, 4, 2), CommissionTerm.GetNextLockDate(new DateOnly(2026, 3, 15)));
        Assert.Equal(new DateOnly(2026, 4, 2), CommissionTerm.GetNextLockDate(new DateOnly(2026, 4, 2)));
        Assert.Equal(new DateOnly(2026, 6, 2), CommissionTerm.GetNextLockDate(new DateOnly(2026, 4, 3)));
        Assert.Equal(new DateOnly(2026, 2, 2), CommissionTerm.GetNextLockDate(new DateOnly(2026, 2, 1)));
    }
}
