using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Material
{
    /// <summary>
    /// 物料主表
    /// </summary>
    [Table("material")]
    public class MaterialInfo : BaseGuidEntity
    {
        /// <summary>
        /// 物料ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("MaterialId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 物料编码
        /// </summary>
        [Required]
        [StringLength(50)]
        public string MaterialCode { get; set; } = string.Empty;

        /// <summary>
        /// 物料名称
        /// </summary>
        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        /// <summary>
        /// 规格型号
        /// </summary>
        [StringLength(100)]
        public string? MaterialModel { get; set; }

        /// <summary>
        /// 品牌ID
        /// </summary>
        [StringLength(36)]
        public string? BrandId { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        [StringLength(36)]
        public string? CategoryId { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        [StringLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// 状态 (1:启用 0:禁用)
        /// </summary>
        public short Status { get; set; } = 1;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("CategoryId")]
        public virtual MaterialCategory? Category { get; set; }
    }

    /// <summary>
    /// 物料分类表
    /// </summary>
    [Table("materialcategory")]
    public class MaterialCategory : BaseGuidEntity
    {
        /// <summary>
        /// 分类ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("CategoryId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 分类编码
        /// </summary>
        [Required]
        [StringLength(50)]
        public string CategoryCode { get; set; } = string.Empty;

        /// <summary>
        /// 分类名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 父分类ID
        /// </summary>
        [StringLength(36)]
        public string? ParentId { get; set; }

        /// <summary>
        /// 层级
        /// </summary>
        public short Level { get; set; } = 1;

        /// <summary>
        /// 排序
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 状态 (1:启用 0:禁用)
        /// </summary>
        public short Status { get; set; } = 1;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("ParentId")]
        public virtual MaterialCategory? Parent { get; set; }
        public virtual ICollection<MaterialCategory> Children { get; set; } = new List<MaterialCategory>();
        public virtual ICollection<MaterialInfo> Materials { get; set; } = new List<MaterialInfo>();
    }
}
