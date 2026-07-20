using System.Security.Claims;
using CRM.Core.Utilities;

namespace CRM.API.Middlewares;

/// <summary>
/// 填充/清理请求级错误审计上下文（供 SaveChanges 拦截器使用）。
/// </summary>
public sealed class SysErrorRequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public SysErrorRequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        SysErrorRequestContext.Clear();
        SysErrorRequestContext.RequestPath = context.Request.Path.Value;
        try
        {
            // 认证之后再补用户；此处先跑 pipeline，在 ErrorHandling 之前尽量在授权后设置
            await _next(context);
        }
        finally
        {
            SysErrorRequestContext.Clear();
        }
    }
}
