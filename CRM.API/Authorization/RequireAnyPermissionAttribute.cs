using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CRM.API.Authorization;

/// <summary>具备任一权限即通过（用于业务员与销售/采购跨模块操作等场景）。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireAnyPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissionCodes;

    public RequireAnyPermissionAttribute(params string[] permissionCodes)
    {
        _permissionCodes = permissionCodes ?? Array.Empty<string>();
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var codes = _permissionCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToArray();
        if (codes.Length == 0)
            return;

        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.Fail("未登录或登录态失效", 401));
            return;
        }

        var rbacService = context.HttpContext.RequestServices.GetService<IRbacService>();
        if (rbacService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var summary = await rbacService.GetUserPermissionSummaryAsync(userId);
        if (summary.IsSysAdmin)
            return;

        var ok = codes.Any(code =>
            summary.PermissionCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)));
        if (ok)
            return;

        context.Result = new ObjectResult(
                ApiResponse<object>.Fail($"无权限访问（需要其一：{string.Join(", ", codes)}）", 403))
            { StatusCode = 403 };
    }
}
