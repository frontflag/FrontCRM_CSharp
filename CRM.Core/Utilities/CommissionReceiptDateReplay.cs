namespace CRM.Core.Utilities;

public readonly record struct CommissionWriteOffStep(
    decimal Amount,
    DateOnly EventDate,
    DateTime CreateTimeUtc);

/// <summary>回放销售明细核销，得到收款核销日期（上海日）。</summary>
public static class CommissionReceiptDateReplay
{
    public static DateOnly? Resolve(
        decimal totalReceivable,
        IReadOnlyList<CommissionWriteOffStep> steps)
    {
        if (steps == null || steps.Count == 0)
            return null;

        var ordered = steps
            .OrderBy(x => x.CreateTimeUtc)
            .ThenBy(x => x.EventDate)
            .ToList();

        if (totalReceivable <= 0m)
            return ordered[^1].EventDate;

        var cum = 0m;
        foreach (var step in ordered)
        {
            cum += step.Amount;
            if (cum >= totalReceivable)
                return step.EventDate;
        }

        return ordered.Max(x => x.EventDate);
    }
}
