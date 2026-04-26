namespace CRM.Core.Models.RFQ
{
    /// <summary>
    /// RFQ 主表 <see cref="RFQ.Status"/> 权威取值（与 DB、前端、文档一致）。
    /// </summary>
    public enum RfqMainStatus : short
    {
        /// <summary>待分配</summary>
        PendingAssign = 0,

        /// <summary>已分配</summary>
        Assigned = 1,

        /// <summary>报价中</summary>
        Quoting = 2,

        /// <summary>已报价</summary>
        Quoted = 3,

        /// <summary>已选价</summary>
        PriceSelected = 4,

        /// <summary>已转订单</summary>
        ConvertedToOrder = 5,

        /// <summary>
        /// 历史「已关闭」占位码，<b>已废弃</b>；新数据必须使用 <see cref="Closed"/>。
        /// 服务层在更新状态时将 <c>6</c> 归一为 <c>7</c>；存量数据由迁移脚本统一为 <c>7</c>。
        /// </summary>
        LegacyObsoleteClosed = 6,

        /// <summary>已关闭：已生成销售订单，需求完成使命（终态）。</summary>
        Closed = 7,

        /// <summary>已取消：不再继续执行，终止使命，保留记录留痕（终态）。</summary>
        Cancelled = 8,
    }
}
