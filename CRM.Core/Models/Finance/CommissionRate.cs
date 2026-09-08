using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Utilities;

namespace CRM.Core.Models.Finance;

[Table("commission_rate")]
public class CommissionRate : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("role_type")]
    public short RoleType { get; set; }

    [Column("user_level")]
    public short UserLevel { get; set; }

    [Column("threshold_1", TypeName = "numeric(18,2)")]
    public decimal? Threshold1 { get; set; }
    [Column("threshold_2", TypeName = "numeric(18,2)")]
    public decimal? Threshold2 { get; set; }
    [Column("threshold_3", TypeName = "numeric(18,2)")]
    public decimal? Threshold3 { get; set; }
    [Column("threshold_4", TypeName = "numeric(18,2)")]
    public decimal? Threshold4 { get; set; }
    [Column("threshold_5", TypeName = "numeric(18,2)")]
    public decimal? Threshold5 { get; set; }
    [Column("threshold_6", TypeName = "numeric(18,2)")]
    public decimal? Threshold6 { get; set; }
    [Column("threshold_7", TypeName = "numeric(18,2)")]
    public decimal? Threshold7 { get; set; }
    [Column("threshold_8", TypeName = "numeric(18,2)")]
    public decimal? Threshold8 { get; set; }
    [Column("threshold_9", TypeName = "numeric(18,2)")]
    public decimal? Threshold9 { get; set; }
    [Column("threshold_10", TypeName = "numeric(18,2)")]
    public decimal? Threshold10 { get; set; }

    [Column("rate_points_1", TypeName = "numeric(5,2)")]
    public decimal? RatePoints1 { get; set; }
    [Column("rate_points_2", TypeName = "numeric(5,2)")]
    public decimal? RatePoints2 { get; set; }
    [Column("rate_points_3", TypeName = "numeric(5,2)")]
    public decimal? RatePoints3 { get; set; }
    [Column("rate_points_4", TypeName = "numeric(5,2)")]
    public decimal? RatePoints4 { get; set; }
    [Column("rate_points_5", TypeName = "numeric(5,2)")]
    public decimal? RatePoints5 { get; set; }
    [Column("rate_points_6", TypeName = "numeric(5,2)")]
    public decimal? RatePoints6 { get; set; }
    [Column("rate_points_7", TypeName = "numeric(5,2)")]
    public decimal? RatePoints7 { get; set; }
    [Column("rate_points_8", TypeName = "numeric(5,2)")]
    public decimal? RatePoints8 { get; set; }
    [Column("rate_points_9", TypeName = "numeric(5,2)")]
    public decimal? RatePoints9 { get; set; }
    [Column("rate_points_10", TypeName = "numeric(5,2)")]
    public decimal? RatePoints10 { get; set; }

    [Column("remark")]
    [StringLength(200)]
    public string? Remark { get; set; }

    [Column("created_by")]
    [StringLength(36)]
    public string? CreatedBy { get; set; }

    [Column("updated_by")]
    [StringLength(36)]
    public string? UpdatedBy { get; set; }

    public CommissionLadderSlot[] GetSlots() =>
    [
        new(Threshold1, RatePoints1),
        new(Threshold2, RatePoints2),
        new(Threshold3, RatePoints3),
        new(Threshold4, RatePoints4),
        new(Threshold5, RatePoints5),
        new(Threshold6, RatePoints6),
        new(Threshold7, RatePoints7),
        new(Threshold8, RatePoints8),
        new(Threshold9, RatePoints9),
        new(Threshold10, RatePoints10)
    ];

    public void SetSlots(IReadOnlyList<CommissionLadderSlot> slots)
    {
        var n = CommissionLadderRules.Normalize(slots);
        Threshold1 = n[0].Threshold; RatePoints1 = n[0].RatePoints;
        Threshold2 = n[1].Threshold; RatePoints2 = n[1].RatePoints;
        Threshold3 = n[2].Threshold; RatePoints3 = n[2].RatePoints;
        Threshold4 = n[3].Threshold; RatePoints4 = n[3].RatePoints;
        Threshold5 = n[4].Threshold; RatePoints5 = n[4].RatePoints;
        Threshold6 = n[5].Threshold; RatePoints6 = n[5].RatePoints;
        Threshold7 = n[6].Threshold; RatePoints7 = n[6].RatePoints;
        Threshold8 = n[7].Threshold; RatePoints8 = n[7].RatePoints;
        Threshold9 = n[8].Threshold; RatePoints9 = n[8].RatePoints;
        Threshold10 = n[9].Threshold; RatePoints10 = n[9].RatePoints;
    }
}
