using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models.Sales;

namespace CRM.Core.Models.Purchase
{
    /// <summary>
    /// 采购订单主表 (PurchaseOrder)
    /// 对应数据库表: purchaseorder
    /// 状态: 1=新建 2=待审核 10=审核通过 20=待确认 30=已确认 50=进行中 100=采购完成 -1=审核失败 -2=取消
    /// </summary>
    [Table("purchaseorder")]
    public class PurchaseOrder : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("PurchaseOrderId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>采购单号(唯一)</summary>
        [Required]
        [StringLength(32)]
        [Column("purchase_order_code")]
        public string PurchaseOrderCode { get; set; } = string.Empty;

        /// <summary>供应商ID</summary>
        [Required]
        [StringLength(36)]
        [Column("vendor_id")]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>供应商名称(冗余)</summary>
        [StringLength(200)]
        [Column("vendor_name")]
        public string? VendorName { get; set; }

        /// <summary>供应商编号</summary>
        [StringLength(50)]
        [Column("vendor_code")]
        public string? VendorCode { get; set; }

        /// <summary>供应商联系人ID</summary>
        [StringLength(36)]
        [Column("vendor_contact_id")]
        public string? VendorContactId { get; set; }

        /// <summary>采购员ID</summary>
        [StringLength(36)]
        [Column("purchase_user_id")]
        public string? PurchaseUserId { get; set; }

        /// <summary>采购员名称(冗余)</summary>
        [StringLength(100)]
        [Column("purchase_user_name")]
        public string? PurchaseUserName { get; set; }

        /// <summary>采购组ID</summary>
        [StringLength(36)]
        [Column("purchase_group_id")]
        public string? PurchaseGroupId { get; set; }

        /// <summary>业务员组ID(关联销售)</summary>
        [StringLength(36)]
        [Column("sales_group_id")]
        public string? SalesGroupId { get; set; }

        /// <summary>订单状态 1=新建 2=待审核 10=审核通过 20=待确认 30=已确认 50=进行中 100=采购完成 -1=审核失败 -2=取消</summary>
        [Column("status")]
        public short Status { get; set; } = 1;

        /// <summary>异常状态</summary>
        [Column("err_status")]
        public short ErrStatus { get; set; } = 0;

        /// <summary>
        /// 订单类型 1=客单采购 2=备货采购 3=样品采购。
        /// 客单：由销售明细/采购申请链路生成且明细带销售行关联；备货：无销售明细关联的直采；样品：无销售关联时可选 3。
        /// </summary>
        [Column("type")]
        public short Type { get; set; } = 1;

        /// <summary>币别 1=RMB 2=USD 3=EUR</summary>
        [Column("currency")]
        public short Currency { get; set; } = 1;

        /// <summary>采购总额</summary>
        [Column("total", TypeName = "numeric(18,2)")]
        public decimal Total { get; set; } = 0.00m;

        /// <summary>折算总额(本位币)</summary>
        [Column("convert_total", TypeName = "numeric(18,2)")]
        public decimal ConvertTotal { get; set; } = 0.00m;

        /// <summary>行项目数</summary>
        [Column("item_rows")]
        public int ItemRows { get; set; } = 0;

        /// <summary>入库状态 0=未入库 1=部分入库 2=全部入库</summary>
        [Column("stock_status")]
        public short StockStatus { get; set; } = 0;

        /// <summary>付款状态 0=未付款 1=部分付款 2=全部付款</summary>
        [Column("finance_status")]
        public short FinanceStatus { get; set; } = 0;

        /// <summary>出库状态 0=未出库 1=部分出库 2=全部出库</summary>
        [Column("stock_out_status")]
        public short StockOutStatus { get; set; } = 0;

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

        /// <summary>内部备注</summary>
        [StringLength(500)]
        [Column("inner_comment")]
        public string? InnerComment { get; set; }

        /// <summary>创建订单时的登录用户 ID（<c>user</c> 表主键 GUID，与 JWT 一致）；基类 <c>CreateUserId</c>(bigint) 勿用于关联用户。</summary>
        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        /// <summary>最后修改订单时的登录用户 ID（GUID）</summary>
        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        // 导航属性
        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }

    /// <summary>
    /// 采购订单明细表 (PurchaseOrderItem)
    /// 对应数据库表: purchaseorderitem
    /// 核心关联字段: SellOrderItemId — 以销定采的关键
    /// </summary>
    [Table("purchaseorderitem")]
    public class PurchaseOrderItem : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("PurchaseOrderItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>采购订单ID(外键)</summary>
        [Required]
        [StringLength(36)]
        [Column("purchase_order_id")]
        public string PurchaseOrderId { get; set; } = string.Empty;

        /// <summary>采购订单明细业务编号（采购单号-序号）</summary>
        [StringLength(64)]
        [Column("purchase_order_item_code")]
        public string PurchaseOrderItemCode { get; set; } = string.Empty;

        /// <summary>销售订单明细ID(外键) — 以销定采；备货/手工行可为空</summary>
        [StringLength(36)]
        [Column("sell_order_item_id")]
        public string? SellOrderItemId { get; set; }

        /// <summary>供应商ID</summary>
        [Required]
        [StringLength(36)]
        [Column("vendor_id")]
        public string VendorId { get; set; } = string.Empty;

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

        /// <summary>采购数量</summary>
        [Column("qty", TypeName = "numeric(18,4)")]
        public decimal Qty { get; set; } = 0.0000m;

        /// <summary>采购单价(成本)</summary>
        [Column("cost", TypeName = "numeric(18,6)")]
        public decimal Cost { get; set; } = 0.000000m;

        /// <summary>单价折合美元（按财务参数中的 USD 基准汇率计算）</summary>
        [Column("convert_price", TypeName = "numeric(18,6)")]
        public decimal ConvertPrice { get; set; } = 0.000000m;

        /// <summary>币别 1=RMB 2=USD 3=EUR 4=HKD（与 <see cref="Constants.CurrencyCode"/> 一致）</summary>
        [Column("currency")]
        public short Currency { get; set; } = 1;

        /// <summary>明细状态 1=新建 2=待审核 10=审核通过 20=待确认 30=已确认 40=已付款 50=已发货 60=已入库 100=采购完成 -1=审核失败 -2=取消</summary>
        [Column("status")]
        public short Status { get; set; } = 1;

        /// <summary>入库状态 0=未入库 1=部分入库 2=全部入库</summary>
        [Column("stock_in_status")]
        public short StockInStatus { get; set; } = 0;

        /// <summary>付款状态 0=未付款 1=部分付款 2=全部付款</summary>
        [Column("finance_payment_status")]
        public short FinancePaymentStatus { get; set; } = 0;

        /// <summary>出库状态 0=未出库 1=部分出库 2=全部出库</summary>
        [Column("stock_out_status")]
        public short StockOutStatus { get; set; } = 0;

        /// <summary>异常状态</summary>
        [Column("err_status")]
        public short ErrStatus { get; set; } = 0;

        /// <summary>交货日期</summary>
        [Column("delivery_date")]
        public DateTime? DeliveryDate { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [Column("comment")]
        public string? Comment { get; set; }

        /// <summary>内部备注</summary>
        [StringLength(500)]
        [Column("inner_comment")]
        public string? InnerComment { get; set; }

        // 导航属性
        public virtual PurchaseOrder? PurchaseOrder { get; set; }
        public virtual SellOrderItem? SellOrderItem { get; set; }
    }
}
