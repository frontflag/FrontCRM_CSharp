using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using System.Collections.Generic;

namespace CRM.Core.Models.Customs;

[Table("customs_broker")]
public class CustomsBroker : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(32)]
    public string BrokerCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Column("cname")]
    public string Cname { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("ename")]
    public string? Ename { get; set; }

    /// <summary>数据库列 <c>Type</c>：<see cref="CustomsBrokerServiceRegion"/>。</summary>
    [Column("Type")]
    [JsonPropertyName("type")]
    public short RegionType { get; set; } = CustomsBrokerServiceRegion.Shenzhen;

    public short Status { get; set; } = 1;

    /// <summary>报关代理费率：1+纯费率（如 1.03 表示 3%）。</summary>
    [Column("agency_rate", TypeName = "numeric(10,6)")]
    public decimal AgencyRate { get; set; } = 1m;

    /// <summary>装箱单收货人联系人。</summary>
    [StringLength(100)]
    [Column("contact_name")]
    public string? ContactName { get; set; }

    /// <summary>装箱单收货人电话。</summary>
    [StringLength(64)]
    [Column("tel")]
    public string? Tel { get; set; }

    /// <summary>装箱单收货人邮箱；空则报表印 —。</summary>
    [StringLength(200)]
    [Column("email")]
    public string? Email { get; set; }

    /// <summary>装箱单收货人地址（按需印出的原文）。</summary>
    [StringLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    [StringLength(36)]
    [Column("deleted_by_user_id")]
    public string? DeletedByUserId { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }
}

