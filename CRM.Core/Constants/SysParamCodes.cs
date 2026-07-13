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
    }
}
