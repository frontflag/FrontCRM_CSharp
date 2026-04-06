using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Sales
{
    /// <summary>
    /// 销售订单主表 (SellOrder)
    /// 对应数据库表: sellorder
    /// 主状态: <see cref="SellOrderMainStatus"/>
    /// </summary>
    [Table("sellorder")]
    public class SellOrder : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("SellOrderId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>销售单号(唯一)</summary>
        [Required]
        [StringLength(32)]
        [Column("sell_order_code")]
        public string SellOrderCode { get; set; } = string.Empty;

        /// <summary>客户ID</summary>
        [Required]
        [StringLength(36)]
        [Column("customer_id")]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>客户名称(冗余)</summary>
        [StringLength(200)]
        [Column("customer_name")]
        public string? CustomerName { get; set; }

        /// <summary>业务员ID</summary>
        [StringLength(36)]
        [Column("sales_user_id")]
        public string? SalesUserId { get; set; }

        /// <summary>业务员名称(冗余)</summary>
        [StringLength(100)]
        [Column("sales_user_name")]
        public string? SalesUserName { get; set; }

        /// <summary>采购员组ID</summary>
        [StringLength(36)]
        [Column("purchase_group_id")]
        public string? PurchaseGroupId { get; set; }

        /// <summary>订单主状态</summary>
        [Column("status")]
        public SellOrderMainStatus Status { get; set; } = SellOrderMainStatus.New;

        /// <summary>异常状态</summary>
        [Column("err_status")]
        public short ErrStatus { get; set; } = 0;

        /// <summary>订单类型 1=客单采购 2=备货采购 3=样品采购</summary>
        [Column("type")]
        public short Type { get; set; } = 1;

        /// <summary>币别 1=RMB 2=USD 3=EUR</summary>
        [Column("currency")]
        public short Currency { get; set; } = 1;

        /// <summary>销售总额</summary>
        [Column("total", TypeName = "numeric(18,2)")]
        public decimal Total { get; set; } = 0.00m;

        /// <summary>折算总额(本位币)</summary>
        [Column("convert_total", TypeName = "numeric(18,2)")]
        public decimal ConvertTotal { get; set; } = 0.00m;

        /// <summary>行项目数</summary>
        [Column("item_rows")]
        public int ItemRows { get; set; } = 0;

        /// <summary>采购订单状态汇总 0=未采购 1=部分采购 2=全部采购</summary>
        [Column("purchase_order_status")]
        public short PurchaseOrderStatus { get; set; } = 0;

        /// <summary>出库状态 0=未出库 1=部分出库 2=全部出库</summary>
        [Column("stock_out_status")]
        public short StockOutStatus { get; set; } = 0;

        /// <summary>入库状态 0=未入库 1=部分入库 2=全部入库</summary>
        [Column("stock_in_status")]
        public short StockInStatus { get; set; } = 0;

        /// <summary>收款状态 0=未收款 1=部分收款 2=全部收款</summary>
        [Column("finance_receipt_status")]
        public short FinanceReceiptStatus { get; set; } = 0;

        /// <summary>付款状态 0=未付款 1=部分付款 2=全部付款</summary>
        [Column("finance_payment_status")]
        public short FinancePaymentStatus { get; set; } = 0;

        /// <summary>开票状态 0=未开票 1=部分开票 2=全部开票</summary>
        [Column("invoice_status")]
        public short InvoiceStatus { get; set; } = 0;

        /// <summary>送货地址</summary>
        [StringLength(500)]
        [Column("delivery_address")]
        public string? DeliveryAddress { get; set; }

        /// <summary>交货日期</summary>
        [Column("delivery_date")]
        public DateTime? DeliveryDate { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [Column("comment")]
        public string? Comment { get; set; }

        /// <summary>审核拒绝原因（审核失败时由审批人填写）</summary>
        [StringLength(500)]
        [Column("audit_remark")]
        public string? AuditRemark { get; set; }

        /// <summary>创建订单时的登录用户 ID（<c>user</c> 表主键 GUID，与 JWT 一致）；基类 <c>CreateUserId</c>(bigint) 与当前用户体系不一致，勿用于关联用户。</summary>
        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        /// <summary>最后修改订单时的登录用户 ID（GUID）</summary>
        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        // 导航属性
        public virtual ICollection<SellOrderItem> Items { get; set; } = new List<SellOrderItem>();
    }

    /// <summary>
    /// 销售订单明细表 (SellOrderItem)
    /// 对应数据库表: sellorderitem
    /// </summary>
    [Table("sellorderitem")]
    public class SellOrderItem : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("SellOrderItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>销售订单ID(外键)</summary>
        [Required]
        [StringLength(36)]
        [Column("sell_order_id")]
        public string SellOrderId { get; set; } = string.Empty;

        /// <summary>销售订单明细业务编号（销售单号-序号）</summary>
        [StringLength(64)]
        [Column("sell_order_item_code")]
        public string SellOrderItemCode { get; set; } = string.Empty;

        /// <summary>报价ID(来源)</summary>
        [StringLength(36)]
        [Column("quote_id")]
        public string? QuoteId { get; set; }

        /// <summary>商品/物料ID</summary>
        [StringLength(36)]
        [Column("product_id")]
        public string? ProductId { get; set; }

        /// <summary>物料型号(PN)</summary>
        [StringLength(200)]
        [Column("pn")]
        public string? PN { get; set; }

        /// <summary>品牌</summary>
        [StringLength(200)]
        [Column("brand")]
        public string? Brand { get; set; }

        /// <summary>客户料号</summary>
        [StringLength(200)]
        [Column("customer_pn_no")]
        public string? CustomerPnNo { get; set; }

        /// <summary>销售数量</summary>
        [Column("qty", TypeName = "numeric(18,4)")]
        public decimal Qty { get; set; } = 0.0000m;

        /// <summary>已采购数量(实时统计)</summary>
        [Column("purchased_qty", TypeName = "numeric(18,4)")]
        public decimal PurchasedQty { get; set; } = 0.0000m;

        /// <summary>销售单价</summary>
        [Column("price", TypeName = "numeric(18,6)")]
        public decimal Price { get; set; } = 0.000000m;

        /// <summary>单价折合美元（按财务参数中的 USD 基准汇率计算）</summary>
        [Column("convert_price", TypeName = "numeric(18,6)")]
        public decimal ConvertPrice { get; set; } = 0.000000m;

        /// <summary>币别 1=RMB 2=USD 3=EUR 4=HKD（与 <see cref="Constants.CurrencyCode"/> 一致）</summary>
        [Column("currency")]
        public short Currency { get; set; } = 1;

        /// <summary>生产日期要求</summary>
        [StringLength(100)]
        [Column("date_code")]
        public string? DateCode { get; set; }

        /// <summary>交货日期</summary>
        [Column("delivery_date")]
        public DateTime? DeliveryDate { get; set; }

        /// <summary>明细状态 0=正常 1=已取消</summary>
        [Column("status")]
        public short Status { get; set; } = 0;

        /// <summary>备注</summary>
        [StringLength(500)]
        [Column("comment")]
        public string? Comment { get; set; }

        // 导航属性
        public virtual SellOrder? SellOrder { get; set; }
    }
}
