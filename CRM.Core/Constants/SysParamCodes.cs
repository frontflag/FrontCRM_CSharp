namespace CRM.Core.Constants
{
    /// <summary>
    /// 系统参数 ParamCode 常量
    /// </summary>
    public static class SysParamCodes
    {
        /// <summary>
        /// IANA 时区 ID，用于前端展示日期时间（如 Asia/Shanghai、UTC）
        /// </summary>
        public const string DisplayTimeZoneId = "System.Display.TimeZoneId";

        /// <summary>
        /// 未配置或无效时的默认显示时区
        /// </summary>
        public const string DefaultDisplayTimeZoneId = "Asia/Shanghai";

        /// <summary>参与需求明细轮询分配的 RBAC 角色编码，逗号分隔（如 purchase_buyer,purchase_staff）</summary>
        public const string RfqRoundRobinPurchaserRoleCodes = "System.RFQ.RoundRobinPurchaserRoleCodes";

        /// <summary>需求明细采购员轮询游标（非负整数，持久化在 ValueString）</summary>
        public const string RfqPurchaserRoundRobinCursor = "System.RFQ.PurchaserRoundRobinCursor";

        /// <summary>每条 RFQ 轮询分配的报价员人数（1 或 2，持久化在 ValueString）</summary>
        public const string RfqRoundRobinAssigneeCount = "System.RFQ.RoundRobinAssigneeCount";

        /// <summary>需求明细保护时长（分钟）；超过后任意采购员可见/可报价；0 表示关闭</summary>
        public const string RfqDemandProtectionMinutes = "System.RFQ.DemandProtectionMinutes";

        /// <summary>新建需求时分配方式默认值（2 条目轮询 / 3 品牌轮询 / 5 采报优先）</summary>
        public const string RfqDefaultAssignMethod = "System.RFQ.DefaultAssignMethod";

        /// <summary>
        /// 销售「刷新客户」是否允许同步已完成业务节点（出库通知已出库、装箱已完成、出库单已出库等）。Boolean，默认 false。
        /// </summary>
        public const string SalesAllowRefreshCompletedBizNodes = "System.Sales.AllowRefreshCompletedBizNodes";

        /// <summary>
        /// 采购「刷新供应商」是否允许同步已完成业务节点（到货已入库、入库已过账、付款已完成、进项已认证/冲红等）。Boolean，默认 false。
        /// </summary>
        public const string PurchaseAllowRefreshCompletedBizNodes = "System.Purchase.AllowRefreshCompletedBizNodes";
    }
}
