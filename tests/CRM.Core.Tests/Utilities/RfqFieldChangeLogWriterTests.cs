using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using CRM.Core.Utilities;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class RfqFieldChangeLogWriterTests
{
    [Theory]
    [InlineData((short)RfqMainStatus.PendingAssign, "待分配")]
    [InlineData((short)RfqMainStatus.Assigned, "已分配")]
    [InlineData((short)RfqMainStatus.Closed, "已关闭")]
    public void FormatRfqMainStatus_ReturnsChineseLabel(short status, string expected)
    {
        Assert.Equal(expected, RfqFieldChangeLogWriter.FormatRfqMainStatus(status));
    }

    [Theory]
    [InlineData((short)RfqItemStatus.Pending, "待报价")]
    [InlineData((short)RfqItemStatus.Quoted, "已报价")]
    [InlineData((short)RfqItemStatus.NoQuoteFound, "查无报价")]
    public void FormatRfqItemStatus_ReturnsChineseLabel(short status, string expected)
    {
        Assert.Equal(expected, RfqFieldChangeLogWriter.FormatRfqItemStatus(status));
    }

    [Fact]
    public async Task AppendRfqStatusChangeAsync_WhenChanged_WritesLog()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var rfq = new RFQ { Id = "r1", RfqCode = "RF0001", Status = (short)RfqMainStatus.Assigned };

        await RfqFieldChangeLogWriter.AppendRfqStatusChangeAsync(
            uow,
            rfq,
            (short)RfqMainStatus.PendingAssign,
            (short)RfqMainStatus.Assigned,
            "u1",
            "测试用户");

        await uow.Received(1).ExecuteAsync(Arg.Is<string>(s =>
            s.Contains("log_change_fldval") &&
            s.Contains("'Rfq'") &&
            s.Contains("status") &&
            s.Contains("待分配") &&
            s.Contains("已分配") &&
            s.Contains("测试用户")));
    }

    [Fact]
    public async Task AppendRfqStatusChangeAsync_WhenUnchanged_SkipsWrite()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var rfq = new RFQ { Id = "r1", RfqCode = "RF0001", Status = (short)RfqMainStatus.PendingAssign };

        await RfqFieldChangeLogWriter.AppendRfqStatusChangeAsync(
            uow,
            rfq,
            (short)RfqMainStatus.PendingAssign,
            (short)RfqMainStatus.PendingAssign,
            "u1",
            "测试用户");

        await uow.DidNotReceive().ExecuteAsync(Arg.Any<string>());
    }
}
