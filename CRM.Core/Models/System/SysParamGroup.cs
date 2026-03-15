using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// 系统参数分组表
    /// 支持多级分组管理
    /// </summary>
    [Table("sysparamgroup")]
    public class SysParamGroup : BaseGuidEntity
    {
        /// <summary>
        /// 分组编码
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("GroupCode")]
        public string GroupCode { get; set; } = string.Empty;

        /// <summary>
        /// 分组名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("GroupName")]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 父分组ID
        /// </summary>
        [Column("ParentId")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 分组层级
        /// 枚举：1=一级, 2=二级, 3=三级
        /// </summary>
        [Column("Level")]
        public short Level { get; set; } = 1;

        /// <summary>
        /// 排序序号
        /// </summary>
        [Column("SortOrder")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 分组描述
        /// </summary>
        [MaxLength(200)]
        [Column("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// 状态
        /// 枚举：0=禁用, 1=启用
        /// </summary>
        [Column("Status")]
        public short Status { get; set; } = 1;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        [Column("ModifyTime")]
        public DateTime? ModifyTime { get; set; }

        // 导航属性
        /// <summary>
        /// 父分组
        /// </summary>
        [ForeignKey("ParentId")]
        public virtual SysParamGroup? Parent { get; set; }

        /// <summary>
        /// 子分组列表
        /// </summary>
        public virtual ICollection<SysParamGroup> Children { get; set; } = new List<SysParamGroup>();

        /// <summary>
        /// 参数列表
        /// </summary>
        public virtual ICollection<SysParam> Params { get; set; } = new List<SysParam>();
    }
}
