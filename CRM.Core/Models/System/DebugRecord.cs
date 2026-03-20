using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.System
{
    /// <summary>
    /// Debug 表：用于在不依赖认证登录的情况下，查看服务端关键调试数据。
    /// </summary>
    [Table("debug")]
    public class DebugRecord
    {
        /// <summary>
        /// Name（主键）
        /// </summary>
        [Key]
        [Column("Name")]
        [Required]
        [StringLength(32)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Value
        /// </summary>
        [Column("Value")]
        [Required]
        [StringLength(128)]
        public string Value { get; set; } = string.Empty;
    }
}

