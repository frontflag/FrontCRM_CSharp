using System.Reflection;
using CRM.API.Models.DTOs;
using CRM.Core.Constants;
using CRM.Core.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CRM.API.Filters;

/// <summary>
/// 若本请求已落库系统错误，给失败 ApiResponse 补上 errorId / 文案中的错误编号。
/// </summary>
public sealed class SysErrorIdResultFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        var errorId = context.HttpContext.Items[SysErrorLogHttpKeys.ErrorIdItem] as string
                      ?? SysErrorRequestContext.ErrorId;
        if (string.IsNullOrWhiteSpace(errorId))
            return;

        context.HttpContext.Items[SysErrorLogHttpKeys.ErrorIdItem] = errorId;

        if (context.Result is not ObjectResult { Value: { } value })
            return;

        var type = value.GetType();
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ApiResponse<>))
            return;

        var successProp = type.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance);
        if (successProp?.GetValue(value) is not false)
            return;

        var errorIdProp = type.GetProperty("ErrorId", BindingFlags.Public | BindingFlags.Instance);
        if (errorIdProp != null && errorIdProp.CanWrite)
        {
            var existing = errorIdProp.GetValue(value) as string;
            if (string.IsNullOrWhiteSpace(existing))
                errorIdProp.SetValue(value, errorId);
        }

        var messageProp = type.GetProperty("Message", BindingFlags.Public | BindingFlags.Instance);
        if (messageProp?.GetValue(value) is string msg && messageProp.CanWrite)
        {
            if (!msg.Contains("错误编号", StringComparison.Ordinal) && !msg.Contains(errorId, StringComparison.OrdinalIgnoreCase))
            {
                var friendly = SanitizeForUser(msg);
                messageProp.SetValue(value, $"{friendly}（错误编号 {errorId}）");
            }
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    private static string SanitizeForUser(string message)
    {
        var m = (message ?? string.Empty).Trim();
        if (m.Length == 0)
            return "操作失败，请稍后重试";

        // EF 外层废话 → 友好文案（详情在 sys_error_log）
        if (m.Contains("See the inner exception", StringComparison.OrdinalIgnoreCase)
            || m.Contains("An error occurred while saving the entity changes", StringComparison.OrdinalIgnoreCase))
            return "保存失败，请稍后重试或联系管理员";

        if (m.Length > 200)
            m = m[..200] + "…";
        return m;
    }
}
