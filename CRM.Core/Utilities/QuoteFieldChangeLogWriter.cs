using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Quote;

namespace CRM.Core.Utilities;

/// <summary>报价字段变更日志写入（供 <see cref="Services.QuoteStatusSyncService"/> 等复用）。</summary>
public static class QuoteFieldChangeLogWriter
{
    public static string FormatQuoteStatus(short status) => status switch
    {
        (short)QuoteMainStatus.New => "新建",
        (short)QuoteMainStatus.Won => "成单",
        (short)QuoteMainStatus.Closed => "关闭",
        _ => status.ToString()
    };

    public static async Task AppendQuoteStatusChangeAsync(
        IUnitOfWork unitOfWork,
        Quote quote,
        short oldStatus,
        short newStatus)
    {
        if (oldStatus == newStatus)
            return;
        await FieldChangeLogAppender.AppendIfChangedAsync(
            unitOfWork,
            BusinessLogTypes.Quote,
            quote.Id,
            quote.QuoteCode,
            "status",
            "报价状态",
            FormatQuoteStatus(oldStatus),
            FormatQuoteStatus(newStatus),
            null,
            "系统");
    }
}
