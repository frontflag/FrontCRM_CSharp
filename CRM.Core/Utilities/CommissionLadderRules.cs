using CRM.Core.Constants;

namespace CRM.Core.Utilities;

public readonly record struct CommissionLadderSlot(decimal? Threshold, decimal? RatePoints);

/// <summary>提成 10 档校验与取档（纯函数）。</summary>
public static class CommissionLadderRules
{
    public static string? Validate(IReadOnlyList<CommissionLadderSlot> slots)
    {
        if (slots == null || slots.Count != CommissionLadder.Count)
            return $"阶梯须为 {CommissionLadder.Count} 档";

        var filled = new bool[CommissionLadder.Count];
        for (var i = 0; i < CommissionLadder.Count; i++)
        {
            var t = slots[i].Threshold;
            var p = slots[i].RatePoints;
            var hasT = t.HasValue;
            var hasP = p.HasValue;
            if (hasT != hasP)
                return $"第 {i + 1} 档达标金额与提成点数需同时填写或同时留空";
            filled[i] = hasT;
        }

        var k = 0;
        while (k < CommissionLadder.Count && filled[k]) k++;
        for (var i = k; i < CommissionLadder.Count; i++)
        {
            if (filled[i])
                return $"第 {i + 1} 档有值时，第 1～{i + 1} 档必须连续填写";
        }

        decimal? prev = null;
        for (var i = 0; i < k; i++)
        {
            var threshold = RoundMoney(slots[i].Threshold!.Value);
            var points = RoundPoints(slots[i].RatePoints!.Value);
            if (threshold < 0m)
                return $"第 {i + 1} 档达标金额不能为负";
            if (points < 0m || points > 100m)
                return $"第 {i + 1} 档提成点数须为 0.0～100.0";
            if (prev.HasValue && threshold <= prev.Value)
                return "达标金额必须随阶梯严格递增";
            prev = threshold;
        }

        return null;
    }

    public static decimal ResolvePoints(IReadOnlyList<CommissionLadderSlot> slots, decimal gp)
    {
        if (slots == null || slots.Count == 0) return 0m;
        for (var i = Math.Min(slots.Count, CommissionLadder.Count) - 1; i >= 0; i--)
        {
            var t = slots[i].Threshold;
            var p = slots[i].RatePoints;
            if (!t.HasValue || !p.HasValue) continue;
            if (t.Value <= gp) return RoundPoints(p.Value);
        }

        return 0m;
    }

    public static decimal ToRate(decimal points) => RoundPoints(points) / 100m;

    public static CommissionLadderSlot[] Normalize(IReadOnlyList<CommissionLadderSlot> slots)
    {
        var result = new CommissionLadderSlot[CommissionLadder.Count];
        for (var i = 0; i < CommissionLadder.Count; i++)
        {
            if (slots != null && i < slots.Count)
            {
                var t = slots[i].Threshold;
                var p = slots[i].RatePoints;
                result[i] = new CommissionLadderSlot(
                    t.HasValue ? RoundMoney(t.Value) : null,
                    p.HasValue ? RoundPoints(p.Value) : null);
            }
            else
            {
                result[i] = default;
            }
        }

        return result;
    }

    public static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero);

    public static decimal RoundPoints(decimal value) =>
        decimal.Round(value, 1, MidpointRounding.AwayFromZero);
}
