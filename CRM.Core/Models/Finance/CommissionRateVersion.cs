using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;

namespace CRM.Core.Models.Finance;

[Table("commission_rate_version")]
public class CommissionRateVersion : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("role_type")]
    public short RoleType { get; set; }

    [Column("version_no")]
    public int VersionNo { get; set; }

    [Column("remark")]
    [StringLength(200)]
    public string? Remark { get; set; }

    [Column("status")]
    public short Status { get; set; } = CommissionRateVersionStatus.Draft;

    [Column("created_by")]
    [StringLength(36)]
    public string? CreatedBy { get; set; }

    [Column("updated_by")]
    [StringLength(36)]
    public string? UpdatedBy { get; set; }

    public bool IsActive => Status == CommissionRateVersionStatus.Active;
}
