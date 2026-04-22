using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 在库明细层：与 <see cref="StockInItem"/> 1:1，数量与采销冗余的事实来源；汇总至 <see cref="StockInfo"/>（<c>StockAggregateId</c>）。
    /// </summary>
    [Table("stock_item")]
    public class StockItem : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("StockItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>对应入库明细主键，全局唯一。</summary>
        [Required]
        [StringLength(36)]
        public string StockInItemId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string StockInId { get; set; } = string.Empty;

        /// <summary>分桶汇总行 <see cref="StockInfo.Id"/>。</summary>
        [Required]
        [StringLength(36)]
        public string StockAggregateId { get; set; } = string.Empty;

        /// <summary>在库明细业务编号（与分桶 <c>StockCode</c> 一致：<c>{StockCode}-{行序号}</c>，生成规则同 <see cref="StockInItem.StockInItemCode"/>）。</summary>
        [StringLength(64)]
        [Column("stock_item_code")]
        public string? StockItemCode { get; set; }

        /// <summary>对应入库明细业务编号（冗余自 <see cref="StockInItem.StockInItemCode"/>，便于列表/拣货少一次关联）。</summary>
        [StringLength(64)]
        [Column("stock_in_item_code")]
        public string? StockInItemCode { get; set; }

        [Required]
        [StringLength(36)]
        public string MaterialId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        [StringLength(36)]
        public string? LocationId { get; set; }

        [StringLength(50)]
        public string? BatchNo { get; set; }

        public DateTime? ProductionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Column("Type")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short StockType { get; set; } = 1;

        [Column("RegionType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;

        [StringLength(200)]
        [Column("purchase_pn")]
        public string? PurchasePn { get; set; }

        [StringLength(200)]
        [Column("purchase_brand")]
        public string? PurchaseBrand { get; set; }

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

        [StringLength(36)]
        public string? VendorId { get; set; }

        [StringLength(200)]
        public string? VendorName { get; set; }

        [StringLength(36)]
        public string? PurchaserId { get; set; }

        [StringLength(100)]
        public string? PurchaserName { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal PurchasePrice { get; set; }

        /// <summary>采购单价币别（与 <see cref="Constants.CurrencyCode"/> 一致）。</summary>
        public short PurchaseCurrency { get; set; } = (short)CurrencyCode.RMB;

        /// <summary>采购单价折合 USD（入库过账时按财务基准汇率计算）。</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal PurchasePriceUsd { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseAmount { get; set; }

        [StringLength(36)]
        public string? CustomerId { get; set; }

        [StringLength(200)]
        public string? CustomerName { get; set; }

        [StringLength(36)]
        public string? SalespersonId { get; set; }

        [StringLength(100)]
        public string? SalespersonName { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal? SalesPrice { get; set; }

        /// <summary>销售单价币别；无销售订单行时为 null。</summary>
        public short? SalesCurrency { get; set; }

        /// <summary>销售单价折合 USD；无销售订单行时为 null。</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal? SalesPriceUsd { get; set; }

        private int _qtyInbound;

        private int _qtyStockOut;

        private short _stockOutStatus;

        private decimal _profitOutBizUsd;

        /// <summary>
        /// 入库时写入的 USD 价差快照：有销售行且 <see cref="SalesPriceUsd"/> 有值时为
        /// <c>(SalesPriceUsd − PurchasePriceUsd) × QtyInbound</c>，否则为 0。
        /// 出库业务利润以 <c>stockoutitemextend.ProfitOutBizUsd</c> 为准，不在出库时刷新本列。
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ProfitOutBizUsd
        {
            get => _profitOutBizUsd;
            private set
            {
                if (_profitOutBizUsd == value)
                    return;
                _profitOutBizUsd = value;
            }
        }

        /// <summary>入库数量；变更时会刷新 <see cref="StockOutStatus"/> 与 <see cref="ProfitOutBizUsd"/>（入库快照）。</summary>
        public int QtyInbound
        {
            get => _qtyInbound;
            set
            {
                if (_qtyInbound == value)
                    return;
                _qtyInbound = value;
                RefreshStockOutStatusIfNeeded();
                SyncProfitOutBizUsdFromInboundSnapshot();
            }
        }

        /// <summary>已出库数量；变更时会刷新 <see cref="StockOutStatus"/>（不刷新 <see cref="ProfitOutBizUsd"/>）。</summary>
        public int QtyStockOut
        {
            get => _qtyStockOut;
            set
            {
                if (_qtyStockOut == value)
                    return;
                _qtyStockOut = value;
                RefreshStockOutStatusIfNeeded();
            }
        }

        /// <summary>
        /// 出库状态：0=无有效入库（<see cref="QtyInbound"/>≤0）；1=未出库；2=部分出库；3=出库完成。
        /// 由 <see cref="QtyInbound"/> 与 <see cref="QtyStockOut"/> 推导并持久化；请勿直接改后备字段。
        /// </summary>
        public short StockOutStatus
        {
            get => _stockOutStatus;
            private set
            {
                if (_stockOutStatus == value)
                    return;
                _stockOutStatus = value;
            }
        }

        /// <summary>与列表/API 筛选逻辑一致的状态计算（单一来源）。</summary>
        public static short ComputeStockOutStatus(int qtyInbound, int qtyStockOut)
        {
            if (qtyInbound <= 0)
                return 0;
            if (qtyStockOut <= 0)
                return 1;
            if (qtyStockOut < qtyInbound)
                return 2;
            return 3;
        }

        private void RefreshStockOutStatusIfNeeded() =>
            StockOutStatus = ComputeStockOutStatus(_qtyInbound, _qtyStockOut);

        /// <summary>
        /// 价差 × 数量（入库层用 <see cref="QtyInbound"/>；出库扩展行用本条出库数量）。无销售行、无销售 USD 快照或数量≤0 时为 0。
        /// </summary>
        public static decimal ComputeProfitOutBizUsd(
            string? sellOrderItemId,
            decimal? salesPriceUsd,
            decimal purchasePriceUsd,
            int qtyForMargin)
        {
            if (string.IsNullOrWhiteSpace(sellOrderItemId) || qtyForMargin <= 0)
                return 0m;
            if (!salesPriceUsd.HasValue)
                return 0m;
            var raw = (salesPriceUsd.GetValueOrDefault() - purchasePriceUsd) * qtyForMargin;
            return Math.Round(raw, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>EF 物化后或绕过 setter 写入数量后调用，使 <see cref="StockOutStatus"/> 与数量一致。</summary>
        public void SyncStockOutStatusFromQuantities() =>
            StockOutStatus = ComputeStockOutStatus(_qtyInbound, _qtyStockOut);

        /// <summary>使 <see cref="ProfitOutBizUsd"/> 与快照价、<see cref="QtyInbound"/> 一致（入库口径）。</summary>
        public void SyncProfitOutBizUsdFromInboundSnapshot() =>
            ProfitOutBizUsd = ComputeProfitOutBizUsd(SellOrderItemId, SalesPriceUsd, PurchasePriceUsd, _qtyInbound);

        /// <summary>物化后与数量、快照价对齐派生持久化列（出库状态、入库毛利快照）。</summary>
        public void SyncDenormalizedComputedFields()
        {
            SyncStockOutStatusFromQuantities();
            SyncProfitOutBizUsdFromInboundSnapshot();
        }

        public int QtyOccupy { get; set; }

        public int QtySales { get; set; }

        public int QtyRepertory { get; set; }

        public int QtyRepertoryAvailable { get; set; }
    }
}
