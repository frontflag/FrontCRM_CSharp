using CRM.Core.Models.Sales;
using Xunit;

namespace CRM.Core.Tests.Models;

public class SellOrderItemCommentCodecTests
{
    [Fact]
    public void TryParse_ZhPrefix_FirstLineOnly()
    {
        var raw = "客户物料型号：ABC-001\n行备注第二行";
        var v = SellOrderItemCommentCodec.TryParseCustomerMaterialModelFromComment(raw);
        Assert.Equal("ABC-001", v);
    }

    [Fact]
    public void TryParse_EnPrefix_NoSecondLine()
    {
        var raw = "Customer part no.：PN-99";
        var v = SellOrderItemCommentCodec.TryParseCustomerMaterialModelFromComment(raw);
        Assert.Equal("PN-99", v);
    }

    [Fact]
    public void TryParse_NoPrefix_ReturnsNull()
    {
        Assert.Null(SellOrderItemCommentCodec.TryParseCustomerMaterialModelFromComment("仅普通备注"));
    }
}
