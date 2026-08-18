using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class StockOutOpsCheckSuggestionsTests
{
    [Fact]
    public void Duplicate_Unverified_KeepsSmallest_NoReverse()
    {
        var extras = new[]
        {
            Hint("STO00222", verified: false),
            Hint("STO00223", verified: false),
            Hint("STO0022M", verified: false)
        };

        var text = StockOutOpsCheckSuggestions.DuplicateKeepSmallest("PAK0020C", thisRowIsPacking: true, extras);

        Assert.Contains("① 打开「出库单列表」，对 STO00222 点「强制删除」，输入 STO00222 确认。", text);
        Assert.Contains("② 对 STO00223 点「强制删除」，输入 STO00223 确认。", text);
        Assert.Contains("③ 对 STO0022M 点「强制删除」，输入 STO0022M 确认。", text);
        Assert.Contains("④ 点本行 PAK0020C 打开「装箱单详情」，点击「刷新」。", text);
        Assert.DoesNotContain("反核销", text);
        Assert.DoesNotContain("STO00221", text);
    }

    [Fact]
    public void Duplicate_VerifiedExtra_NamesReceiptThenForceDelete()
    {
        var extras = new[]
        {
            Hint("STO00222", verified: true, receipts: new[] { "FRC0001A" })
        };

        var text = StockOutOpsCheckSuggestions.DuplicateKeepSmallest("PAK0020C", thisRowIsPacking: true, extras);

        Assert.Contains("① 打开「收款单」FRC0001A，点「反核销」，输入 FRC0001A 确认。", text);
        Assert.Contains("② 打开「出库单列表」，对 STO00222 点「强制删除」，输入 STO00222 确认。", text);
        Assert.Contains("③ 点本行 PAK0020C 打开「装箱单详情」，点击「刷新」。", text);
    }

    [Fact]
    public void VoidReceivable_Unverified_NoReverse()
    {
        var ar = new StockOutOpsCheckSuggestions.ReceivableHint("ARV00007", 0m, Array.Empty<string>());
        var text = StockOutOpsCheckSuggestions.VoidReceivableChain(ar);

        Assert.Equal("① 打开「应收款详情」ARV00007，点「作废应收」，输入 ARV00007 确认。", text);
        Assert.DoesNotContain("反核销", text);
    }

    [Fact]
    public void VoidReceivable_Verified_NamesReceipt()
    {
        var ar = new StockOutOpsCheckSuggestions.ReceivableHint("ARV00008", 100m, new[] { "FRC0009B" });
        var text = StockOutOpsCheckSuggestions.VoidReceivableChain(ar);

        Assert.Contains("① 打开「收款单」FRC0009B，点「反核销」，输入 FRC0009B 确认。", text);
        Assert.Contains("② 打开「应收款详情」ARV00008，点「作废应收」，输入 ARV00008 确认。", text);
    }

    [Fact]
    public void ReverseWriteOffSteps_Unverified_Empty()
    {
        Assert.Empty(StockOutOpsCheckSuggestions.ReverseWriteOffSteps("ARV1", 0m, new[] { "FRC1" }));
    }

    [Fact]
    public void VoidThenRebuild_Unverified_ForceDeletesWithoutVoid()
    {
        var ar = new StockOutOpsCheckSuggestions.ReceivableHint("ARV00007", 0m, Array.Empty<string>());
        var text = StockOutOpsCheckSuggestions.VoidThenRebuild(ar, "STO0020X");

        Assert.Contains("① 打开「出库单列表」，对 STO0020X 点「强制删除」，输入 STO0020X 确认。", text);
        Assert.Contains("标记完成", text);
        Assert.DoesNotContain("作废应收", text);
        Assert.DoesNotContain("反核销", text);
    }

    [Fact]
    public void VoidThenRebuild_Verified_ReverseThenForceDelete()
    {
        var ar = new StockOutOpsCheckSuggestions.ReceivableHint("ARV00008", 100m, new[] { "FRC0009B" });
        var text = StockOutOpsCheckSuggestions.VoidThenRebuild(ar, "STO0020X");

        Assert.Contains("① 打开「收款单」FRC0009B，点「反核销」，输入 FRC0009B 确认。", text);
        Assert.Contains("② 打开「出库单列表」，对 STO0020X 点「强制删除」，输入 STO0020X 确认。", text);
        Assert.DoesNotContain("作废应收", text);
    }

    [Fact]
    public void JoinSteps_UsesCircledNumbersAndNewlines()
    {
        var text = StockOutOpsCheckSuggestions.JoinSteps(new[] { "第一步", "第二步" });
        Assert.Equal("① 第一步\n② 第二步", text);
    }

    private static StockOutOpsCheckSuggestions.StockOutHint Hint(
        string code,
        bool verified,
        IReadOnlyList<string>? receipts = null) =>
        new(
            code,
            4,
            new[]
            {
                new StockOutOpsCheckSuggestions.ReceivableHint(
                    "AR" + code,
                    verified ? 10m : 0m,
                    receipts ?? Array.Empty<string>())
            });
}
