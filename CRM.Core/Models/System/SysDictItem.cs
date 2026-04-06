using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// 数据字典项（下拉选项等；与 vendorinfo 等存库编码通过 ItemCode 对齐）
    /// </summary>
    [Table("sys_dict_item")]
    public class SysDictItem : BaseGuidEntity
    {
        [Key]
        [MaxLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(64)]
        [Column("Category")]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        [Column("ItemCode")]
        public string ItemCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("NameZh")]
        public string NameZh { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("NameEn")]
        public string? NameEn { get; set; }

        [Column("SortOrder")]
        public int SortOrder { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
