using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.Customer
{
    /// <summary>
    /// 客户联系人表
    /// </summary>
    [Table("customercontactinfo")]
    public class CustomerContactInfo : BaseGuidEntity
    {
        /// <summary>
        /// 联系人ID (主键)
        /// </summary>
        [StringLength(36)]
        [Column("ContactId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 客户ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// 姓名（优先使用中文名）
        /// </summary>
        [NotMapped]
        public string? Name
        {
            get => !string.IsNullOrEmpty(CName) ? CName : EName;
            set => CName = value;
        }

        /// <summary>
        /// 联系人姓名（Name的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? ContactName
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// 中文名
        /// </summary>
        [StringLength(50)]
        [Column("CName")]
        public string? CName { get; set; }

        /// <summary>
        /// 英文名
        /// </summary>
        [StringLength(100)]
        [Column("EName")]
        public string? EName { get; set; }

        /// <summary>
        /// 性别：0=保密、1=男、2=女
        /// </summary>
        public short? Gender { get; set; }

        /// <summary>
        /// 职位
        /// </summary>
        [StringLength(50)]
        [Column("Title")]
        public string? Title { get; set; }

        /// <summary>
        /// 职位（别名）
        /// </summary>
        [NotMapped]
        public string? Position
        {
            get => Title;
            set => Title = value;
        }

        /// <summary>
        /// 部门
        /// </summary>
        [StringLength(50)]
        public string? Department { get; set; }

        /// <summary>
        /// 手机
        /// </summary>
        [StringLength(20)]
        public string? Mobile { get; set; }

        /// <summary>
        /// 手机号（Mobile的别名，用于前端兼容）
        /// </summary>
        [NotMapped]
        public string? MobilePhone
        {
            get => Mobile;
            set => Mobile = value;
        }

        /// <summary>
        /// 电话
        /// </summary>
        [StringLength(30)]
        [Column("Tel")]
        public string? Tel { get; set; }

        /// <summary>
        /// 电话（别名）
        /// </summary>
        [NotMapped]
        public string? Phone
        {
            get => Tel;
            set => Tel = value;
        }

        /// <summary>
        /// 传真
        /// </summary>
        [StringLength(30)]
        [NotMapped]
        public string? Fax { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [StringLength(100)]
        public string? Email { get; set; }

        /// <summary>
        /// QQ
        /// </summary>
        [StringLength(20)]
        public string? QQ { get; set; }

        /// <summary>
        /// 微信
        /// </summary>
        [StringLength(50)]
        public string? WeChat { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [StringLength(200)]
        public string? Address { get; set; }

        /// <summary>
        /// 是否主联系人
        /// </summary>
        [Column("IsMain")]
        public bool IsMain { get; set; } = false;

        /// <summary>
        /// 是否默认联系人（别名）
        /// </summary>
        [NotMapped]
        public bool IsDefault
        {
            get => IsMain;
            set => IsMain = value;
        }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("CustomerId")]
        public virtual CustomerInfo? Customer { get; set; }
    }
}
