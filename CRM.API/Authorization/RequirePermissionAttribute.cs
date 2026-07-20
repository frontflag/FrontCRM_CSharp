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
                context.Result = new ObjectResult(ApiResponse<object>.Fail("权限服务未就绪", 500))
                {
                    StatusCode = 500
                };
                return;
            }

            try
            {
                var summary = await rbacService.GetUserPermissionSummaryAsync(userId);
                var ok = summary.IsSysAdmin || summary.PermissionCodes.Any(c =>
                    string.Equals(c, _permissionCode, StringComparison.OrdinalIgnoreCase));
                if (ok)
                    return;

                context.Result = new ObjectResult(ApiResponse<object>.Fail($"无权限访问: {_permissionCode}", 403))
                {
                    StatusCode = 403
                };
            }
            catch (Exception ex)
            {
                context.Result = new ObjectResult(
                    ApiResponse<object>.Fail($"权限校验失败: {ex.Message}", 500))
                {
                    StatusCode = 500
                };
            }
        }
    }
}
