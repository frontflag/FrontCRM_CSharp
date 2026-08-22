using CRM.Core.Utilities;
using Xunit;

namespace CRM.Core.Tests.Utilities;

public class CompanyEmailSuffixTests
{
    [Fact]
    public void Empty_IsNull()
    {
        Assert.True(CompanyEmailSuffix.TryNormalize("  ", out var suffix, out var error));
        Assert.Null(suffix);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("@huawei.com", "@huawei.com")]
    [InlineData("Huawei.COM", "@huawei.com")]
    [InlineData("zhang@idesemi.com", "@idesemi.com")]
    [InlineData("a@b@eco-inf.com.cn", "@eco-inf.com.cn")]
    public void Normalize_AcceptsDomainOrFullEmail(string input, string expected)
    {
        Assert.True(CompanyEmailSuffix.TryNormalize(input, out var suffix, out var error));
        Assert.Equal(expected, suffix);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("@")]
    [InlineData("@com")]
    [InlineData("中文@公司")]
    [InlineData("abc")]
    public void Invalid_Rejected(string input)
    {
        Assert.False(CompanyEmailSuffix.TryNormalize(input, out var suffix, out var error));
        Assert.Null(suffix);
        Assert.Equal(CompanyEmailSuffix.InvalidFormatMessage, error);
    }

    [Fact]
    public void PublicSuffix_Detected()
    {
        Assert.True(CompanyEmailSuffix.IsPublic("@QQ.com"));
        Assert.True(CompanyEmailSuffix.IsPublic("user@gmail.com"));
        Assert.False(CompanyEmailSuffix.IsPublic("@huawei.com"));
    }
}
