using CRM.API.Controllers;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CRM.API.Tests.Controllers;

public sealed class ReportParamsControllerTests
{
    private static ReportParamsController CreateController(IReportParamsService service)
    {
        return new ReportParamsController(service, Substitute.For<ILogger<ReportParamsController>>());
    }

    [Fact]
    public async Task GetEffectiveStyleVersion_Returns200()
    {
        var service = Substitute.For<IReportParamsService>();
        service.GetStyleVersionAsync(Arg.Any<CancellationToken>()).Returns("V2");

        var action = await CreateController(service).GetEffectiveStyleVersion(CancellationToken.None);

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var api = ok.Value.Should().BeOfType<ApiResponse<ReportParamsStyleVersionDto>>().Subject;
        api.Success.Should().BeTrue();
        api.Data!.StyleVersion.Should().Be("V2");
    }

    [Fact]
    public async Task GetStyleVersion_Returns200()
    {
        var service = Substitute.For<IReportParamsService>();
        service.GetStyleVersionAsync(Arg.Any<CancellationToken>()).Returns("V1");

        var action = await CreateController(service).GetStyleVersion(CancellationToken.None);

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var api = ok.Value.Should().BeOfType<ApiResponse<ReportParamsStyleVersionDto>>().Subject;
        api.Success.Should().BeTrue();
        api.Data!.StyleVersion.Should().Be("V1");
    }

    [Fact]
    public async Task SetStyleVersion_Returns400_WhenBodyMissing()
    {
        var service = Substitute.For<IReportParamsService>();
        var action = await CreateController(service).SetStyleVersion(null, CancellationToken.None);

        var bad = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var api = bad.Value.Should().BeOfType<ApiResponse<ReportParamsStyleVersionDto>>().Subject;
        api.Success.Should().BeFalse();
        api.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetStyleVersion_Returns400_WhenInvalid()
    {
        var service = Substitute.For<IReportParamsService>();
        service.SetStyleVersionAsync("V3", Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new ArgumentException("报表样式版本仅允许 V1 或 V2"));

        var action = await CreateController(service).SetStyleVersion(
            new SetReportParamsStyleVersionRequest { StyleVersion = "V3" },
            CancellationToken.None);

        var bad = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var api = bad.Value.Should().BeOfType<ApiResponse<ReportParamsStyleVersionDto>>().Subject;
        api.Success.Should().BeFalse();
        api.ErrorCode.Should().Be(400);
        await service.Received(1).SetStyleVersionAsync("V3", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetStyleVersion_Returns200()
    {
        var service = Substitute.For<IReportParamsService>();
        service.SetStyleVersionAsync("V2", Arg.Any<CancellationToken>()).Returns("V2");

        var action = await CreateController(service).SetStyleVersion(
            new SetReportParamsStyleVersionRequest { StyleVersion = "V2" },
            CancellationToken.None);

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var api = ok.Value.Should().BeOfType<ApiResponse<ReportParamsStyleVersionDto>>().Subject;
        api.Success.Should().BeTrue();
        api.Data!.StyleVersion.Should().Be("V2");
    }
}
