using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Tag
{
    /// <summary>
    /// 用户标签偏好表
    /// </summary>
    [Table("user_tag_preference")]
    public class UserTagPreference : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Key, Column(Order = 0)]
        public long UserId { get; set; }

        /// <summary>
        /// 标签ID
        /// </summary>
        [Key, Column(Order = 1)]
        [StringLength(36)]
        public string TagId { get; set; } = string.Empty;

        /// <summary>
        /// 使用次数
        /// </summary>
        public long UseCount { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedTime { get; set; }

        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool IsFavorite { get; set; }
    }
}
