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

    [Fact]
    public void FormatDocPartySlot_UsesDocumentAndPartyCodes()
    {
        Assert.Equal(
            "PAK0023V（CUS001）",
            OpsCheckDocumentCodes.FormatDocPartySlot("PAK0023V", "ecb43f5b-d9d7-4130-9ec4-b422a2b29d4a", "CUS001"));
    }

    [Fact]
    public void FormatDocPartySlot_NeverEmitsGuid()
    {
        var guid = "6c01f097-8c38-4a88-9110-b3e9fbd82488";
        Assert.Equal("ST000265（单号缺失）", OpsCheckDocumentCodes.FormatDocPartySlot("ST000265", guid, guid));
        Assert.Equal("单号缺失（空）", OpsCheckDocumentCodes.FormatDocPartySlot(null, null, guid));
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [guid] = "CUS002"
        };
        Assert.Equal("ST000265（CUS002）", OpsCheckDocumentCodes.FormatDocPartySlot("ST000265", guid, map));
        Assert.DoesNotContain(guid, OpsCheckDocumentCodes.FormatDocPartySlot("ST000265", guid, map), StringComparison.OrdinalIgnoreCase);
    }
}
