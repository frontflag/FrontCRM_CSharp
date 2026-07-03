using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.RFQ;

namespace CRM.Core.Utilities;

/// <summary>需求字段变更日志写入（状态回写等可复用）。</summary>
public static class RfqFieldChangeLogWriter
{
    public static string FormatRfqMainStatus(short status) => status switch
    {
        (short)RfqMainStatus.PendingAssign => "待分配",
        (short)RfqMainStatus.Assigned => "已分配",
        (short)RfqMainStatus.Quoting => "报价中",
        (short)RfqMainStatus.Quoted => "已报价",
        (short)RfqMainStatus.PriceSelected => "已选价",
        (short)RfqMainStatus.ConvertedToOrder => "已转订单",
        (short)RfqMainStatus.LegacyObsoleteClosed => "已关闭",
        (short)RfqMainStatus.Closed => "已关闭",
        (short)RfqMainStatus.Cancelled => "已取消",
        _ => status.ToString()
    };

    public static string FormatRfqItemStatus(short status) => status switch
    {
        (short)RfqItemStatus.Pending => "待报价",
        (short)RfqItemStatus.Quoted => "已报价",
        (short)RfqItemStatus.Accepted => "已接受",
        (short)RfqItemStatus.Rejected => "已拒绝",
        (short)RfqItemStatus.Closed => "已关闭",
        (short)RfqItemStatus.NoQuoteFound => "查无报价",
        _ => status.ToString()
    };

    public static async Task AppendRfqStatusChangeAsync(
        IUnitOfWork unitOfWork,
        RFQ rfq,
        short oldStatus,
        short newStatus,
        string? changedByUserId,
        string changedByUserName)
    {
        if (oldStatus == newStatus)
            return;
        await FieldChangeLogAppender.AppendIfChangedAsync(
            unitOfWork,
            BusinessLogTypes.Rfq,
            rfq.Id,
            rfq.RfqCode,
            "status",
            "需求状态",
            FormatRfqMainStatus(oldStatus),
            FormatRfqMainStatus(newStatus),
            changedByUserId,
            changedByUserName);
    }
}
