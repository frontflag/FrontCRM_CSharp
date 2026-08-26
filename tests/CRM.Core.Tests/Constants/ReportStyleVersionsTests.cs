using CRM.Core.Constants;
using FluentAssertions;

namespace CRM.Core.Tests.Constants;

public sealed class ReportStyleVersionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("v1")]
    [InlineData("V3")]
    [InlineData("1")]
    public void NormalizeOrDefault_FallsBackToV1(string? value)
    {
        ReportStyleVersions.NormalizeOrDefault(value).Should().Be(ReportStyleVersions.V1);
    }

    [Theory]
    [InlineData("V1")]
    [InlineData(" V1 ")]
    [InlineData("V2")]
    [InlineData("\tV2")]
    public void NormalizeOrDefault_KeepsAllowed(string value)
    {
        ReportStyleVersions.NormalizeOrDefault(value).Should().Be(value.Trim());
    }

    [Fact]
    public void RequireAllowed_ThrowsOnInvalid()
    {
        var act = () => ReportStyleVersions.RequireAllowed("V3");
        act.Should().Throw<ArgumentException>().WithMessage("*V1*V2*");
    }

    [Fact]
    public void RequireAllowed_AcceptsTrimmedV2()
    {
        ReportStyleVersions.RequireAllowed(" V2 ").Should().Be("V2");
    }
}
