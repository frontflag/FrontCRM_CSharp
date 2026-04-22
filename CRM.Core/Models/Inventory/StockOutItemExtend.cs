using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 出库明细扩展：与 <see cref="StockOutItem"/> 一对一（主键同明细 <c>ItemId</c>）。
    /// 记录拣货层（<c>StockItemId</c>、<c>Type</c>）及过账时采销价、币别、折合 USD 与业务利润快照。
    /// </summary>
    [Table("stock_out_item_extend")]
    public class StockOutItemExtend : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("StockOutItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>拣货绑定的在库明细主键（<c>stockitem.StockItemId</c>）。</summary>
        [StringLength(36)]
        [Column("StockItemId")]
        public string? StockItemId { get; set; }

        /// <summary>对应入库明细主键（冗余自 <see cref="StockInItem.Id"/> / <c>stock_in_item.ItemId</c>，经在库层 <see cref="StockItem.StockInItemId"/>）。</summary>
        [StringLength(36)]
        [Column("StockInItemId")]
        public string? StockInItemId { get; set; }

        /// <summary>入库明细业务编号（冗余自 <c>stock_in_item.stock_in_item_code</c>，可与 <see cref="StockItem.StockInItemCode"/> 对齐）。</summary>
        [StringLength(64)]
        [Column("stock_in_item_code")]
        public string? StockInItemCode { get; set; }

        /// <summary>库存层类型（与 <see cref="StockItem.StockType"/> / <c>stockitem.Type</c> 一致）。</summary>
        [Column("Type")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short StockType { get; set; } = 1;

        [StringLength(36)]
        [Column("sell_order_item_id")]
        public string? SellOrderItemId { get; set; }

        [StringLength(64)]
        [Column("sell_order_item_code")]
        public string? SellOrderItemCode { get; set; }

        [StringLength(36)]
        [Column("purchase_order_item_id")]
        public string? PurchaseOrderItemId { get; set; }

        [StringLength(64)]
        [Column("purchase_order_item_code")]
        public string? PurchaseOrderItemCode { get; set; }

        /// <summary>本行出库数量快照（与执行出库数量口径一致）。</summary>
        public int QtyStockOut { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal PurchasePrice { get; set; }

        /// <summary>采购单价币别（与 <see cref="CurrencyCode"/> 一致）。</summary>
        public short PurchaseCurrency { get; set; } = (short)CurrencyCode.RMB;

        /// <summary>采购单价折合 USD（过账时按财务基准汇率计算）。</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal PurchasePriceUsd { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal? SalesPrice { get; set; }

        public short? SalesCurrency { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal? SalesPriceUsd { get; set; }

        /// <summary>出库业务 USD 利润快照。</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ProfitOutBizUsd { get; set; }

        /// <summary>出库明细（主键与 <see cref="Id"/> / <c>StockOutItemId</c> 相同）。</summary>
        public virtual StockOutItem? StockOutItem { get; set; }
    }
}
