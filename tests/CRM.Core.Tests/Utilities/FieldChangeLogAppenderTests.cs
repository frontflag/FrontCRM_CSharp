using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class FieldChangeLogAppenderTests
{
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", "", false)]
    [InlineData("  a  ", "a", false)]
    [InlineData("old", "new", true)]
    [InlineData(null, "x", true)]
    public void ValuesDiffer_DetectsMeaningfulChanges(string? oldVal, string? newVal, bool expected) =>
        Assert.Equal(expected, FieldChangeLogAppender.ValuesDiffer(oldVal, newVal));

    [Fact]
    public void NormalizeValue_TrimsAndNullifiesBlank() =>
        Assert.Null(FieldChangeLogAppender.NormalizeValue("   "));
}

public class MasterEntityStatusLabelsTests
{
    [Theory]
    [InlineData((short)2, "待审核")]
    [InlineData((short)10, "已审核")]
    [InlineData((short)-1, "审核失败")]
    public void Format_KnownStatuses(short status, string expected) =>
        Assert.Equal(expected, MasterEntityStatusLabels.Format(status));
}
