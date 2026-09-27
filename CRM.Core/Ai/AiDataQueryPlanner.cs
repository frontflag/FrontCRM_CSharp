using System.Globalization;
using System.Text.RegularExpressions;
using CRM.Core.Models.Ai;

namespace CRM.Core.Ai;

/// <summary>
/// 把一句话收成白名单意图和槽位。缺必填槽时只追问，不表示可以查数。
/// </summary>
public static class AiDataQueryPlanner
{
    private static readonly Regex AgePattern = new(
        @"超过\s*(\d+)\s*天|(\d+)\s*天以上|库龄\s*(\d+)",
        RegexOptions.Compiled);

    private static readonly Regex TopPattern = new(
        @"Top\s*(\d+)|前\s*(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BareNumber = new(@"^\d{1,4}$", RegexOptions.Compiled);

    public static AiDataQueryPlan Plan(string? question, AiDataQueryState? previous)
    {
        var text = (question ?? string.Empty).Trim();
        var detected = DetectIntent(text);
        var state = detected != null && !string.Equals(detected, previous?.Intent, StringComparison.Ordinal)
            ? new AiDataQueryState { Intent = detected }
            : Clone(previous);

        if (detected != null)
            state.Intent = detected;

        var filledPending = TryFillPending(state, text);
        var extracted = Extract(state, text);
        if (state.PendingSlot != null && !filledPending && detected == null && !extracted)
            state.MissCount++;
        else if (filledPending || detected != null || extracted)
            state.MissCount = 0;

        if (string.IsNullOrWhiteSpace(state.Intent))
        {
            return new AiDataQueryPlan
            {
                Kind = AiDataQueryKinds.Unsupported,
                Summary = "这个数还不能查。",
                State = state
            };
        }

        if (string.Equals(state.Metric, "profit", StringComparison.Ordinal))
        {
            state.Metric = null;
            state.PendingSlot = AiDataQuerySlotNames.Metric;
            state.MissCount = 0;
            return Clarify(
                state,
                "出库毛利按期间合计还不能查。请改选出库金额或订单金额。",
                MetricOptions(state.Intent!, includeProfit: false));
        }

        var missing = NextMissing(state);
        if (missing == null)
        {
            state.PendingSlot = null;
            state.MissCount = 0;
            return new AiDataQueryPlan
            {
                Kind = AiDataQueryKinds.Result,
                Summary = string.Empty,
                State = state
            };
        }

        state.PendingSlot = missing;
        var retry = state.MissCount >= 2;
        return Clarify(state, QuestionFor(state, missing, retry), OptionsFor(state, missing));
    }

    private static AiDataQueryPlan Clarify(
        AiDataQueryState state,
        string summary,
        IReadOnlyList<AiDataQueryOptionDto> options) =>
        new()
        {
            Kind = AiDataQueryKinds.Clarify,
            Summary = summary,
            State = state,
            Options = options
        };

    public static string? DetectIntent(string text)
    {
        if (text.Contains("积压", StringComparison.Ordinal))
            return AiDataQueryIntents.Backlog;
        if (text.Contains("待收款", StringComparison.Ordinal) || text.Contains("待核销", StringComparison.Ordinal))
            return AiDataQueryIntents.ReceivableTotal;
        if (text.Contains("有应收", StringComparison.Ordinal))
            return AiDataQueryIntents.ReceivableCustomers;
        if (text.Contains("库存金额", StringComparison.Ordinal))
            return AiDataQueryIntents.InventoryAmount;
        if (text.Contains("卖得最好", StringComparison.Ordinal)
            || text.Contains("卖最好", StringComparison.Ordinal)
            || TopPattern.IsMatch(text))
            return AiDataQueryIntents.TopBrands;
        if (text.Contains("订单趋势", StringComparison.Ordinal))
            return AiDataQueryIntents.OrderTrend;
        if (text.Contains("销售趋势", StringComparison.Ordinal)
            || (text.Contains('趋') && text.Contains("销售", StringComparison.Ordinal)))
            return AiDataQueryIntents.SalesTrend;
        if (text.Contains("业绩", StringComparison.Ordinal) && text.Contains('趋'))
            return AiDataQueryIntents.SalesTrend;
        if (text.Contains("业绩", StringComparison.Ordinal))
            return AiDataQueryIntents.Performance;
        if (IsNewOrderAsk(text))
            return AiDataQueryIntents.Performance;
        if (text.Contains('趋'))
            return AiDataQueryIntents.SalesTrend;
        return null;
    }

    private static bool IsNewOrderAsk(string text)
    {
        if (text.Contains('趋') || !text.Contains("订单", StringComparison.Ordinal))
            return false;
        return text.Contains("多少条", StringComparison.Ordinal)
            || text.Contains("几条", StringComparison.Ordinal)
            || text.Contains("多少张", StringComparison.Ordinal)
            || text.Contains("条数", StringComparison.Ordinal)
            || text.Contains("新建", StringComparison.Ordinal);
    }

    private static string? NextMissing(AiDataQueryState state)
    {
        switch (state.Intent)
        {
            case AiDataQueryIntents.Performance:
                if (string.IsNullOrEmpty(state.Metric)) return AiDataQuerySlotNames.Metric;
                if (string.IsNullOrEmpty(state.Period)) return AiDataQuerySlotNames.Period;
                return null;
            case AiDataQueryIntents.SalesTrend:
                if (string.IsNullOrEmpty(state.Metric)) return AiDataQuerySlotNames.Metric;
                if (string.IsNullOrEmpty(state.Period)) return AiDataQuerySlotNames.Period;
                return null;
            case AiDataQueryIntents.OrderTrend:
                if (string.IsNullOrEmpty(state.Metric)) return AiDataQuerySlotNames.Metric;
                if (string.IsNullOrEmpty(state.Period)) return AiDataQuerySlotNames.Period;
                return null;
            case AiDataQueryIntents.TopBrands:
                if (string.IsNullOrEmpty(state.Measure)) return AiDataQuerySlotNames.Measure;
                if (string.IsNullOrEmpty(state.Basis)) return AiDataQuerySlotNames.Basis;
                if (state.TopN is null or < 1) return AiDataQuerySlotNames.TopN;
                if (string.IsNullOrEmpty(state.Period)) return AiDataQuerySlotNames.Period;
                return null;
            case AiDataQueryIntents.Backlog:
                if (state.AgeDays is null or < 1) return AiDataQuerySlotNames.AgeDays;
                if (string.IsNullOrEmpty(state.Measure)) return AiDataQuerySlotNames.Measure;
                if (string.IsNullOrEmpty(state.Dimension)) return AiDataQuerySlotNames.Dimension;
                return null;
            case AiDataQueryIntents.InventoryAmount:
            case AiDataQueryIntents.ReceivableTotal:
            case AiDataQueryIntents.ReceivableCustomers:
                return null;
            default:
                return AiDataQuerySlotNames.Metric;
        }
    }

    private static string QuestionFor(AiDataQueryState state, string slot, bool retry)
    {
        if (retry)
            return "还是没对上可查的条件。请从下面选一个，也可以直接输入。";

        return slot switch
        {
            AiDataQuerySlotNames.Metric when state.Intent == AiDataQueryIntents.OrderTrend
                => "订单趋势看金额还是单量？",
            AiDataQuerySlotNames.Metric when state.Intent == AiDataQueryIntents.SalesTrend
                => "销售趋势看出库金额、订单金额，还是单量？",
            AiDataQuerySlotNames.Metric => "业绩看出库金额、订单金额，还是毛利？",
            AiDataQuerySlotNames.Period => "看哪一段时间？",
            AiDataQuerySlotNames.Measure => "按金额还是按数量？",
            AiDataQuerySlotNames.Basis => "按已完成出库，还是按下单？",
            AiDataQuerySlotNames.AgeDays => "库龄超过多少天算积压？可以选下面的天数，也可以直接输入天数。",
            AiDataQuerySlotNames.Dimension => "看品牌、客户，还是两个都看？",
            AiDataQuerySlotNames.TopN => "看前几名？",
            _ => "还缺一个条件才能查。"
        };
    }

    private static IReadOnlyList<AiDataQueryOptionDto> OptionsFor(AiDataQueryState state, string slot) =>
        slot switch
        {
            AiDataQuerySlotNames.Metric => MetricOptions(state.Intent!, includeProfit: state.Intent == AiDataQueryIntents.Performance),
            AiDataQuerySlotNames.Period => PeriodOptions(),
            AiDataQuerySlotNames.Measure => new[]
            {
                Opt("amount", "按金额"),
                Opt("qty", "按数量")
            },
            AiDataQuerySlotNames.Basis => new[]
            {
                Opt("stockOut", "按出库"),
                Opt("order", "按下单")
            },
            AiDataQuerySlotNames.AgeDays => new[]
            {
                Opt("30", "30天"),
                Opt("60", "60天"),
                Opt("90", "90天"),
                Opt("180", "180天")
            },
            AiDataQuerySlotNames.Dimension => new[]
            {
                Opt("brand", "品牌"),
                Opt("customer", "客户"),
                Opt("both", "品牌和客户")
            },
            AiDataQuerySlotNames.TopN => new[]
            {
                Opt("5", "前5"),
                Opt("10", "前10")
            },
            _ => Array.Empty<AiDataQueryOptionDto>()
        };

    private static IReadOnlyList<AiDataQueryOptionDto> MetricOptions(string intent, bool includeProfit)
    {
        if (intent == AiDataQueryIntents.OrderTrend)
        {
            return new[]
            {
                Opt("orderAmount", "订单金额"),
                Opt("orderCount", "单量")
            };
        }

        var list = new List<AiDataQueryOptionDto>
        {
            Opt("stockOutAmount", "出库金额"),
            Opt("orderAmount", "订单金额")
        };
        if (intent == AiDataQueryIntents.SalesTrend)
            list.Add(Opt("orderCount", "单量"));
        if (includeProfit)
            list.Add(Opt("profit", "毛利"));
        return list;
    }

    private static IReadOnlyList<AiDataQueryOptionDto> PeriodOptions() => new[]
    {
        Opt("today", "今天"),
        Opt("thisMonth", "本月"),
        Opt("lastMonth", "上月"),
        Opt("last3Months", "近三个月"),
        Opt("last6Months", "近半年")
    };

    private static bool TryFillPending(AiDataQueryState state, string text)
    {
        if (string.IsNullOrEmpty(state.PendingSlot) || string.IsNullOrEmpty(state.Intent))
            return false;

        var options = OptionsFor(state, state.PendingSlot);
        foreach (var option in options)
        {
            if (!text.Contains(option.Label, StringComparison.Ordinal)
                && !string.Equals(text, option.Id, StringComparison.OrdinalIgnoreCase))
                continue;
            ApplyOption(state, state.PendingSlot, option.Id);
            return true;
        }

        if (state.PendingSlot == AiDataQuerySlotNames.AgeDays && TryReadDays(text, out var days))
        {
            state.AgeDays = days;
            return true;
        }

        if (state.PendingSlot == AiDataQuerySlotNames.TopN && TryReadTop(text, out var top))
        {
            state.TopN = top;
            return true;
        }

        return false;
    }

    private static void ApplyOption(AiDataQueryState state, string slot, string id)
    {
        switch (slot)
        {
            case AiDataQuerySlotNames.Metric:
                state.Metric = id;
                break;
            case AiDataQuerySlotNames.Period:
                state.Period = id;
                break;
            case AiDataQuerySlotNames.Measure:
                state.Measure = id;
                break;
            case AiDataQuerySlotNames.Basis:
                state.Basis = id;
                break;
            case AiDataQuerySlotNames.Dimension:
                state.Dimension = id;
                break;
            case AiDataQuerySlotNames.AgeDays:
                if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days))
                    state.AgeDays = days;
                break;
            case AiDataQuerySlotNames.TopN:
                if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var top))
                    state.TopN = top;
                break;
        }
    }

    /// <returns>这句话里是否写出了新的槽位。</returns>
    private static bool Extract(AiDataQueryState state, string text)
    {
        var before = Snapshot(state);
        if (text.Contains("出库金额", StringComparison.Ordinal))
            state.Metric = "stockOutAmount";
        else if (text.Contains("订单金额", StringComparison.Ordinal))
            state.Metric = "orderAmount";
        else if (text.Contains("毛利", StringComparison.Ordinal))
            state.Metric = "profit";
        else if (text.Contains("单量", StringComparison.Ordinal)
                 || text.Contains("订单数", StringComparison.Ordinal)
                 || text.Contains("笔数", StringComparison.Ordinal))
            state.Metric = "orderCount";

        var asksCount = text.Contains("多少条", StringComparison.Ordinal)
            || text.Contains("几条", StringComparison.Ordinal)
            || text.Contains("多少张", StringComparison.Ordinal)
            || text.Contains("条数", StringComparison.Ordinal);
        var asksOrderMoney = text.Contains("金额", StringComparison.Ordinal)
            && !text.Contains("出库金额", StringComparison.Ordinal)
            && !text.Contains("库存金额", StringComparison.Ordinal)
            && !text.Contains("按金额", StringComparison.Ordinal);
        if (asksCount && asksOrderMoney)
            state.Metric = "orderCountAndAmount";
        else if (asksCount && string.IsNullOrEmpty(state.Metric))
            state.Metric = "orderCount";

        if (text.Contains("按数量", StringComparison.Ordinal) || text.Contains("按件数", StringComparison.Ordinal))
            state.Measure = "qty";
        else if (text.Contains("按金额", StringComparison.Ordinal))
            state.Measure = "amount";

        if (state.Intent == AiDataQueryIntents.TopBrands)
        {
            if (text.Contains("按下单", StringComparison.Ordinal) || text.Contains("下单", StringComparison.Ordinal))
                state.Basis = "order";
            else if (text.Contains("按出库", StringComparison.Ordinal) || text.Contains("出库", StringComparison.Ordinal))
                state.Basis = "stockOut";
        }

        if (state.Intent == AiDataQueryIntents.Backlog && TryReadDays(text, out var days))
            state.AgeDays = days;
        if (state.Intent == AiDataQueryIntents.TopBrands && TryReadTop(text, out var top))
            state.TopN = top;

        var period = ReadPeriod(text);
        if (period != null)
            state.Period = period;

        var hasBrand = text.Contains("品牌", StringComparison.Ordinal);
        var hasCustomer = text.Contains("客户", StringComparison.Ordinal);
        if (state.Intent == AiDataQueryIntents.Backlog || state.Intent == AiDataQueryIntents.TopBrands)
        {
            if (hasBrand && hasCustomer)
                state.Dimension = "both";
            else if (hasBrand)
                state.Dimension = "brand";
            else if (hasCustomer && state.Intent == AiDataQueryIntents.Backlog)
                state.Dimension = "customer";
        }

        if (state.Intent == AiDataQueryIntents.TopBrands && string.IsNullOrEmpty(state.Dimension))
            state.Dimension = "brand";

        return Snapshot(state) != before;
    }

    private static string? ReadPeriod(string text)
    {
        if (text.Contains("今天", StringComparison.Ordinal) || text.Contains("今日", StringComparison.Ordinal))
            return "today";
        if (text.Contains("上月", StringComparison.Ordinal) || text.Contains("上个月", StringComparison.Ordinal))
            return "lastMonth";
        if (text.Contains("近三个月", StringComparison.Ordinal)
            || text.Contains("近3个月", StringComparison.Ordinal)
            || text.Contains("最近三个月", StringComparison.Ordinal))
            return "last3Months";
        if (text.Contains("近半年", StringComparison.Ordinal)
            || text.Contains("近六个月", StringComparison.Ordinal)
            || text.Contains("近6个月", StringComparison.Ordinal))
            return "last6Months";
        if (text.Contains("本月", StringComparison.Ordinal) || text.Contains("这个月", StringComparison.Ordinal))
            return "thisMonth";
        return null;
    }

    private static bool TryReadDays(string text, out int days)
    {
        days = 0;
        var match = AgePattern.Match(text);
        if (match.Success)
        {
            var raw = FirstGroup(match);
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out days) && days is >= 1 and <= 3650)
                return true;
        }

        if (BareNumber.IsMatch(text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out days)
            && days is >= 1 and <= 3650)
            return true;
        days = 0;
        return false;
    }

    private static bool TryReadTop(string text, out int top)
    {
        top = 0;
        var match = TopPattern.Match(text);
        if (!match.Success)
            return false;
        var raw = FirstGroup(match);
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out top))
            return false;
        if (top < 1) return false;
        if (top > 10) top = 10;
        return true;
    }

    private static string FirstGroup(Match match)
    {
        for (var i = 1; i < match.Groups.Count; i++)
        {
            if (match.Groups[i].Success)
                return match.Groups[i].Value;
        }
        return string.Empty;
    }

    private static string Snapshot(AiDataQueryState state) =>
        string.Join('|', state.Metric, state.Measure, state.Basis, state.AgeDays, state.Dimension, state.TopN, state.Period);

    private static AiDataQueryState Clone(AiDataQueryState? source) =>
        source == null
            ? new AiDataQueryState()
            : new AiDataQueryState
            {
                Intent = source.Intent,
                Metric = source.Metric,
                Measure = source.Measure,
                Basis = source.Basis,
                AgeDays = source.AgeDays,
                Dimension = source.Dimension,
                TopN = source.TopN,
                Period = source.Period,
                PendingSlot = source.PendingSlot,
                MissCount = source.MissCount
            };

    private static AiDataQueryOptionDto Opt(string id, string label) => new() { Id = id, Label = label };
}