/// <summary>报关主单；V2 与报关装箱单 1:1（<see cref="PackingId"/>）。</summary>
[Table("customs_declaration")]
public class CustomsDeclaration : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("CustomsDeclarationId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(32)]
    public string DeclarationCode { get; set; } = string.Empty;

    /// <summary>报关装箱单主键（装箱确认时写入）。</summary>
    [StringLength(36)]
    [Column("packing_id")]
    public string? PackingId { get; set; }

    [Required]
    [StringLength(36)]
    public string CustomsBrokerId { get; set; } = string.Empty;

    public short DeclarationType { get; set; } = CustomsDeclarationType.Import;

    public short InternalStatus { get; set; } = CustomsDeclarationInternalStatus.Pending;

    public short CustomsClearanceStatus { get; set; } = CustomsClearanceStatusCodes.None;

    public DateTime DeclareDate { get; set; } = DateTime.UtcNow.Date;

    [Column(TypeName = "numeric(18,6)")]
    public decimal ExchangeRate { get; set; }

    /// <summary>试算时快照的代理费率（1+纯费率）。系统模式从报关公司覆盖；手工模式保留本列。</summary>
    [Column("broker_agency_rate", TypeName = "numeric(10,6)")]
    public decimal BrokerAgencyRate { get; set; } = 1m;

    /// <summary>false=用报关公司资料；true=本单手工。换报关公司时强制回 false。</summary>
    [Column("agency_rate_manual")]
    public bool AgencyRateManual { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalTaxAmount { get; set; }

    [Column("fees_calculated_at")]
    public DateTime? FeesCalculatedAt { get; set; }

    [Column("fees_locked")]
    public bool FeesLocked { get; set; }

    [Required]
    [StringLength(36)]
    public string FromWarehouseId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string ToWarehouseId { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Remark { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    public virtual StockTransfer? StockTransfer { get; set; }

    public virtual ICollection<CustomsDeclarationItem> Items { get; set; } = new List<CustomsDeclarationItem>();
}

[Table("customs_declaration_item")]
public class CustomsDeclarationItem : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("CustomsDeclarationItemId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string DeclarationId { get; set; } = string.Empty;

    public int LineNo { get; set; }

    /// <summary>拣货后回写；装箱生成报关明细时可为空。</summary>
    [StringLength(36)]
    public string? SourceStockItemId { get; set; }

    /// <summary>销售出库通知 <c>stockout_notify.ID</c>（Type=10）。</summary>
    [Required]
    [StringLength(36)]
    public string StockOutRequestId { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("customs_pendlist_id")]
    public string? CustomsPendlistId { get; set; }

    [StringLength(36)]
    [Column("customs_stockout_notify_id")]
    public string? CustomsStockOutNotifyId { get; set; }

    [StringLength(36)]
    [Column("packing_item_id")]
    public string? PackingItemId { get; set; }

    [Column("original_purchase_price", TypeName = "numeric(18,6)")]
    public decimal OriginalPurchasePrice { get; set; }

    [StringLength(36)]
    [Column("purchase_cost_param_id")]
    public string? PurchaseCostParamId { get; set; }

    [Column("purchase_ratio", TypeName = "numeric(10,4)")]
    public decimal PurchaseRatio { get; set; } = 1m;

    /// <summary>采购币别快照（<see cref="Constants.CurrencyCode"/>）。</summary>
    [Column("purchase_currency")]
    public short? PurchaseCurrency { get; set; }

    [Column("cost_usd", TypeName = "numeric(18,6)")]
    public decimal CostUsd { get; set; }

    [Column("duty_rate", TypeName = "numeric(18,6)")]
    public decimal DutyRate { get; set; }

    [Column("vat_rate", TypeName = "numeric(18,6)")]
    public decimal VatRate { get; set; } = 0.13m;

    [Column("customs_usd_price", TypeName = "numeric(18,6)")]
    public decimal CustomsUsdPrice { get; set; }

    [StringLength(36)]
    [Column("vendor_id")]
    public string? VendorId { get; set; }

    [Required]
    [StringLength(36)]
    public string MaterialId { get; set; } = string.Empty;

    /// <summary>物料型号 / PN 快照（与 <c>stockitem.purchase_pn</c> 口径一致）。</summary>
    [StringLength(200)]
    [Column("purchase_pn")]
    public string? PurchasePn { get; set; }

    /// <summary>品牌快照（与 <c>stockitem.purchase_brand</c> 口径一致）。</summary>
    [StringLength(200)]
    [Column("purchase_brand")]
    public string? PurchaseBrand { get; set; }

    /// <summary>客户 ID 快照（与 <c>sellorder.customer_id</c> 口径一致）。</summary>
    [StringLength(36)]
    [Column("customer_id")]
    public string? CustomerId { get; set; }

    /// <summary>业务员用户 ID 快照（与 <c>sellorder.sales_user_id</c> 口径一致）。</summary>
    [StringLength(36)]
    [Column("sales_user_id")]
    public string? SalesUserId { get; set; }

    /// <summary>销售订单明细业务编号快照（与 <c>sellorderitem.sell_order_item_code</c> 一致）。</summary>
    [StringLength(64)]
    [Column("sell_order_item_code")]
    public string? SellOrderItemCode { get; set; }

    /// <summary>销售订单明细主键（<c>sellorderitem.SellOrderItemId</c>）。</summary>
    [StringLength(36)]
    [Column("sell_order_item_id")]
    public string? SellOrderItemId { get; set; }

    [StringLength(32)]
    public string? HsCode { get; set; }

    public int DeclareQty { get; set; }

    [Column(TypeName = "numeric(18,6)")]
    public decimal DeclareUnitPrice { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal DutyAmount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal CustomsPaymentGoods { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal CustomsAgencyFee { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal OtherFee { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal InspectionFee { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalValueTax { get; set; }

    [Column(TypeName = "numeric(18,6)")]
    public decimal TaxIncludedUnitPrice { get; set; }

    [ForeignKey(nameof(DeclarationId))]
    public virtual CustomsDeclaration? Declaration { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

[Table("stocktransfer_customers")]
public class StockTransfer : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("StockTransferId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(32)]
    public string TransferCode { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string BizScene { get; set; } = StockTransferBizScene.CustomsImport;

    [Required]
    [StringLength(36)]
    public string CustomsDeclarationId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string FromWarehouseId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string ToWarehouseId { get; set; } = string.Empty;

    public short Status { get; set; } = StockTransferStatus.Confirmed;

    public DateTime? ConfirmedTime { get; set; }

    [StringLength(36)]
    public string? ConfirmedByUserId { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    public virtual CustomsDeclaration? Declaration { get; set; }

    public virtual ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

[Table("stocktransfer_item_customers")]
public class StockTransferItem : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("StockTransferItemId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string StockTransferId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string SourceStockItemId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string CustomsDeclarationItemId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string StockOutRequestId { get; set; } = string.Empty;

    public int Qty { get; set; }

    [StringLength(36)]
    public string? TargetStockItemId { get; set; }

    [ForeignKey(nameof(StockTransferId))]
    public virtual StockTransfer? StockTransfer { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
