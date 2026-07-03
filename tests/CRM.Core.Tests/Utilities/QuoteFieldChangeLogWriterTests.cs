using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;
using CRM.Core.Utilities;
using NSubstitute;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class QuoteFieldChangeLogWriterTests
{
    [Theory]
    [InlineData((short)QuoteMainStatus.New, "新建")]
    [InlineData((short)QuoteMainStatus.Won, "成单")]
    [InlineData((short)QuoteMainStatus.Closed, "关闭")]
    public void FormatQuoteStatus_ReturnsChineseLabel(short status, string expected)
    {
        Assert.Equal(expected, QuoteFieldChangeLogWriter.FormatQuoteStatus(status));
    }

    [Fact]
    public async Task AppendQuoteStatusChangeAsync_WhenChanged_WritesLog()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var quote = new Quote { Id = "q1", QuoteCode = "QT0001", Status = (short)QuoteMainStatus.Won };

        await QuoteFieldChangeLogWriter.AppendQuoteStatusChangeAsync(
            uow,
            quote,
            (short)QuoteMainStatus.New,
            (short)QuoteMainStatus.Won);

        await uow.Received(1).ExecuteAsync(Arg.Is<string>(s =>
            s.Contains("log_change_fldval") &&
            s.Contains("'Quote'") &&
            s.Contains("status") &&
            s.Contains("新建") &&
            s.Contains("成单") &&
            s.Contains("系统")));
    }

    [Fact]
    public async Task AppendQuoteStatusChangeAsync_WhenUnchanged_SkipsWrite()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var quote = new Quote { Id = "q1", QuoteCode = "QT0001", Status = (short)QuoteMainStatus.New };

        await QuoteFieldChangeLogWriter.AppendQuoteStatusChangeAsync(
            uow,
            quote,
            (short)QuoteMainStatus.New,
            (short)QuoteMainStatus.New);

        await uow.DidNotReceive().ExecuteAsync(Arg.Any<string>());
    }
}
