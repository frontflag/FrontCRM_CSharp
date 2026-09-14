namespace CRM.Core.Interfaces;

/// <summary>
/// 工作日程时间戳单独落库。这两列不参与 RFQ/销售订单实体映射，
/// 避免生产漏跑 DDL 时整表 SELECT 因缺列 42703 拖垮审批、订单、桌面。
/// </summary>
public interface IWorkCalendarStampWriter
{
    Task TrySetRfqAssignedAtAsync(string rfqId, DateTime utc, CancellationToken cancellationToken = default);

    Task TrySetSellOrderApprovedAtAsync(string sellOrderId, DateTime utc, CancellationToken cancellationToken = default);
}
