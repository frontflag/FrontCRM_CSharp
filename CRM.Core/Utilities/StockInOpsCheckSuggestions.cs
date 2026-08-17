namespace CRM.Core.Utilities;

/// <summary>入库运维检查「运维操作建议」：按本行单据生成编号步骤。</summary>
public static class StockInOpsCheckSuggestions
{
    private static readonly string[] Circles =
    {
        "①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨", "⑩",
        "⑪", "⑫", "⑬", "⑭", "⑮", "⑯", "⑰", "⑱", "⑲", "⑳"
    };

    public readonly record struct StockInHint(
        string StockInCode,
        IReadOnlyList<string> InvoiceCodes);

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

    public static List<string> DistinctCodes(IReadOnlyList<string>? codes) =>
        (codes ?? Array.Empty<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IEnumerable<string> ReversePurchaseInvoiceSteps(IReadOnlyList<string>? invoiceCodes)
    {
        foreach (var code in DistinctCodes(invoiceCodes))
            yield return $"打开「进项发票」{code}，点「反核销」，输入 {code} 确认。";
    }

    public static string RefreshArrivalOnDebug() =>
        "打开「Debug 模拟数据」，点「刷新到货通知状态」。";

    public static string ForceDeleteStockIn(string stockInCode, bool openList) =>
        openList
            ? $"打开「入库单列表」，对 {stockInCode} 点「强制删除」，输入 {stockInCode} 确认。"
            : $"对 {stockInCode} 点「强制删除」，输入 {stockInCode} 确认。";

    public static string MarkStockedIn(string stockInCode, bool openList) =>
        openList
            ? $"打开「入库单列表」，对 {stockInCode} 点「标记已入库」。"
            : $"对 {stockInCode} 点「标记已入库」。";

    public static string ForceDeleteStockItem(string stockItemCode, bool openList) =>
        openList
            ? $"打开「库存明细」，对 {stockItemCode} 点「强制删除」，输入 {stockItemCode} 确认。"
            : $"对 {stockItemCode} 点「强制删除」，输入 {stockItemCode} 确认。";

    public static string Notice100NoPosted(IReadOnlyList<string> unpostedStockInCodes) =>
        JoinSteps(MarkStockedInSteps(unpostedStockInCodes).Append(RefreshArrivalOnDebug()));

    public static string PostedButNoticeNot100() =>
        JoinSteps(new[] { RefreshArrivalOnDebug() });

    public static string DuplicateKeepSmallest(IReadOnlyList<StockInHint> extras)
    {
        var steps = new List<string>();
        AppendForceDeleteStockIns(steps, extras);
        steps.Add(RefreshArrivalOnDebug());
        return JoinSteps(steps);
    }

    public static string PostedItemNoStockItem(StockInHint si) =>
        JoinSteps(ReversePurchaseInvoiceSteps(si.InvoiceCodes)
            .Append(ForceDeleteStockIn(si.StockInCode, openList: true))
            .Append("按质检「生成入库」重新入库。"));

    public static string OrphanStockItem(string stockItemCode) =>
        JoinSteps(new[] { ForceDeleteStockItem(stockItemCode, openList: true) });

    public static string DuplicateStockItems(IReadOnlyList<string> extraCodes)
    {
        var steps = new List<string>();
        var opened = false;
        foreach (var code in DistinctCodes(extraCodes))
        {
            steps.Add(ForceDeleteStockItem(code, openList: !opened));
            opened = true;
        }

        return JoinSteps(steps);
    }

    public static string AmountMismatchRebuild(StockInHint si) =>
        JoinSteps(ReversePurchaseInvoiceSteps(si.InvoiceCodes)
            .Append($"打开「入库单详情」{si.StockInCode}，核对本行数量×单价与金额。")
            .Append(ForceDeleteStockIn(si.StockInCode, openList: true))
            .Append("按质检「生成入库」重新入库。"));

    public static string VendorMismatch(string? purchaseOrderCode, string? stockInCode, string? noticeCode)
    {
        var openBits = new List<string>();
        if (!string.IsNullOrWhiteSpace(purchaseOrderCode))
            openBits.Add($"打开「采购订单」{purchaseOrderCode.Trim()}");
        if (!string.IsNullOrWhiteSpace(stockInCode))
            openBits.Add($"打开「入库单详情」{stockInCode.Trim()}");

        var head = openBits.Count > 0
            ? string.Join("，", openBits) + "，核对供应商。"
            : "打开对应采购订单与入库单，核对供应商。";

        if (string.IsNullOrWhiteSpace(noticeCode))
            return JoinSteps(new[] { head });

        return JoinSteps(new[] { head, $"到货通知 {noticeCode.Trim()} 一并核对。" });
    }

    public static string OrphanWriteOff(string invoiceCode) =>
        JoinSteps(ReversePurchaseInvoiceSteps(new[] { invoiceCode }));

    public static string QcStockInStatusLag(string qcCode) =>
        JoinSteps(new[]
        {
            $"打开「质检」{qcCode} 核对该单入库状态；重新进入质检列表后系统会同步。"
        });

    private static IEnumerable<string> MarkStockedInSteps(IReadOnlyList<string> codes)
    {
        var opened = false;
        foreach (var code in DistinctCodes(codes))
        {
            yield return MarkStockedIn(code, openList: !opened);
            opened = true;
        }
    }

    private static void AppendForceDeleteStockIns(List<string> steps, IReadOnlyList<StockInHint> stockIns)
    {
        var opened = false;
        foreach (var si in stockIns)
        {
            steps.AddRange(ReversePurchaseInvoiceSteps(si.InvoiceCodes));
            steps.Add(ForceDeleteStockIn(si.StockInCode, openList: !opened));
            opened = true;
        }
    }
}
