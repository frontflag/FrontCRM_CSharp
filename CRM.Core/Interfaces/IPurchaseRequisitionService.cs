using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces
{
    /// <summary>采购申请服务接口</summary>
    public interface IPurchaseRequisitionService
    {
        /// <summary>根据销售订单获取“可申请采购”的明细行选项</summary>
        Task<IEnumerable<SellOrderLineOptionDto>> GetSellOrderLineOptionsAsync(string sellOrderId);

        /// <summary>创建采购申请（单行）</summary>
        Task<PurchaseRequisition> CreateAsync(CreatePurchaseRequisitionRequest request);

        /// <summary>以销定采：自动生成采购申请（简化实现：为剩余明细创建）</summary>
        Task<IEnumerable<PurchaseRequisition>> AutoGenerateAsync(string sellOrderId, string purchaseUserId);

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
        public decimal remainingQty { get; set; }
    }
}
