using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Inventory;

/// <summary>装箱单主表。</summary>
[Table("packing")]
public class Packing : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>装箱单编号（流水前缀 Pak）。</summary>
    [Required]
    [StringLength(32)]
    [Column("Code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>业务状态，见 <see cref="PackingStatusCode"/>。</summary>
    public short Status { get; set; } = PackingStatusCode.New;

    /// <summary>出库类型，见 <see cref="StockOutTypeCode"/>。</summary>
    [Column("StockOutType")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public short StockOutType { get; set; } = PackingStockOutTypeCode.Sales;

    /// <summary>物料类型，见 <see cref="PackingMaterialTypeCode"/>。</summary>
    [Column("MaterialType")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public short MaterialType { get; set; } = PackingMaterialTypeCode.Normal;

    [StringLength(36)]
    [Column("customer_id")]
    public string? CustomerId { get; set; }

    [StringLength(36)]
    [Column("sales_id")]
    public string? SalesId { get; set; }

    [Column("schedule_ship_date")]
    public DateTime? ScheduleShipDate { get; set; }

    /// <summary>出库仓库主键（<c>warehouseinfo.Id</c>）。</summary>
    [StringLength(36)]
    [Column("storage_id")]
    public string? StorageId { get; set; }

    [Column("item_rows")]
    public int ItemRows { get; set; }

    [StringLength(500)]
    [Column("comment")]
    public string? Comment { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    public virtual PackingExtend? Extend { get; set; }

    public virtual PackingExtendBox? ExtendBox { get; set; }

    public virtual PackingExtendShip? ExtendShip { get; set; }

    public virtual ICollection<PackingItem> Items { get; set; } = new List<PackingItem>();
}

/// <summary>装箱单主单级扩展（1:1 packing），维护明细行序号水位。</summary>
[Table("packing_extend")]
public class PackingExtend
{
    [Key]
    [StringLength(36)]
    [Column("PackingId")]
    public string PackingId { get; set; } = string.Empty;

    /// <summary>装箱明细行序号水位（删除行不回收序号）。</summary>
    [Column("last_item_line_seq")]
    public int LastItemLineSeq { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("ModifyTime")]
    public DateTime? ModifyTime { get; set; }

    public virtual Packing? Packing { get; set; }
}

/// <summary>装箱单箱规扩展表（净重/毛重/尺寸/箱数）。</summary>
[Table("packing_extend_box")]
public class PackingExtendBox
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("PackingId")]
    public string PackingId { get; set; } = string.Empty;

    [Column("NW", TypeName = "numeric(18,4)")]
    public decimal? Nw { get; set; }

    [Column("GW", TypeName = "numeric(18,4)")]
    public decimal? Gw { get; set; }

    [StringLength(200)]
    [Column("DIM")]
    public string? Dim { get; set; }

    [Column("CTNS")]
    public int? Ctns { get; set; }

    public virtual Packing? Packing { get; set; }
}

/// <summary>装箱单收发货地址扩展表。</summary>
[Table("packing_extend_ship")]
public class PackingExtendShip
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("PackingId")]
    public string PackingId { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("ship_company")]
    public string? ShipCompany { get; set; }

    [StringLength(256)]
    [Column("ship_address")]
    public string? ShipAddress { get; set; }

    [StringLength(100)]
    [Column("ship_attn")]
    public string? ShipAttn { get; set; }

    [StringLength(64)]
    [Column("ship_tel")]
    public string? ShipTel { get; set; }

    [StringLength(200)]
    [Column("bill_company")]
    public string? BillCompany { get; set; }

    [StringLength(256)]
    [Column("bill_address")]
    public string? BillAddress { get; set; }

    [StringLength(100)]
    [Column("bill_attn")]
    public string? BillAttn { get; set; }

    [StringLength(64)]
    [Column("bill_tel")]
    public string? BillTel { get; set; }

    [StringLength(256)]
    [Column("delivery_req")]
    public string? DeliveryReq { get; set; }

    /// <summary>送货方式，见 <see cref="PackingDeliveryMethodCode"/>。</summary>
    [Column("delivery_method")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public short? DeliveryMethod { get; set; }

    public virtual Packing? Packing { get; set; }
}

/// <summary>装箱单明细表。</summary>
[Table("packing_item")]
public class PackingItem : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("PackingId")]
    public string PackingId { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("sell_order_id")]
    public string? SellOrderId { get; set; }

    [StringLength(36)]
    [Column("sell_order_item_id")]
    public string? SellOrderItemId { get; set; }

    /// <summary>出库通知主键（<c>stockout_notify.ID</c>）。</summary>
    [StringLength(36)]
    [Column("stockout_notify_id")]
    public string? StockOutNotifyId { get; set; }

    /// <summary>装箱明细业务编号（如 装箱单号-行序），同单内唯一。</summary>
    [StringLength(64)]
    [Column("item_code")]
    public string? ItemCode { get; set; }

    [StringLength(36)]
    [Column("product_id")]
    public string? ProductId { get; set; }

    [StringLength(36)]
    [Column("stock_item_id")]
    public string? StockItemId { get; set; }

    [StringLength(200)]
    [Column("PN")]
    public string? Pn { get; set; }

    [StringLength(200)]
    [Column("Brand")]
    public string? Brand { get; set; }

    public int Qty { get; set; }

    [StringLength(20)]
    [Column("Unit")]
    public string? Unit { get; set; }

    [StringLength(64)]
    [Column("CO")]
    public string? Co { get; set; }

    [StringLength(500)]
    [Column("comment")]
    public string? Comment { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    public virtual PackingItemExtend? Extend { get; set; }

    [ForeignKey(nameof(PackingId))]
    public virtual Packing? Packing { get; set; }
}

/// <summary>装箱单明细扩展表（客户/业务员/销售价快照）。</summary>
[Table("packing_item_extend")]
public class PackingItemExtend
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("PackingItemId")]
    public string PackingItemId { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("customer_id")]
    public string? CustomerId { get; set; }

    [StringLength(36)]
    [Column("sales_id")]
    public string? SalesId { get; set; }

    [StringLength(36)]
    [Column("sell_order_id")]
    public string? SellOrderId { get; set; }

    [StringLength(36)]
    [Column("sell_order_item_id")]
    public string? SellOrderItemId { get; set; }

    [Column("Price", TypeName = "numeric(18,6)")]
    public decimal? Price { get; set; }

    [Column("PriceCurrency")]
    public short? PriceCurrency { get; set; }

    [Column("PriceConvertPrice", TypeName = "numeric(18,6)")]
    public decimal? PriceConvertPrice { get; set; }

    [StringLength(200)]
    [Column("customer_so")]
    public string? CustomerSo { get; set; }

    [StringLength(200)]
    [Column("customer_pn")]
    public string? CustomerPn { get; set; }

    [StringLength(200)]
    [Column("customer_brand")]
    public string? CustomerBrand { get; set; }

    public virtual PackingItem? PackingItem { get; set; }
}
