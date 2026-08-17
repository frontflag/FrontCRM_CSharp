using CRM.Core.Utilities;

namespace CRM.Core.Tests.Utilities;

public class StockInOpsCheckSuggestionsTests
{
    [Fact]
    public void Duplicate_UnwrittenOff_KeepsSmallest_NoReverse()
    {
        var extras = new[]
        {
            Hint("STI00222"),
            Hint("STI00223"),
            Hint("STI0022M")
        };

        var text = StockInOpsCheckSuggestions.DuplicateKeepSmallest(extras);

        Assert.Contains("① 打开「入库单列表」，对 STI00222 点「强制删除」，输入 STI00222 确认。", text);
        Assert.Contains("② 对 STI00223 点「强制删除」，输入 STI00223 确认。", text);
        Assert.Contains("③ 对 STI0022M 点「强制删除」，输入 STI0022M 确认。", text);
        Assert.Contains("④ 打开「Debug 模拟数据」，点「刷新到货通知状态」。", text);
        Assert.DoesNotContain("反核销", text);
        Assert.DoesNotContain("STI00221", text);
    }

    [Fact]
    public void Duplicate_WrittenOffExtra_NamesInvoiceThenForceDelete()
    {
        var extras = new[]
        {
            Hint("STI00222", "INVI0001A")
        };

        var text = StockInOpsCheckSuggestions.DuplicateKeepSmallest(extras);

        Assert.Contains("① 打开「进项发票」INVI0001A，点「反核销」，输入 INVI0001A 确认。", text);
        Assert.Contains("② 打开「入库单列表」，对 STI00222 点「强制删除」，输入 STI00222 确认。", text);
        Assert.Contains("③ 打开「Debug 模拟数据」，点「刷新到货通知状态」。", text);
    }

    [Fact]
    public void OrphanWriteOff_NamesInvoiceReverse()
    {
        var text = StockInOpsCheckSuggestions.OrphanWriteOff("INVI0009B");
        Assert.Equal("① 打开「进项发票」INVI0009B，点「反核销」，输入 INVI0009B 确认。", text);
    }

    [Fact]
    public void Notice100_Unposted_MarksThenDebug()
    {
        var text = StockInOpsCheckSuggestions.Notice100NoPosted(new[] { "STI0001A" });
        Assert.Contains("① 打开「入库单列表」，对 STI0001A 点「标记已入库」。", text);
        Assert.Contains("② 打开「Debug 模拟数据」，点「刷新到货通知状态」。", text);
        Assert.DoesNotContain("反核销", text);
    }

    [Fact]
    public void Notice100_NoUnposted_OnlyDebug()
    {
        var text = StockInOpsCheckSuggestions.Notice100NoPosted(Array.Empty<string>());
        Assert.Equal("① 打开「Debug 模拟数据」，点「刷新到货通知状态」。", text);
        Assert.DoesNotContain("反核销", text);
        Assert.DoesNotContain("强制删除", text);
    }

    [Fact]
    public void ReversePurchaseInvoiceSteps_Empty_WhenNoCodes()
    {
        Assert.Empty(StockInOpsCheckSuggestions.ReversePurchaseInvoiceSteps(Array.Empty<string>()));
        Assert.Empty(StockInOpsCheckSuggestions.ReversePurchaseInvoiceSteps(null));
    }

    [Fact]
    public void JoinSteps_UsesCircledNumbersAndNewlines()
    {
        var text = StockInOpsCheckSuggestions.JoinSteps(new[] { "第一步", "第二步" });
        Assert.Equal("① 第一步\n② 第二步", text);
    }

    private static StockInOpsCheckSuggestions.StockInHint Hint(string code, params string[] invoices) =>
        new(code, invoices);
}
