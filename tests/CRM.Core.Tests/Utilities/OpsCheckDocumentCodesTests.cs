using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class OpsCheckDocumentCodesTests
{
    [Fact]
    public void ForSuggestion_PrefersPrimaryCode()
    {
        Assert.Equal("ARV00007", OpsCheckDocumentCodes.ForSuggestion("ARV00007", "db9a0fa0-a414-49d2-989b-0a5803ebd6ab"));
    }

    [Fact]
    public void ForSuggestion_SkipsGuidAlternates()
    {
        Assert.Equal("STO0020H", OpsCheckDocumentCodes.ForSuggestion(null, "db9a0fa0-a414-49d2-989b-0a5803ebd6ab", "STO0020H"));
    }

    [Fact]
    public void ForSuggestion_ReturnsMissingWhenOnlyGuids()
    {
        Assert.Equal(OpsCheckDocumentCodes.Missing,
            OpsCheckDocumentCodes.ForSuggestion("db9a0fa0-a414-49d2-989b-0a5803ebd6ab"));
    }

    [Fact]
    public void FilterCodes_RemovesGuids()
    {
        var list = OpsCheckDocumentCodes.FilterCodes(new[] { "REC001ZP", "db9a0fa0-a414-49d2-989b-0a5803ebd6ab", "REC002OK" });
        Assert.Equal(new[] { "REC001ZP", "REC002OK" }, list);
    }
}
