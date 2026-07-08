using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

[Table("freight_forwarder_company")]
public class FreightForwarderCompany : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(32)]
    public string CompanyCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Column("cname")]
    public string Cname { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("ename")]
    public string? Ename { get; set; }

    public short Status { get; set; } = 1;

    [StringLength(500)]
    public string? Remark { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
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

    [NotMapped]
    public IReadOnlyList<FreightForwarderCompanyBank> Banks { get; set; } = Array.Empty<FreightForwarderCompanyBank>();
}
