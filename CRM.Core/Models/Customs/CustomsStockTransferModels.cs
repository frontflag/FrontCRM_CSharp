using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;
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

/// <summary>报关主单；与出库通知、移库单 1:1:1。</summary>
[Table("customs_declaration")]
public class CustomsDeclaration : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("CustomsDeclarationId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(32)]
    public string DeclarationCode { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string StockOutRequestId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string CustomsBrokerId { get; set; } = string.Empty;

    public short DeclarationType { get; set; } = CustomsDeclarationType.Import;

    public short InternalStatus { get; set; } = CustomsDeclarationInternalStatus.Pending;

    public short CustomsClearanceStatus { get; set; } = CustomsClearanceStatusCodes.None;

    public DateTime DeclareDate { get; set; } = DateTime.UtcNow.Date;

    [Column(TypeName = "numeric(18,6)")]
    public decimal ExchangeRate { get; set; } = 1m;

    [Column(TypeName = "numeric(18,2)")]
    public decimal TotalTaxAmount { get; set; }

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

    public virtual StockTransfer? StockTransfer { get; set; }

    public virtual ICollection<CustomsDeclarationItem> Items { get; set; } = new List<CustomsDeclarationItem>();
}

[Table("customs_declaration_item")]
public class CustomsDeclarationItem : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("CustomsDeclarationItemId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string DeclarationId { get; set; } = string.Empty;

    public int LineNo { get; set; }

    [Required]
    [StringLength(36)]
    public string SourceStockItemId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string StockOutRequestId { get; set; } = string.Empty;

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
}

[Table("stocktransfer")]
public class StockTransfer : BaseGuidEntity
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
}

[Table("stocktransferitem")]
public class StockTransferItem : BaseGuidEntity
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
}
