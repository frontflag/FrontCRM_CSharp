using CRM.Core.Models.Finance;

namespace CRM.Core.Utilities;

/// <summary>客户对账单期间余额与流水（纯计算，不访问数据库）。</summary>
public static class FinanceReceivableStatementCalculator
{
    public static FinanceReceivableStatementHeaderDto Compute(
        DateOnly periodFrom,
        DateOnly periodTo,
        DateOnly generatedOn,
        DateOnly agingCutoff,
        short currency,
        IReadOnlyList<FinanceReceivableStatementEvent> events,
        out IReadOnlyList<FinanceReceivableStatementLineDto> lines)
    {
        if (periodFrom > periodTo)
            throw new ArgumentException("对账期间起日不能晚于止日。");

        var opening = 0m;
        var periodIncrease = 0m;
        var periodReceived = 0m;
        var periodEvents = new List<FinanceReceivableStatementEvent>();

        foreach (var ev in events)
        {
            if (ev.BusinessDate < periodFrom)
            {
                if (ev.LineType == FinanceReceivableStatementLineTypes.Increase)
                    opening += ev.Amount;
                else if (ev.LineType == FinanceReceivableStatementLineTypes.Receipt)
                    opening -= ev.Amount;
                continue;
            }

            if (ev.BusinessDate > periodTo)
                continue;

            periodEvents.Add(ev);
            if (ev.LineType == FinanceReceivableStatementLineTypes.Increase)
                periodIncrease += ev.Amount;
            else if (ev.LineType == FinanceReceivableStatementLineTypes.Receipt)
                periodReceived += ev.Amount;
        }

        var ending = opening + periodIncrease - periodReceived;
        var built = new List<FinanceReceivableStatementLineDto>
        {
            new()
            {
                LineType = FinanceReceivableStatementLineTypes.Opening,
                Date = periodFrom,
                DocNo = null,
                Summary = "期初应收余额",
                IncreaseAmount = null,
                ReceivedAmount = null,
                Balance = opening
            }
        };

        var ordered = periodEvents
            .OrderBy(e => e.BusinessDate)
            .ThenBy(e => e.LineType == FinanceReceivableStatementLineTypes.Increase ? 0 : 1)
            .ThenBy(e => e.DocNo ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.ReceivableId ?? e.WriteOffId ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        var balance = opening;
        foreach (var ev in ordered)
        {
            var increase = ev.LineType == FinanceReceivableStatementLineTypes.Increase ? ev.Amount : (decimal?)null;
            var received = ev.LineType == FinanceReceivableStatementLineTypes.Receipt ? ev.Amount : (decimal?)null;
            balance = balance + (increase ?? 0m) - (received ?? 0m);
            built.Add(new FinanceReceivableStatementLineDto
            {
                LineType = ev.LineType,
                Date = ev.BusinessDate,
                DocNo = ev.DocNo,
                Summary = ev.Summary,
                IncreaseAmount = increase,
                ReceivedAmount = received,
                Balance = balance,
                ReceivableId = ev.ReceivableId,
                StockOutId = ev.StockOutId,
                ReceiptId = ev.ReceiptId,
                WriteOffId = ev.WriteOffId
            });
        }

        lines = built;
        return new FinanceReceivableStatementHeaderDto
        {
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            GeneratedOn = generatedOn,
            AgingCutoff = agingCutoff,
            Currency = currency,
            Opening = opening,
            PeriodIncrease = periodIncrease,
            PeriodReceived = periodReceived,
            Ending = ending
        };
    }
}
