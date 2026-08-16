namespace CRM.Core.Utilities;

/// <summary>出库运维检查「运维操作建议」：按本行单据生成编号步骤。</summary>
public static class StockOutOpsCheckSuggestions
{
    private static readonly string[] Circles =
    {
        "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩",
        "⑪", "⑫", "⑬", "⑭", "⑮", "⑯", "⑰", "⑱", "⑲", "⑳"
    };

    public readonly record struct ReceivableHint(
        string ReceivableCode,
        decimal VerifiedDone,
        IReadOnlyList<string> ReceiptCodes);

    public readonly record struct StockOutHint(
        string StockOutCode,
        short Status,
        IReadOnlyList<ReceivableHint> Receivables);

    public static string JoinSteps(IEnumerable<string> steps)
    {
        var list = steps
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();
        if (list.Count == 0)
            return string.Empty;
        return string.Join("\n", list.Select((s, i) =>
            i < Circles.Length ? $"{Circles[i]} {s}" : $"{i + 1}. {s}"));
    }

    public static bool IsWrittenOff(decimal verifiedDone) => verifiedDone > 0m;

    public static IEnumerable<string> ReverseWriteOffSteps(
        string receivableCode,
        decimal verifiedDone,
        IReadOnlyList<string>? receiptCodes)
    {
        if (!IsWrittenOff(verifiedDone))
            yield break;

        var codes = DistinctCodes(receiptCodes);
        if (codes.Count == 0)
        {
            yield return $"打开「应收款详情」{receivableCode}，在核销记录打开对应收款单，点「反核销」。";
            yield break;
        }

        foreach (var code in codes)
            yield return $"打开「收款单」{code}，点「反核销」，输入 {code} 确认。";
    }

    public static string VoidReceivable(string arCode) =>
        $"打开「应收款详情」{arCode}，点「作废应收」，输入 {arCode} 确认。";

    public static string ForceDeleteStockOut(string stockOutCode, bool openList) =>
        openList
            ? $"打开「出库单列表」，对 {stockOutCode} 点「强制删除」，输入 {stockOutCode} 确认。"
            : $"对 {stockOutCode} 点「强制删除」，输入 {stockOutCode} 确认。";

    public static string RefreshPacking(string packingCode, bool thisRow) =>
        thisRow
            ? $"点本行 {packingCode} 打开「装箱单详情」，点击「刷新」。"
            : $"打开「装箱单详情」{packingCode}，点击「刷新」。";

    public static string MarkFinished(string stockOutCode, bool openList) =>
        openList
            ? $"打开「出库单列表」，对 {stockOutCode} 点「标记完成」。"
            : $"对 {stockOutCode} 点「标记完成」。";

    public static string RefreshCustomer(string? sellOrderCode) =>
        string.IsNullOrWhiteSpace(sellOrderCode)
            ? "打开对应「销售订单详情」，点击页头「刷新」右侧下拉「刷新客户」并确认。"
            : $"打开「销售订单详情」{sellOrderCode.Trim()}，点击页头「刷新」右侧下拉「刷新客户」并确认。";

    public static string PackingFinishedNoStockOut(string packingCode) =>
        JoinSteps(new[] { RefreshPacking(packingCode, thisRow: true) });

    public static string PackingHasStockOutNotFinished(string packingCode) =>
        JoinSteps(new[] { RefreshPacking(packingCode, thisRow: true) });

    public static string DuplicateKeepSmallest(
        string packingCode,
        bool thisRowIsPacking,
        IReadOnlyList<StockOutHint> extras)
    {
        var steps = new List<string>();
        AppendForceDeleteStockOuts(steps, extras);
        steps.Add(RefreshPacking(packingCode, thisRowIsPacking));
        return JoinSteps(steps);
    }

    public static string PackingItemUnlinked(
        string packingCode,
        string itemCode,
        IReadOnlyList<StockOutHint> stockOuts)
    {
        var steps = new List<string>
        {
            $"打开「装箱单详情」{packingCode}，核对本行装箱明细 {itemCode}。"
        };
        if (stockOuts.Count == 0)
        {
            steps.Add("没有一键修复：按装箱流程重新出库。");
            return JoinSteps(steps);
        }

        foreach (var so in stockOuts)
            steps.Add($"打开「出库单详情」{so.StockOutCode}，核对本行是否挂了拣货。");
        AppendForceDeleteStockOuts(steps, stockOuts);
        steps.Add("按装箱流程重新出库。");
        return JoinSteps(steps);
    }

    public static string SalesDoneNoReceivable(string stockOutCode) =>
        JoinSteps(new[]
        {
            $"打开「出库单详情」{stockOutCode}，确认是销售出库且已完成。",
            ForceDeleteStockOut(stockOutCode, openList: true),
            "按装箱重新出库。",
            "完成后在「出库单列表」对新出库单点「标记完成」，以生成应收。"
        });

    public static string VoidReceivableChain(ReceivableHint ar) =>
        JoinSteps(ReverseWriteOffSteps(ar.ReceivableCode, ar.VerifiedDone, ar.ReceiptCodes)
            .Append(VoidReceivable(ar.ReceivableCode)));

    public static string VoidThenRebuild(ReceivableHint ar, string stockOutCode) =>
        JoinSteps(ReverseWriteOffSteps(ar.ReceivableCode, ar.VerifiedDone, ar.ReceiptCodes)
            .Append(VoidReceivable(ar.ReceivableCode))
            .Append(ForceDeleteStockOut(stockOutCode, openList: true))
            .Append("按装箱重出后，在「出库单列表」对新出库单点「标记完成」。"));

    public static string NotifyNotStockedOut(string notifyCode, IReadOnlyList<StockOutHint> stockOuts)
    {
        var unfinished = stockOuts.Where(s => s.Status != 4).ToList();
        if (unfinished.Count == 0)
        {
            var soText = stockOuts.Count == 0
                ? "出库单"
                : string.Join("、", stockOuts.Select(s => s.StockOutCode));
            return JoinSteps(new[]
            {
                $"{soText} 已完成。打开「出库通知」{notifyCode} 核对状态；当前没有单独改通知状态的按钮。"
            });
        }

        var steps = new List<string>();
        var opened = false;
        foreach (var so in unfinished)
        {
            steps.Add(MarkFinished(so.StockOutCode, openList: !opened));
            opened = true;
        }

        return JoinSteps(steps);
    }

    public static string CustomerRefresh(string? sellOrderCode, ReceivableHint? ar)
    {
        var steps = new List<string>();
        if (ar is { } written)
            steps.AddRange(ReverseWriteOffSteps(written.ReceivableCode, written.VerifiedDone, written.ReceiptCodes));
        steps.Add(RefreshCustomer(sellOrderCode));
        return JoinSteps(steps);
    }

    public static List<string> DistinctCodes(IReadOnlyList<string>? codes) =>
        (codes ?? Array.Empty<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static void AppendForceDeleteStockOuts(List<string> steps, IReadOnlyList<StockOutHint> stockOuts)
    {
        var opened = false;
        foreach (var so in stockOuts)
        {
            foreach (var ar in so.Receivables)
                steps.AddRange(ReverseWriteOffSteps(ar.ReceivableCode, ar.VerifiedDone, ar.ReceiptCodes));
            steps.Add(ForceDeleteStockOut(so.StockOutCode, openList: !opened));
            opened = true;
        }
    }
}
