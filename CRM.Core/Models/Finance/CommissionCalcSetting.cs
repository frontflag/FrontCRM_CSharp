using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance;

[Table("commission_calc_setting")]
public class CommissionCalcSetting : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("receipt_writeoff_delay_days")]
    public int ReceiptWriteoffDelayDays { get; set; }

    [Column("updated_by")]
    [StringLength(36)]
    public string? UpdatedBy { get; set; }
}
