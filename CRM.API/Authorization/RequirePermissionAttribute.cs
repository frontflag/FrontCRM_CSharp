using System.Security.Claims;
using CRM.API.Models.DTOs;
using CRM.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CRM.API.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permissionCode;

        public RequirePermissionAttribute(string permissionCode)
        {
            _permissionCode = permissionCode;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (string.IsNullOrWhiteSpace(_permissionCode))
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
            if (summary.IsSysAdmin || summary.PermissionCodes.Contains(_permissionCode))
                return;

            context.Result = new ObjectResult(ApiResponse<object>.Fail($"无权限访问: {_permissionCode}", 403))
            {
                StatusCode = 403
            };
        }
    }
}
