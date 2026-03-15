using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models
{
    /// <summary>
    /// 用户实体类
    /// </summary>
    [Table("user")]
    public class User : BaseGuidEntity
    {
        /// <summary>
        /// 登录账号
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("UserName")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 电子邮箱
        /// </summary>
        [MaxLength(100)]
        [Column("Email")]
        public string? Email { get; set; }

        /// <summary>
        /// 密码哈希值
        /// </summary>
        [Required]
        [MaxLength(256)]
        [Column("Password")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 密码加密盐值
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("Salt")]
        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// 密码明文（测试用途）
        /// 仅用于开发测试阶段记录明文密码，不参与业务逻辑
        /// TODO: 生产环境发布前删除此字段
        /// </summary>
        [MaxLength(100)]
        [Column("PasswordPlain")]
        public string? PasswordPlain { get; set; }

        /// <summary>
        /// 账号是否启用
        /// </summary>
        [Column("Status")]
        public short Status { get; set; } = 1;

        /// <summary>
        /// 账号是否启用（便捷属性）
        /// </summary>
        [NotMapped]
        public bool IsActive
        {
            get => Status == 1;
            set => Status = value ? (short)1 : (short)0;
        }

        /// <summary>
        /// 密码最后修改时间
        /// </summary>
        [Column("PasswordChangeTime")]
        public DateTime? PasswordChangeTime { get; set; }

        /// <summary>
        /// 真实姓名
        /// </summary>
        [MaxLength(50)]
        [Column("RealName")]
        public string? RealName { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        [MaxLength(20)]
        [Column("Mobile")]
        public string? Mobile { get; set; }
    }
}
