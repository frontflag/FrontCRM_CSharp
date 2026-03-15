using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Vendor
{
    /// <summary>
    /// 供应商联系人表
    /// </summary>
    [Table("vendorcontactinfo")]
    public class VendorContactInfo : BaseGuidEntity
    {
        /// <summary>
        /// 联系人ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ContactId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 供应商ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>
        /// 中文名
        /// </summary>
        [StringLength(50)]
        public string? CName { get; set; }

        /// <summary>
        /// 英文名
        /// </summary>
        [StringLength(100)]
        public string? EName { get; set; }

        /// <summary>
        /// 职位
        /// </summary>
        [StringLength(50)]
        public string? Title { get; set; }

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
        /// 电话
        /// </summary>
        [StringLength(30)]
        public string? Tel { get; set; }

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
        public bool IsMain { get; set; } = false;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("VendorId")]
        public virtual VendorInfo? Vendor { get; set; }
    }
}
