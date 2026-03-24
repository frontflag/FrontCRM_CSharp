namespace CRM.Core.Models.Sales
{
    /// <summary>
    /// 销售订单主状态（与 sellorder.status smallint 一致）
    /// </summary>
    public enum SellOrderMainStatus : short
    {
        New = 1, // 新建
        PendingAudit = 2, // 待审核
        Approved = 10, // 审核通过
        InProgress = 20, // 进行中
        Completed = 100, // 完成
        AuditFailed = -1, // 审核失败
        Cancelled = -2 // 取消
    }
}
