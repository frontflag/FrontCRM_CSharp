using CRM.Core.Models.Sales;
using Xunit;

namespace CRM.Core.Tests.Models;

public class SellOrderHeaderRemarkCodecTests
{
    [Fact]
    public void ParseLegacyComment_ZhPrefixes_SplitsBlocks()
    {
        var raw = "产品：期货\n客户联系人：张三\n发票信息：税号123\n账期：NET 45\n\n自由一行";
        var b = SellOrderHeaderRemarkCodec.ParseLegacyComment(raw);
        Assert.Equal("期货", b.ProductKind);
        Assert.Equal("张三", b.CustomerContactName);
        Assert.Equal("税号123", b.InvoiceInfo);
        Assert.Equal("NET 45", b.PaymentTermsText);
        Assert.Equal("自由一行", b.LooseRemark);
    }

    [Fact]
    public void BuildDisplayComment_PrefersStructuredColumns()
    {
        var o = new SellOrder
        {
            ProductKind = "现货",
            PaymentTermsText = "NET 30",
            Comment = "尾注"
        };
        var s = SellOrderHeaderRemarkCodec.BuildDisplayComment(o);
        Assert.Contains("产品：现货", s);
        Assert.Contains("账期：NET 30", s);
        Assert.Contains("尾注", s);
    }

    [Fact]
    public void TryMaterialize_PutsLooseInComment()
    {
        var o = new SellOrder { Comment = "产品：排单\n仅备注" };
        Assert.True(SellOrderHeaderRemarkCodec.TryMaterializeFromLegacyComment(o));
        Assert.Equal("排单", o.ProductKind);
        Assert.Equal("仅备注", o.Comment);
    }

    [Fact]
    public void TrySplitCommentOntoStructuredColumns_PlainTextNotLegacy_NoOp()
    {
        var o = new SellOrder { Comment = "  整段无前缀  " };
        Assert.False(SellOrderHeaderRemarkCodec.TrySplitCommentOntoStructuredColumns(o));
        Assert.Equal("  整段无前缀  ", o.Comment);
    }

    [Fact]
    public void TrySplitCommentOntoStructuredColumns_DoesNotOverwriteExistingProductKind()
    {
        var o = new SellOrder { ProductKind = "现货", Comment = "产品：期货" };
        Assert.True(SellOrderHeaderRemarkCodec.TrySplitCommentOntoStructuredColumns(o));
        Assert.Equal("现货", o.ProductKind);
        Assert.Null(o.Comment);
    }
}
