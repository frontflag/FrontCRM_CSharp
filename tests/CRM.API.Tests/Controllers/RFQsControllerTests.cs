using System.Security.Claims;
using CRM.API.Controllers;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CRM.API.Tests.Controllers;

/// <summary>
/// RFQs API：控制器层测试（含 <see cref="CRM.API.Authorization.RequirePermissionAttribute"/> 与 Claims）。
/// 不启动 Kestrel、不连数据库，通过模拟 <see cref="IRFQService"/> / <see cref="IDataPermissionService"/> / <see cref="IRbacService"/>。
/// </summary>
public sealed class RFQsControllerTests
{
    private static RFQsController CreateController(
        IRFQService rfq,
        IDataPermissionService dataPermission,
        Action<IRbacService>? configureRbac = null,
        string? userId = "test-user-1")
    {
        var rbac = Substitute.For<IRbacService>();
        if (configureRbac != null)
            configureRbac(rbac);
        else
        {
            rbac.GetUserPermissionSummaryAsync(Arg.Any<string>()).Returns(new UserPermissionSummaryDto
            {
                UserId = userId ?? "",
                IsSysAdmin = true,
                PermissionCodes = new[] { "rfq.read", "rfq.write", "rfq.create", "rfq.delete" }
            });
        }

        var services = new ServiceCollection();
        services.AddSingleton<IRbacService>(rbac);
        var sp = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = sp };
        if (!string.IsNullOrEmpty(userId))
        {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                "Test"));
        }

        return new RFQsController(
            rfq,
            dataPermission,
            Substitute.For<ILogger<RFQsController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    [Fact]
    public async Task GetRFQs_Returns200_AndPagedShape()
    {
        var rfq = Substitute.For<IRFQService>();
        rfq.GetPagedAsync(Arg.Any<RFQQueryRequest>()).Returns(new PagedResult<RFQListItem>
        {
            Items = new List<RFQListItem>
            {
                new()
                {
                    Id = "r1",
                    RfqCode = "RF001",
                    CustomerId = "c1",
                    CustomerName = "客户A",
                    Status = 0,
                    RfqType = 1,
                    Importance = 5,
                    ItemCount = 1,
                    CreateTime = DateTime.UtcNow
                }
            },
            TotalCount = 1,
            PageIndex = 1,
            PageSize = 20
        });

        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.GetRFQs(1, 20, null, null, null, null, null);

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var api = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.Success.Should().BeTrue();
        await rfq.Received(1).GetPagedAsync(Arg.Is<RFQQueryRequest>(q =>
            q.PageIndex == 1 && q.PageSize == 20 && q.CurrentUserId == "test-user-1"));
    }

    [Fact]
    public async Task GetRFQItems_Returns200_AndUsesQuery()
    {
        var rfq = Substitute.For<IRFQService>();
        rfq.GetPagedItemsAsync(Arg.Any<RFQItemQueryRequest>())
            .Returns(new PagedResult<RFQItemListItem>
            {
                Items = new List<RFQItemListItem>(),
                TotalCount = 0,
                PageIndex = 1,
                PageSize = 20
            });

        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.GetRFQItems(2, 10, "2026-01-01", "2026-01-31", "kw", "mpn", salesUserId: null, salesUserKeyword: "sales");

        action.Result.Should().BeOfType<OkObjectResult>();
        await rfq.Received(1).GetPagedItemsAsync(Arg.Is<RFQItemQueryRequest>(q =>
            q.PageIndex == 2 &&
            q.PageSize == 10 &&
            q.CustomerKeyword == "kw" &&
            q.MaterialModel == "mpn" &&
            q.SalesUserKeyword == "sales" &&
            q.CurrentUserId == "test-user-1"));
    }

    [Fact]
    public async Task GetRFQ_Returns404_WhenServiceReturnsNull()
    {
        var rfq = Substitute.For<IRFQService>();
        rfq.GetByIdAsync("x", Arg.Any<string?>()).Returns((RFQ?)null);

        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.GetRFQ("x");

        action.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRFQ_Returns403_WhenDataPermissionDenies()
    {
        var rfq = Substitute.For<IRFQService>();
        var entity = new RFQ { Id = "r1", RfqCode = "RF1", CustomerId = "c1" };
        rfq.GetByIdAsync("r1", Arg.Any<string?>()).Returns(entity);

        var data = Substitute.For<IDataPermissionService>();
        data.CanAccessRFQAsync("test-user-1", entity).Returns(false);

        var c = CreateController(rfq, data);
        var action = await c.GetRFQ("r1");

        var forbidden = action.Result.Should().BeOfType<ObjectResult>().Subject;
        forbidden.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetRFQ_Returns200_WhenAllowed()
    {
        var rfq = Substitute.For<IRFQService>();
        var entity = new RFQ { Id = "r1", RfqCode = "RF1", CustomerId = "c1" };
        rfq.GetByIdAsync("r1", Arg.Any<string?>()).Returns(entity);

        var data = Substitute.For<IDataPermissionService>();
        data.CanAccessRFQAsync("test-user-1", entity).Returns(true);

        var c = CreateController(rfq, data);
        var action = await c.GetRFQ("r1");

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var api = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.Success.Should().BeTrue();
        api.Data.Should().BeSameAs(entity);
    }

    [Fact]
    public async Task CreateRFQ_Returns400_OnArgumentException()
    {
        var rfq = Substitute.For<IRFQService>();
        rfq.CreateAsync(Arg.Any<CreateRFQRequest>(), Arg.Any<string>())
            .Returns(Task.FromException<RFQ>(new ArgumentException("请选择客户")));

        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.CreateRFQ(new CreateRFQRequest());

        var bad = action.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var api = bad.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.Success.Should().BeFalse();
        api.Message.Should().Contain("客户");
    }

    [Fact]
    public async Task CreateRFQ_Returns201_WhenOk()
    {
        var rfq = Substitute.For<IRFQService>();
        var created = new RFQ { Id = "new-id", RfqCode = "RF99", CustomerId = "c1" };
        rfq.CreateAsync(Arg.Any<CreateRFQRequest>(), Arg.Any<string>()).Returns(created);

        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.CreateRFQ(new CreateRFQRequest { CustomerId = "c1", Items = { new CreateRFQItemRequest { Mpn = "M", Brand = "B" } } });

        var createdResult = action.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        var api = createdResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRFQ_Returns404_WhenServiceThrowsNotFound()
    {
        var rfq = Substitute.For<IRFQService>();
        rfq.UpdateAsync(Arg.Any<string>(), Arg.Any<UpdateRFQRequest>())
            .Returns(Task.FromException<RFQ>(new InvalidOperationException("需求 x 不存在")));

        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.UpdateRFQ("x", new UpdateRFQRequest());

        action.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteRFQ_Returns404_WhenMissing()
    {
        var rfq = Substitute.For<IRFQService>();
        rfq.DeleteAsync("missing")
            .Returns(Task.FromException(new InvalidOperationException("需求 missing 不存在")));

        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.DeleteRFQ("missing");

        action.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_Returns200_WhenOk()
    {
        var rfq = Substitute.For<IRFQService>();
        var c = CreateController(rfq, Substitute.For<IDataPermissionService>());
        var action = await c.UpdateStatus("id1", new UpdateStatusRequest { Status = 2 });

        var ok = action.Result.Should().BeOfType<OkObjectResult>().Subject;
        var api = ok.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        api.Success.Should().BeTrue();
        await rfq.Received(1).UpdateStatusAsync("id1", 2);
    }

}
