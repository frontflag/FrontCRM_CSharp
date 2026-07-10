namespace CRM.Core.Interfaces;

/// <summary>销售订单主表 status 与明细扩展执行进度对齐（列表「状态」列同源）。</summary>
public interface ISellOrderMainStatusSyncService
{
    /// <summary>
    /// 按未取消明细的扩展进度重算 <c>sellorder.status</c>。
    /// 新建/待审核/取消/审核失败不自动改写；已审核起可根据执行链上调或纠正「完成」。
    /// </summary>
    /// <returns>主状态是否发生变更。</returns>
    Task<bool> TrySyncOrderMainStatusAsync(string sellOrderId, CancellationToken cancellationToken = default);
}
