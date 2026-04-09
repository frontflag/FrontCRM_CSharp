using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace CRM.API.Middlewares;

/// <summary>
/// 已颁发 JWT 后若账号被冻结/停用/软删，后续请求拒绝（避免仅靠 Token 有效期内的业务访问）。
/// </summary>
public sealed class RequireActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public RequireActiveUserMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IUserService userService)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        var uid = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
        {
            await _next(context);
            return;
        }

        var user = await userService.GetByIdForAdminAsync(uid.Trim());
        if (user == null || !user.IsActive || user.Status != UserAccountStatus.Active)
        {
            var msg = user?.Status == UserAccountStatus.Frozen
                ? "账号已冻结，请重新登录"
                : "账号已停用或不可用，请重新登录";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(msg, 401));
            return;
        }

        await _next(context);
    }
}
