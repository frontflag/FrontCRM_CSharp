using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public sealed class CommissionLadderRulesTests
{
    static CommissionLadderSlot[] Slots(params (decimal? t, decimal? p)[] filled)
    {
        var slots = new CommissionLadderSlot[10];
        for (var i = 0; i < filled.Length; i++)
            slots[i] = new CommissionLadderSlot(filled[i].t, filled[i].p);
        return slots;
    }

    [Fact]
    public void Validate_empty_ok()
    {
        Assert.Null(CommissionLadderRules.Validate(Slots()));
    }

    [Fact]
    public void Validate_gap_fails()
    {
        var slots = Slots((0m, 5m), (null, null), (100m, 7m));
        Assert.Contains("连续", CommissionLadderRules.Validate(slots));
    }

    [Fact]
    public void Validate_unpaired_fails()
    {
        var slots = Slots((0m, null));
        Assert.Contains("同时", CommissionLadderRules.Validate(slots));
    }

    [Fact]
    public void Validate_not_strictly_increasing_fails()
    {
        var slots = Slots((100m, 5m), (50m, 7m));
        Assert.Contains("严格递增", CommissionLadderRules.Validate(slots));
        var equal = Slots((50m, 5m), (50m, 7m));
        Assert.Contains("严格递增", CommissionLadderRules.Validate(equal));
    }

    [Fact]
    public void Validate_three_tiers_ok()
    {
        var slots = Slots((0m, 5m), (50000m, 7m), (100000m, 9m));
        Assert.Null(CommissionLadderRules.Validate(slots));
    }

    [Fact]
    public void ResolvePoints_picks_highest_threshold_not_exceeding_gp()
    {
        var slots = Slots((0m, 5m), (50000m, 7m), (100000m, 9m));
        Assert.Equal(7m, CommissionLadderRules.ResolvePoints(slots, 60000m));
        Assert.Equal(5m, CommissionLadderRules.ResolvePoints(slots, 0m));
        Assert.Equal(0m, CommissionLadderRules.ResolvePoints(slots, -100m));
        Assert.Equal(9m, CommissionLadderRules.ResolvePoints(slots, 100000m));
        Assert.Equal(0m, CommissionLadderRules.ResolvePoints(Slots(), 999m));
    }

    [Fact]
    public void Feb_and_Mar_qualify_independently()
    {
        var slots = Slots((300m, 3m), (1000m, 5m));
        var febPoints = CommissionLadderRules.ResolveMonthlyPoints(slots, 800m);
        var marPoints = CommissionLadderRules.ResolveMonthlyPoints(slots, 200m);
        Assert.Equal(3m, febPoints);
        Assert.Equal(0m, marPoints);
        Assert.Equal(24.00m, decimal.Round(CommissionLadderRules.CalcGp(800m) * febPoints / 100m, 2));
        Assert.Equal(0.00m, decimal.Round(CommissionLadderRules.CalcGp(200m) * marPoints / 100m, 2));
    }

    [Fact]
    public void Monthly_qualify_uses_first_tier_then_full_ladder()
    {
        var slots = Slots((300m, 3m), (1000m, 5m));
        Assert.False(CommissionLadderRules.MeetsFirstTier(slots, 200m));
        Assert.Equal(0m, CommissionLadderRules.ResolveMonthlyPoints(slots, 200m));
        Assert.True(CommissionLadderRules.MeetsFirstTier(slots, 700m));
        Assert.Equal(3m, CommissionLadderRules.ResolveMonthlyPoints(slots, 700m));
        Assert.Equal(5m, CommissionLadderRules.ResolveMonthlyPoints(slots, 1000m));
        Assert.Equal(0m, CommissionLadderRules.CalcGp(-80m));
        Assert.Equal(12m, CommissionLadderRules.CalcGp(12m));
    }

    [Fact]
    public void ToRate_divides_by_100()
    {
        Assert.Equal(0.05m, CommissionLadderRules.ToRate(5m));
    }

    [Fact]
    public void Normalize_threshold_integer_points_one_decimal()
    {
        var n = CommissionLadderRules.Normalize(Slots((100.4m, 1.55m)));
        Assert.Equal(100m, n[0].Threshold);
        Assert.Equal(1.6m, n[0].RatePoints);
    }
}
