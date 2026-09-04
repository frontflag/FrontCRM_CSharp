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
    public void VoidReceivable_MissingCode_UsesStockOutCode()
    {
        var ar = new StockOutOpsCheckSuggestions.ReceivableHint(
            "",
            0m,
            Array.Empty<string>(),
            "STO0020H");
        var text = StockOutOpsCheckSuggestions.VoidReceivableChain(ar);

        Assert.Contains("STO0020H", text);
        Assert.DoesNotContain("作废应收」，输入", text);
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
        var text = StockOutOpsCheckSuggestions.VoidThenRebuild(ar, "STO0020X", "PAK001ZB");

        Assert.Contains("① 打开「出库单列表」，对 STO0020X 点「强制删除」，输入 STO0020X 确认。", text);
        Assert.Contains("打开「装箱单详情」PAK001ZB，按装箱流程重新出库。", text);
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

    [Fact]
    public void NotifyNotStockedOut_WhenStockOutFinished_SuggestsRefreshPacking()
    {
        var text = StockOutOpsCheckSuggestions.NotifyNotStockedOut(
            "STOR0021C",
            "PAK001ZU",
            new[] { new StockOutOpsCheckSuggestions.StockOutHint("STO0020Y", 4, Array.Empty<StockOutOpsCheckSuggestions.ReceivableHint>()) });

        Assert.Equal("① 打开「装箱单详情」PAK001ZU，点击「刷新」。", text);
        Assert.DoesNotContain("无单独改", text);
    }

    [Fact]
    public void NotifyNotStockedOut_WhenStockOutUnfinished_SuggestsMarkFinished()
    {
        var text = StockOutOpsCheckSuggestions.NotifyNotStockedOut(
            "STOR0021C",
            "PAK001ZU",
            new[] { new StockOutOpsCheckSuggestions.StockOutHint("STO0020Y", 2, Array.Empty<StockOutOpsCheckSuggestions.ReceivableHint>()) });

        Assert.Contains("标记完成", text);
        Assert.DoesNotContain("刷新", text);
    }

    [Fact]
    public void SalesDoneNoReceivable_IncludesPackingCodeInReOutboundStep()
    {
        var text = StockOutOpsCheckSuggestions.SalesDoneNoReceivable("STO0022V", packingCode: "PAK001ZB");

        Assert.Contains("③ 打开「装箱单详情」PAK001ZB，按装箱流程重新出库。", text);
    }

    [Fact]
    public void IsExpectedMissingReceivable_ZeroPrice_PassesOpsCheck()
    {
        var diag = new StockOutOpsCheckSuggestions.MissingReceivableDiagnosis(
            StockOutOpsCheckSuggestions.MissingReceivableCause.ZeroPrice,
            "系统判定：销售行单价为 0",
            "SO0024P");
        Assert.True(StockOutOpsCheckSuggestions.IsExpectedMissingReceivable(diag));
        Assert.False(StockOutOpsCheckSuggestions.IsExpectedMissingReceivable(
            new StockOutOpsCheckSuggestions.MissingReceivableDiagnosis(
                StockOutOpsCheckSuggestions.MissingReceivableCause.Unknown,
                "未生成应收")));
        Assert.False(StockOutOpsCheckSuggestions.IsExpectedMissingReceivable(null));
    }

    [Fact]
    public void PackingItemUnlinked_IncludesPackingCodeWhenReOutbound()
    {
        var text = StockOutOpsCheckSuggestions.PackingItemUnlinked(
            "PAK001ZB",
            "PAK001ZB-1",
            Array.Empty<StockOutOpsCheckSuggestions.StockOutHint>());

        Assert.Contains("打开「装箱单详情」PAK001ZB，按装箱流程重新出库。", text);
    }

    [Fact]
    public void CustomerNameSnapshotRefresh_NamesReceivableAndKeepsPrimaryKey()
    {
        var text = StockOutOpsCheckSuggestions.CustomerNameSnapshotRefresh("SO0020H", "ARV00001");
        Assert.Contains("① 打开「销售订单详情」SO0020H", text);
        Assert.Contains("刷新客户", text);
        Assert.Contains("ARV00001", text);
        Assert.Contains("不改客户主键", text);
        Assert.DoesNotContain("反核销", text);
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
