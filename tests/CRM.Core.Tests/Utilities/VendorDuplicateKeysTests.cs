using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities
{
    public class VendorDuplicateKeysTests
    {
        [Fact]
        public void Empty_DoesNotMatch()
        {
            Assert.False(VendorDuplicateKeys.IsMatch(null, "  ", "", null, "甲", "Acme", "9111", "12-345"));
            Assert.False(VendorDuplicateKeys.HasAnyKey("  ", "\u3000", null, "-"));
        }

        [Fact]
        public void OfficialName_StripsInnerWhitespace()
        {
            Assert.True(VendorDuplicateKeys.IsMatch(
                "华 为 技术", null, null, null,
                "华为技术", null, null, null));
        }

        [Fact]
        public void English_IsCaseInsensitive()
        {
            Assert.True(VendorDuplicateKeys.IsMatch(
                null, "Acme  Corp", null, null,
                null, "ACMECORP", null, null));
        }

        [Fact]
        public void CreditCode_Uppercases()
        {
            Assert.True(VendorDuplicateKeys.IsMatch(
                null, null, "91 4403xxab", null,
                null, null, "914403XXAB", null));
        }

        [Fact]
        public void Duns_StripsHyphenAndWhitespace()
        {
            Assert.True(VendorDuplicateKeys.IsMatch(
                null, null, null, "12-345 6789",
                null, null, null, "123456789"));
        }

        [Fact]
        public void DifferentFields_DoNotCrossMatch()
        {
            Assert.False(VendorDuplicateKeys.IsMatch(
                "华为", null, null, null,
                null, "华为", null, null));
        }
    }
}
