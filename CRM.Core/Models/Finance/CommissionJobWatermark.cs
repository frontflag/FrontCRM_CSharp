using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;

namespace CRM.Core.Models.Finance;

[Table("commission_job_watermark")]
public class CommissionJobWatermark : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = CommissionJobWatermarkCode.SeedId;

    [Column("dynamic_job_date")]
    public DateOnly? DynamicJobDate { get; set; }

    [Column("lock_term")]
    [StringLength(6)]
    public string? LockTerm { get; set; }
}
