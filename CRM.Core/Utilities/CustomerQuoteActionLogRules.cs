namespace CRM.Core.Utilities;

/// <summary>客户报价单预览页「打印 / 导出 / 发送邮件」日志口径。</summary>
public static class CustomerQuoteActionLogRules
{
    public const string Print = "打印";
    public const string Export = "导出";
    public const string SendEmail = "发送邮件";
    public const string InvalidActionMessage = "不支持的日志操作";

    public const string ClientPrint = "print";
    public const string ClientExport = "export";

    public static readonly string[] DisplayActions = [Print, Export, SendEmail];

    public static bool IsDisplayAction(string? actionType) =>
        actionType == Print || actionType == Export || actionType == SendEmail;

    /// <summary>预览页只允许上报打印、导出；发送邮件由服务端在发信成功后写入。</summary>
    public static string ParseClientAction(string? action)
    {
        var key = (action ?? string.Empty).Trim().ToLowerInvariant();
        return key switch
        {
            ClientPrint or "打印" => Print,
            ClientExport or "export-pdf" or "导出" => Export,
            _ => throw new InvalidOperationException(InvalidActionMessage)
        };
    }
}
