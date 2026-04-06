using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System;

/// <summary>
/// 订单旅程日志，表 <c>log_orderjourney</c>。与 <see cref="Models.BaseEntity"/> 兼容以使用 <see cref="Interfaces.IRepository{T}"/>。
/// </summary>
[Table("log_orderjourney")]
public class OrderJourneyLog : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, StringLength(32)]
    public string EntityKind { get; set; } = string.Empty;

    [Required, StringLength(36)]
    public string EntityId { get; set; } = string.Empty;

    [StringLength(32)]
    public string? ParentEntityKind { get; set; }

    [StringLength(36)]
    public string? ParentEntityId { get; set; }

    [StringLength(64)]
    public string? DocumentCode { get; set; }

    [StringLength(200)]
    public string? LineHint { get; set; }

    [Required, StringLength(64)]
    public string EventCode { get; set; } = string.Empty;

    [StringLength(200)]
    public string? EventLabel { get; set; }

    [StringLength(32)]
    public string? FromState { get; set; }

    [StringLength(32)]
    public string? ToState { get; set; }

    [Column("EventTime")]
    public DateTime EventTime { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "numeric(18,4)")]
    public decimal? Quantity { get; set; }

    [Column(TypeName = "numeric(18,6)")]
    public decimal? Amount { get; set; }

    public short? Currency { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public string? PayloadJson { get; set; }

    [StringLength(32)]
    public string? RelatedEntityKind { get; set; }

    [StringLength(36)]
    public string? RelatedEntityId { get; set; }

    [StringLength(16)]
    public string? ActorKind { get; set; }

    [StringLength(36)]
    public string? ActorUserId { get; set; }

    [StringLength(100)]
    public string? ActorUserName { get; set; }

    [StringLength(36)]
    public string? ActorVendorId { get; set; }

    [StringLength(64)]
    public string? Source { get; set; }
}
