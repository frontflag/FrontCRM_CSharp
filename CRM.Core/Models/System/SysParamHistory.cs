using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// 系统参数历史记录表
    /// 记录参数修改历史
    /// </summary>
    [Table("sysparamhistory")]
    public class SysParamHistory : BaseGuidEntity
    {
        /// <summary>
        /// 参数ID（与 <see cref="SysParam.Id"/> / sysparam.ParamId 一致，varchar(36)）
        /// </summary>
        [Required]
        [MaxLength(36)]
        [Column("ParamId")]
        public string ParamId { get; set; } = string.Empty;

        /// <summary>
        /// 修改前值
        /// </summary>
        [MaxLength(500)]
        [Column("OldValue")]
        public string? OldValue { get; set; }

        /// <summary>
        /// 修改后值
        /// </summary>
        [MaxLength(500)]
        [Column("NewValue")]
        public string? NewValue { get; set; }

        /// <summary>
        /// 修改原因
        /// </summary>
        [MaxLength(200)]
        [Column("ChangeReason")]
        public string? ChangeReason { get; set; }

        /// <summary>
        /// 操作人ID
        /// </summary>
        [Column("OperatorId")]
        public Guid? OperatorId { get; set; }

        /// <summary>
        /// 操作人姓名
        /// </summary>
        [MaxLength(50)]
        [Column("OperatorName")]
        public string? OperatorName { get; set; }

        // 导航属性
        /// <summary>
        /// 关联参数
        /// </summary>
        [ForeignKey("ParamId")]
        public virtual SysParam Param { get; set; } = null!;
    }
}
