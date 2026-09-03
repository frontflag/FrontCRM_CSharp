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
        IReadOnlyList<string> ReceiptCodes,
        string? StockOutCode = null);

    public readonly record struct StockOutHint(
        string StockOutCode,
        short Status,
        IReadOnlyList<ReceivableHint> Receivables);

    public static string ReceivableDisplayCode(ReceivableHint ar) =>
        OpsCheckDocumentCodes.ForSuggestion(ar.ReceivableCode, ar.StockOutCode);

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
            var arLabel = OpsCheckDocumentCodes.ForSuggestion(receivableCode);
            yield return $"打开「应收款详情」{arLabel}，在核销记录中打开已关联的收款单，点「反核销」并输入该收款单号确认。";
            yield break;
        }

        foreach (var code in codes)
            yield return $"打开「收款单」{code}，点「反核销」，输入 {code} 确认。";
    }

    public static string VoidReceivable(ReceivableHint ar)
    {
        if (OpsCheckDocumentCodes.IsUsableCode(ar.ReceivableCode))
        {
            var code = ar.ReceivableCode.Trim();
            return $"打开「应收款详情」{code}，点「作废应收」，输入 {code} 确认。";
        }

        if (OpsCheckDocumentCodes.IsUsableCode(ar.StockOutCode))
            return $"打开「出库单详情」{ar.StockOutCode!.Trim()}，在应收面板作废该笔应收（当前应收单号缺失，无法按单号输入确认）。";

        return "打开本行应收款详情作废该笔应收（当前应收单号缺失，请联系管理员补全单号）。";
    }

    public static string ForceDeleteStockOut(string stockOutCode, bool openList) =>
        openList
            ? $"打开「出库单列表」，对 {stockOutCode} 点「强制删除」，输入 {stockOutCode} 确认。"
            : $"对 {stockOutCode} 点「强制删除」，输入 {stockOutCode} 确认。";

    public static string RefreshPacking(string packingCode, bool thisRow) =>
        thisRow
            ? $"点本行 {packingCode} 打开「装箱单详情」，点击「刷新」。"
            : $"打开「装箱单详情」{packingCode}，点击「刷新」。";

    public static string ReOutboundByPacking(string? packingCode)
    {
        var code = packingCode?.Trim();
        if (string.IsNullOrEmpty(code))
            return "按装箱流程重新出库。";
        return $"打开「装箱单详情」{code}，按装箱流程重新出库。";
    }

    public static string MarkFinished(string stockOutCode, bool openList) =>
        openList
            ? $"打开「出库单列表」，对 {stockOutCode} 点「标记完成」。"
            : $"对 {stockOutCode} 点「标记完成」。";

    public static string RefreshCustomer(string? sellOrderCode) =>
        string.IsNullOrWhiteSpace(sellOrderCode)
            ? "打开本行关联的「销售订单详情」，点击页头「刷新」右侧下拉「刷新客户」并确认。"
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
            steps.Add($"没有一键修复：{ReOutboundByPacking(packingCode)}。");
            return JoinSteps(steps);
        }

        foreach (var so in stockOuts)
            steps.Add($"打开「出库单详情」{so.StockOutCode}，核对本行是否挂了拣货。");
        AppendForceDeleteStockOuts(steps, stockOuts);
        steps.Add(ReOutboundByPacking(packingCode));
        return JoinSteps(steps);
    }

    public static bool IsExpectedMissingReceivable(MissingReceivableDiagnosis? diagnosis) =>
        diagnosis?.Cause == MissingReceivableCause.ZeroPrice;

    public static string SalesDoneNoReceivable(
        string stockOutCode,
        MissingReceivableDiagnosis? diagnosis = null,
        string? packingCode = null,
        bool includeAdminDebugSuggestions = false)
    {
        if (diagnosis?.Cause == MissingReceivableCause.NoSellLineLink)
        {
            return JoinSteps(new[]
            {
                $"打开「出库单详情」{stockOutCode}，确认出库明细已关联销售订单明细。",
                ForceDeleteStockOut(stockOutCode, openList: true),
                $"{ReOutboundByPacking(packingCode)}，并在新出库单上「标记完成」。"
            });
        }

        return JoinSteps(new[]
        {
            $"打开「出库单详情」{stockOutCode}，确认是销售出库且状态为「完成」、应收面板为空。",
            ForceDeleteStockOut(stockOutCode, openList: true),
            ReOutboundByPacking(packingCode),
            "在「出库单列表」对新出库单点「标记完成」以生成应收（原单若仍是完成态，重复标记无效）。"
        });
    }

    public enum MissingReceivableCause
    {
        Unknown,
        NoSellLineLink,
        ZeroQty,
        ZeroPrice
    }

    public readonly record struct MissingReceivableDiagnosis(
        MissingReceivableCause Cause,
        string ReasonSuffix,
        string? SellOrderCode = null);

    public static string VoidReceivableChain(ReceivableHint ar) =>
        JoinSteps(ReverseWriteOffSteps(ReceivableDisplayCode(ar), ar.VerifiedDone, ar.ReceiptCodes)
            .Append(VoidReceivable(ar)));

    public static string VoidThenRebuild(ReceivableHint ar, string stockOutCode, string? packingCode = null) =>
        JoinSteps(ReverseWriteOffSteps(ReceivableDisplayCode(ar), ar.VerifiedDone, ar.ReceiptCodes)
            .Append(ForceDeleteStockOut(stockOutCode, openList: true))
            .Append($"{ReOutboundByPacking(packingCode)}，在「出库单列表」对新出库单点「标记完成」。"));

    public static string NotifyNotStockedOut(
        string notifyCode,
        string? packingCode,
        IReadOnlyList<StockOutHint> stockOuts)
    {
        var unfinished = stockOuts.Where(s => s.Status != 4).ToList();
        if (unfinished.Count == 0)
        {
            var doneSteps = new List<string>();
            if (!string.IsNullOrWhiteSpace(packingCode))
                doneSteps.Add(RefreshPacking(packingCode.Trim(), thisRow: false));
            else
            {
                var soCodes = stockOuts
                    .Select(s => s.StockOutCode?.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var soText = soCodes.Count > 0
                    ? string.Join("、", soCodes)
                    : "关联出库单";
                doneSteps.Add(
                    $"出库单 {soText} 已完成。打开「出库通知」{notifyCode} 核对；请通过关联装箱单详情点击「刷新」同步通知状态。");
            }
            return JoinSteps(doneSteps);
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
        if (ar is { } written && IsWrittenOff(written.VerifiedDone))
            steps.AddRange(ReverseWriteOffSteps(ReceivableDisplayCode(written), written.VerifiedDone, written.ReceiptCodes));

        var refreshStep = RefreshCustomer(sellOrderCode);
        if (ar is { } hint && !IsWrittenOff(hint.VerifiedDone))
            refreshStep = $"{refreshStep}（会同步未核销应收 {ReceivableDisplayCode(hint)} 的客户）";
        steps.Add(refreshStep);
        return JoinSteps(steps);
    }

    public static List<string> DistinctCodes(IReadOnlyList<string>? codes) =>
        OpsCheckDocumentCodes.FilterCodes(codes);

    private static void AppendForceDeleteStockOuts(List<string> steps, IReadOnlyList<StockOutHint> stockOuts)
    {
        var opened = false;
        foreach (var so in stockOuts)
        {
            foreach (var ar in so.Receivables)
                steps.AddRange(ReverseWriteOffSteps(ReceivableDisplayCode(ar), ar.VerifiedDone, ar.ReceiptCodes));
            steps.Add(ForceDeleteStockOut(so.StockOutCode, openList: !opened));
            opened = true;
        }
    }
}
