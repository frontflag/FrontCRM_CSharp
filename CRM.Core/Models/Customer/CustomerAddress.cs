using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Customer
{
    /// <summary>
    /// 客户地址表
    /// </summary>
    [Table("customeraddress")]
    public class CustomerAddress : BaseGuidEntity, ISoftDeletable
    {
        /// <summary>
        /// 地址ID (主键)
        /// </summary>
        [StringLength(36)]
        [Column("AddressId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 客户ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// 地址类型 (1:收货地址 2:账单地址)
        /// </summary>
        public short AddressType { get; set; } = 1;

        /// <summary>
        /// 国家/地区代码：1=中国（含大陆/港/台），2=海外
        /// </summary>
        public short? Country { get; set; }

        /// <summary>
        /// 国家/地区名称（如 中国、United States）
        /// </summary>
        [StringLength(100)]
        public string? CountryName { get; set; }

        /// <summary>
        /// 省份
        /// </summary>
        [StringLength(50)]
        public string? Province { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        [StringLength(50)]
        public string? City { get; set; }

        /// <summary>
        /// 区域
        /// </summary>
        [StringLength(50)]
        public string? Area { get; set; }

        /// <summary>
        /// 详细地址
        /// </summary>
        [StringLength(200)]
        public string? Address { get; set; }

        /// <summary>
        /// 地址公司名称
        /// </summary>
        [StringLength(200)]
        public string? CompanyName { get; set; }

        /// <summary>
        /// 联系人
        /// </summary>
        [StringLength(50)]
        public string? ContactName { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [StringLength(20)]
        public string? ContactPhone { get; set; }

        /// <summary>
        /// 邮政编码
        /// </summary>
        [StringLength(20)]
        public string? ZipCode { get; set; }

        /// <summary>
        /// 是否默认地址
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("CustomerId")]
        public virtual CustomerInfo? Customer { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}
