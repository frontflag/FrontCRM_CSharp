using CRM.API.Controllers;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;

namespace CRM.API.Tests.Controllers;

public sealed class CustomerWorkspaceControllerTests
{
    private static CustomerWorkspaceController CreateController(
        ICustomerWorkspaceService workspace,
        string? userId = "test-user-1")
    {
        var http = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(userId))
        {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                "Test"));
        }

        return new CustomerWorkspaceController(
            workspace,
            Substitute.For<ILogger<CustomerWorkspaceController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task Get_Returns200()
    {
        var workspace = Substitute.For<ICustomerWorkspaceService>();
        workspace.GetAsync("rfqItem", "i1", "test-user-1")
            .Returns(new CustomerWorkspaceDto
            {
                HasCustomer = true,
                CanViewFull = true,
                CustomerId = "c1",
                CustomerCode = "C001"
            });

        var c = CreateController(workspace);
        var action = await c.Get("rfqItem", "i1");

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var api = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.Success.Should().BeTrue();
        await workspace.Received(1).GetAsync("rfqItem", "i1", "test-user-1");
    }

    [Fact]
    public async Task Get_Returns400_WhenSourceInvalid()
    {
        var workspace = Substitute.For<ICustomerWorkspaceService>();
        workspace.GetAsync("nope", "i1", "test-user-1")
            .Returns<CustomerWorkspaceDto?>(_ => throw new ArgumentException("不支持的来源"));

        var c = CreateController(workspace);
        var action = await c.Get("nope", "i1");

        var bad = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var api = bad.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.Success.Should().BeFalse();
        api.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Get_Returns404_WhenDocumentMissing()
    {
        var workspace = Substitute.For<ICustomerWorkspaceService>();
        workspace.GetAsync("rfq", "missing", "test-user-1").Returns((CustomerWorkspaceDto?)null);

        var c = CreateController(workspace);
        var action = await c.Get("rfq", "missing");

        var nf = action.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var api = nf.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.ErrorCode.Should().Be(404);
    }
}
