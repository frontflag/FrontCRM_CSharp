namespace CRM.Core.Constants
{
    /// <summary>
    /// 采购订单主表状态（与 <see cref="Models.Purchase.PurchaseOrder"/> 注释一致）。
    /// </summary>
    public static class PurchaseOrderMainStatusCodes
    {
        /// <summary>已确认（供应商确认）及之后（30/50/100）才允许销售明细侧「申请出库」。</summary>
        public const short VendorConfirmedOrBeyond = 30;
    }
}
