using System.Security.Claims;
using CRM.API.Authorization;
using CRM.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CRM.API.Tests.Authorization;

/// <summary>
/// 直接测试 <see cref="RequirePermissionAttribute"/>，不经过完整 MVC 管道，避免与 ActionResult 包装细节耦合。
/// </summary>
public sealed class RequirePermissionAttributeTests
{
    private static AuthorizationFilterContext CreateFilterContext(HttpContext http)
    {
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = "RFQs",
            ActionName = "GetRFQs"
        };
        var actionContext = new ActionContext(http, new RouteData(), actionDescriptor);
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task OnAuthorization_NoUserId_Sets401()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRbacService>(Substitute.For<IRbacService>());
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.User = new ClaimsPrincipal(new ClaimsIdentity());

        var ctx = CreateFilterContext(http);
        var attr = new RequirePermissionAttribute("rfq.read");

        await attr.OnAuthorizationAsync(ctx);

        var unauthorized = ctx.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorized.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task OnAuthorization_NoRbacService_Sets500()
    {
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u1")
        }, "Test"));

        var ctx = CreateFilterContext(http);
        var attr = new RequirePermissionAttribute("rfq.read");

        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeOfType<StatusCodeResult>().Subject.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task OnAuthorization_NoPermission_Sets403()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("u1").Returns(new UserPermissionSummaryDto
        {
            UserId = "u1",
            IsSysAdmin = false,
            PermissionCodes = new List<string>()
        });

        var services = new ServiceCollection();
        services.AddSingleton<IRbacService>(rbac);
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u1")
        }, "Test"));

        var ctx = CreateFilterContext(http);
        var attr = new RequirePermissionAttribute("rfq.read");

        await attr.OnAuthorizationAsync(ctx);

        var obj = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task OnAuthorization_SysAdmin_Allows()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("u1").Returns(new UserPermissionSummaryDto
        {
            IsSysAdmin = true,
            PermissionCodes = Array.Empty<string>()
        });

        var services = new ServiceCollection();
        services.AddSingleton<IRbacService>(rbac);
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u1")
        }, "Test"));

        var ctx = CreateFilterContext(http);
        var attr = new RequirePermissionAttribute("rfq.read");

        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorization_HasPermissionCode_Allows()
    {
        var rbac = Substitute.For<IRbacService>();
        rbac.GetUserPermissionSummaryAsync("u1").Returns(new UserPermissionSummaryDto
        {
            IsSysAdmin = false,
            PermissionCodes = new List<string> { "rfq.read", "other" }
        });

        var services = new ServiceCollection();
        services.AddSingleton<IRbacService>(rbac);
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "u1")
        }, "Test"));

        var ctx = CreateFilterContext(http);
        var attr = new RequirePermissionAttribute("rfq.read");

        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorization_EmptyPermissionCode_SkipsCheck()
    {
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        http.User = new ClaimsPrincipal(new ClaimsIdentity());

        var ctx = CreateFilterContext(http);
        var attr = new RequirePermissionAttribute("");

        await attr.OnAuthorizationAsync(ctx);

        ctx.Result.Should().BeNull();
    }
}
