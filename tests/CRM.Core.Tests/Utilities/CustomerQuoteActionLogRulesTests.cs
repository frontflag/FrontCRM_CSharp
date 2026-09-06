using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CustomerQuoteActionLogRulesTests
{
    [Theory]
    [InlineData("print", "打印")]
    [InlineData("PRINT", "打印")]
    [InlineData("打印", "打印")]
    [InlineData("export", "导出")]
    [InlineData("export-pdf", "导出")]
    [InlineData("导出", "导出")]
    public void ParseClientAction_MapsPrintAndExport(string input, string expected)
    {
        Assert.Equal(expected, CustomerQuoteActionLogRules.ParseClientAction(input));
    }

    [Theory]
    [InlineData("send-email")]
    [InlineData("发送邮件")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseClientAction_RejectsSendEmailAndUnknown(string? input)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomerQuoteActionLogRules.ParseClientAction(input));
        Assert.Equal(CustomerQuoteActionLogRules.InvalidActionMessage, ex.Message);
    }

    [Fact]
    public void IsDisplayAction_OnlyPrintExportEmail()
    {
        Assert.True(CustomerQuoteActionLogRules.IsDisplayAction("打印"));
        Assert.True(CustomerQuoteActionLogRules.IsDisplayAction("导出"));
        Assert.True(CustomerQuoteActionLogRules.IsDisplayAction("发送邮件"));
        Assert.False(CustomerQuoteActionLogRules.IsDisplayAction("设置已发送"));
    }
}
