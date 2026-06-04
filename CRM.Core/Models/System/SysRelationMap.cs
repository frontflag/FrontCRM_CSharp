using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.System;

/// <summary>
/// 对象关系配置（表 <c>sys_relation_map</c>）：由 <see cref="Type"/> 定义关系语义，
/// <c>obj_src</c> 一侧配对一组 <c>obj_dest</c>（人员、业务或其它对象主键/编码）。
/// </summary>
[Table("sys_relation_map")]
public class SysRelationMap : ISoftDeletable
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>关系类型，见 <see cref="Constants.SysRelationMapTypeCode"/>。</summary>
    [Column("type")]
    public short Type { get; set; }

    /// <summary>源对象标识（如助理用户 Id、采购员用户 Id）。</summary>
    [Required]
    [StringLength(64)]
    [Column("obj_src")]
    public string ObjSrc { get; set; } = string.Empty;

    /// <summary>目标对象标识（如被负责的销售员 Id、可报价的销售员 Id）。</summary>
    [Required]
    [StringLength(64)]
    [Column("obj_dest")]
    public string ObjDest { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("remark")]
    public string? Remark { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
