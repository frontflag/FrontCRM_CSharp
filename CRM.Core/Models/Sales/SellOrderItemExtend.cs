using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.Sales
{
    /// <summary>
    /// 销售明细扩展：货/票/款汇总及出货通知数量（与 sellorderitem 一对一，主键同明细 Id）
    /// </summary>
    [Table("sellorderitemextend")]
    public class SellOrderItemExtend : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("SellOrderItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>已下达出库通知数量累计</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockOutNotify { get; set; }

        /// <summary>剩余可下达出库通知数量</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockOutNotifyNot { get; set; }

        // --- 销项发票 ---
        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceAmountFinish { get; set; }

        // --- 收款 ---
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountFinish { get; set; }

        // --- 付款（采购侧，对销售明细维度汇总时可维护）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountDone { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountToBe { get; set; }

        // --- 进项发票（镜像）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceDone { get; set; }
    }
}
