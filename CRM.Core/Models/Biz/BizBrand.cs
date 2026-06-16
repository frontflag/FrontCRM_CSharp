using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRM.Core.Interfaces;

namespace CRM.Core.Models.Biz;

/// <summary>品牌主数据（表 <c>biz_brand</c>）。</summary>
[Table("biz_brand")]
public class BizBrand : ISoftDeletable
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>品牌英文名。</summary>
    [StringLength(200)]
    [Column("brand_e_name")]
    public string? BrandEName { get; set; }

    /// <summary>品牌中文名。</summary>
    [StringLength(200)]
    [Column("brand_c_name")]
    public string? BrandCName { get; set; }

    /// <summary>标准品牌名（如 EN/CN 组合展示名）。</summary>
    [StringLength(300)]
    [Column("standard_brand")]
    public string? StandardBrand { get; set; }

    /// <summary>别名（可多值，逗号或换行分隔）。</summary>
    [StringLength(500)]
    [Column("alias")]
    public string? Alias { get; set; }

    /// <summary>国家/地区代码（如 CN、US）。</summary>
    [StringLength(10)]
    [Column("country_code")]
    public string? CountryCode { get; set; }

    /// <summary>国家/地区名称。</summary>
    [StringLength(100)]
    [Column("country")]
    public string? Country { get; set; }

    /// <summary>备注。</summary>
    [StringLength(500)]
    [Column("remark")]
    public string? Remark { get; set; }

    /// <summary>创建人用户 ID。</summary>
    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    /// <summary>创建日期。</summary>
    [Column("create_time")]
    public DateTime? CreateTime { get; set; }

    /// <summary>审核状态：1 待审核，2 已审核。</summary>
    [Column("audit_status")]
    public short? AuditStatus { get; set; }

    /// <summary>审核人用户 ID。</summary>
    [StringLength(36)]
    [Column("audit_by_user_id")]
    public string? AuditByUserId { get; set; }

    /// <summary>审核日期。</summary>
    [Column("audit_time")]
    public DateTime? AuditTime { get; set; }

    /// <summary>是否已删除（软删除）。</summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>删除时间。</summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>删除操作人用户 ID。</summary>
    [StringLength(36)]
    [Column("deleted_by_user_id")]
    public string? DeletedByUserId { get; set; }
}
