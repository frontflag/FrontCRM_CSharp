using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Biz;

/// <summary>品牌导入学习映射（全公司共享，原文 → brand_id）。</summary>
[Table("biz_brand_learned_mapping")]
public class BizBrandLearnedMapping
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("source_text")]
    [MaxLength(500)]
    public string SourceText { get; set; } = string.Empty;

    [Column("source_key")]
    [MaxLength(500)]
    public string SourceKey { get; set; } = string.Empty;

    [Column("brand_id")]
    public long BrandId { get; set; }

    [Column("hit_count")]
    public int HitCount { get; set; } = 1;

    [Column("last_used_by_user_id")]
    [MaxLength(36)]
    public string? LastUsedByUserId { get; set; }

    [Column("create_by_user_id")]
    [MaxLength(36)]
    public string? CreateByUserId { get; set; }

    [Column("create_time")]
    public DateTime CreateTime { get; set; }

    [Column("update_time")]
    public DateTime UpdateTime { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
