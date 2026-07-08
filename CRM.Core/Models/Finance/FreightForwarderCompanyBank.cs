using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

[Table("freight_forwarder_company_bank")]
public class FreightForwarderCompanyBank : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("Id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string FreightForwarderCompanyId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string BankName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? AccountName { get; set; }

    [StringLength(64)]
    public string? AccountNo { get; set; }

    public byte Currency { get; set; } = 1;

    public bool IsDefault { get; set; }

    public bool IsDisabled { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }
}
