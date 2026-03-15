using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models
{
    /// <summary>
    /// 基础实体类
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建人ID
        /// </summary>
        public long? CreateUserId { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime? ModifyTime { get; set; }

        /// <summary>
        /// 修改人ID
        /// </summary>
        public long? ModifyUserId { get; set; }
    }

    /// <summary>
    /// GUID 主键基础实体
    /// </summary>
    public abstract class BaseGuidEntity : BaseEntity
    {
        /// <summary>
        /// 主键ID (GUID)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("UserId")]
        public virtual string Id { get; set; } = Guid.NewGuid().ToString();
    }
}
