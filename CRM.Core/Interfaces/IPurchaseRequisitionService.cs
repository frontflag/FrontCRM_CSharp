using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces
{
    /// <summary>采购申请服务接口</summary>
    public interface IPurchaseRequisitionService
    {
        /// <summary>
        /// 根据销售订单获取「可申请采购」的明细行选项。口径与 <see cref="CreateAsync"/> 一致：
        /// remainingQty = 销售明细数量 − 已下采购量 − 进行中采购申请量（状态 0/1）；≤0 不返回。允许同一明细多次申请，但总占用不超过剩余可采。
        /// </summary>
        Task<IEnumerable<SellOrderLineOptionDto>> GetSellOrderLineOptionsAsync(string sellOrderId);

        /// <summary>创建采购申请（单行）</summary>
        Task<PurchaseRequisition> CreateAsync(CreatePurchaseRequisitionRequest request, string? actingUserId = null);

        /// <summary>以销定采：自动生成采购申请（简化实现：为剩余明细创建）</summary>
        Task<IEnumerable<PurchaseRequisition>> AutoGenerateAsync(string sellOrderId, string purchaseUserId, string? actingUserId = null);

        /// <summary>重新计算（占位：当前简化实现不做处理）</summary>
        Task RecalculateAsync(string id);
    }

    public class CreatePurchaseRequisitionRequest
    {
        public string SellOrderItemId { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public DateTime ExpectedPurchaseTime { get; set; }
        public short Type { get; set; } = 0; // 0=专属 1=公开备货
        public string? PurchaseUserId { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>销售订单明细行的申请采购选项</summary>
    public class SellOrderLineOptionDto
    {
        public string sellOrderItemId { get; set; } = string.Empty;
        public string? pn { get; set; }
        public string? brand { get; set; }
        public decimal salesOrderQty { get; set; }
        /// <summary>已下采购订单明细数量合计（同一销售明细）</summary>
        public decimal purchasedQty { get; set; }
        /// <summary>进行中采购申请数量合计（状态 0=新建 1=部分完成）</summary>
        public decimal openPurchaseRequisitionQty { get; set; }
        /// <summary>仍可申请的采购数量上限（前端仅展示，以服务端校验为准）</summary>
        public decimal remainingQty { get; set; }
    }
}
