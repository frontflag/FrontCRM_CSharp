using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.RFQ;

/// <summary>需求关闭记录（rfq_close_record）</summary>
[Table("rfq_close_record")]
public class RfqCloseRecord : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("rfq_close_record_id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("rfq_id")]
    public string RfqId { get; set; } = string.Empty;

    /// <summary>1 正常关闭 2 客户取消 3 价格不符 9 其他</summary>
    [Column("close_type")]
    public short CloseType { get; set; }

    [Required]
    [StringLength(500)]
    [Column("close_reason")]
    public string CloseReason { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("remark")]
    public string? Remark { get; set; }

    [StringLength(36)]
    [Column("closed_by_user_id")]
    public string? ClosedByUserId { get; set; }

    [NotMapped]
    public string? ClosedByName { get; set; }
}
