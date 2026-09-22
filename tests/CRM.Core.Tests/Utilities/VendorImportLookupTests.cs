using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities
{
    public class VendorImportLookupTests
    {
        [Fact]
        public void OfficialName_StripsInnerWhitespace_AndSkips()
        {
            var lookup = new VendorImportLookup();
            lookup.Add("华为技术", null, "id-1", "VEN00001");

            var hit = lookup.Match("华 为 技术", null);

            Assert.NotNull(hit);
            Assert.Equal("VEN00001", hit!.Code);
            Assert.False(hit.IsPending);
        }

        [Fact]
        public void CreditCode_Matches_WhenNameDiffers()
        {
            var lookup = new VendorImportLookup();
            lookup.Add("甲公司", "914403xxab", "id-1", "VEN00001");

            var hit = lookup.Match("乙公司", "91 4403XXAB");

            Assert.NotNull(hit);
            Assert.Equal("id-1", hit!.Id);
        }

        [Fact]
        public void NameMatch_WinsOverDifferentCreditMatch()
        {
            var lookup = new VendorImportLookup();
            lookup.Add("甲公司", "CODE-A", "id-a", "VEN0000A");
            lookup.Add("乙公司", "CODE-B", "id-b", "VEN0000B");

            var hit = lookup.Match("甲公司", "CODE-B");

            Assert.Equal("id-a", hit!.Id);
        }

        [Fact]
        public void EmptyCredit_DoesNotMatch()
        {
            var lookup = new VendorImportLookup();
            lookup.Add("甲公司", null, "id-1", "VEN00001");

            Assert.Null(lookup.Match("乙公司", "  "));
        }

        [Fact]
        public void FirstName_IsKept_WhenAddedTwice()
        {
            var lookup = new VendorImportLookup();
            lookup.Add("甲公司", null, "id-1", "VEN00001");
            lookup.Add("甲 公司", null, "id-2", "VEN00002");

            Assert.Equal("id-1", lookup.Match("甲公司", null)!.Id);
        }

        [Fact]
        public void PendingRow_HasNoExistingCode()
        {
            var lookup = new VendorImportLookup();
            lookup.Add("甲公司", "CODE-A", "", "");

            var hit = lookup.Match("甲公司", null);

            Assert.NotNull(hit);
            Assert.True(hit!.IsPending);
            Assert.Equal("", hit.Code);
        }
    }
}
