using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Tag
{
    /// <summary>
    /// 标签定义表
    /// </summary>
    [Table("tag_definition")]
    public class TagDefinition : BaseGuidEntity
    {
        /// <summary>
        /// 标签主键
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("TagId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 标签名称
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 标签编码（系统标签唯一，用户标签可空或自动生成）
        /// </summary>
        [StringLength(50)]
        public string? Code { get; set; }

        /// <summary>
        /// 颜色（如 #FF6B6B）
        /// </summary>
        [StringLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// 图标标识
        /// </summary>
        [StringLength(100)]
        public string? Icon { get; set; }

        /// <summary>
        /// 标签类型：1=系统标签，2=用户自定义
        /// </summary>
        public short Type { get; set; } = 2;

        /// <summary>
        /// 分类（如 优先级/客户类型/跟进状态）
        /// </summary>
        [StringLength(50)]
        public string? Category { get; set; }

        /// <summary>
        /// 适用范围（实体编码集合，逗号分隔：CUSTOMER,VENDOR,...）
        /// </summary>
        [StringLength(200)]
        public string? Scope { get; set; }

        /// <summary>
        /// 状态：1=启用，0=停用
        /// </summary>
        public short Status { get; set; } = 1;

        /// <summary>
        /// 排序权重
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 使用次数
        /// </summary>
        public long UsageCount { get; set; }

        /// <summary>
        /// 创建者（系统标签为 null）
        /// </summary>
        public long? OwnerUserId { get; set; }

        /// <summary>
        /// 可见范围：1=仅自己，2=团队共享，3=全局公开
        /// </summary>
        public short Visibility { get; set; } = 3;
    }
}
