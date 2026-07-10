namespace CRM.Core.Interfaces;

/// <summary>采购订单主表 status 与明细扩展执行进度对齐。</summary>
public interface IPurchaseOrderMainStatusSyncService
{
    /// <summary>
    /// 按未取消明细扩展进度重算 <c>purchaseorder.status</c>。
    /// 已确认(30)起：任一明细部分付款/部分入库→进行中(50)；全部采购完成→采购完成(100)。
    /// 取消/审核失败不自动改写。
    /// </summary>
    /// <returns>主状态是否发生变更。</returns>
    Task<bool> TrySyncOrderMainStatusAsync(string purchaseOrderId, CancellationToken cancellationToken = default);
}
