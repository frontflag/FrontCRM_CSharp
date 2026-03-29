using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.Purchase
{
    /// <summary>
    /// 采购明细扩展：货/票/款汇总及到货通知余量（与 purchaseorderitem 一对一，主键同明细 Id）
    /// </summary>
    [Table("purchaseorderitemextend")]
    public class PurchaseOrderItemExtend : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("PurchaseOrderItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>剩余可下达货通知数量（文档 QtyStockInNotifyNot，由业务重算）</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockInNotifyNot { get; set; }

        /// <summary>累计预期到货（所有批次 ExpectQty 之和）</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockInNotifyExpectSum { get; set; }

        /// <summary>累计实收数量（各批次 ReceiveQty 之和）</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyReceiveTotal { get; set; }

        // --- 票（进项）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceDone { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceToBe { get; set; }

        // --- 款（采购付款）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountFinish { get; set; }

        // --- 关联销售侧收款汇总（以销定采对账用，可选维护）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountFinish { get; set; }
    }
}
