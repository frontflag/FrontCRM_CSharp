using System.Net;
using System.Security.Claims;
using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.API.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            string? errorId = context.Items[SysErrorLogHttpKeys.ErrorIdItem] as string
                              ?? SysErrorRequestContext.ErrorId;

            try
            {
                var errorLog = context.RequestServices.GetService<IErrorLogService>();
                // DbUpdate 已由 Interceptor 落库时复用编号，避免重复写入
                if (errorLog != null && string.IsNullOrWhiteSpace(errorId))
                {
                    var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var userName = context.User?.FindFirstValue(ClaimTypes.Name)
                                   ?? context.User?.Identity?.Name;
                    var id = await errorLog.LogAsync(
                        moduleName: "未处理异常",
                        errorMessage: RootMessage(exception),
                        exception: exception,
                        operationType: context.Request.Method,
                        userId: userId,
                        userName: userName,
                        requestPath: context.Request.Path.Value);
                    if (id.HasValue)
                    {
                        errorId = SysErrorLogIdFormat.Format(id.Value);
                        context.Items[SysErrorLogHttpKeys.ErrorIdItem] = errorId;
                        SysErrorRequestContext.ErrorId = errorId;
                    }
                }
            }
            catch
            {
                // ignore secondary failures
            }

            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var message = string.IsNullOrWhiteSpace(errorId)
                    ? "系统繁忙，请稍后重试"
                    : $"系统繁忙，请稍后重试（错误编号 {errorId}）";

                var response = new
                {
                    success = false,
                    message,
                    errorCode = 500,
                    errorId,
                    data = (object?)null
                };

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(json);
            }
        }

        private static string RootMessage(Exception ex)
        {
            var cur = ex;
            while (cur.InnerException != null)
                cur = cur.InnerException;
            return string.IsNullOrWhiteSpace(cur.Message) ? ex.Message : cur.Message;
        }
    }
}
