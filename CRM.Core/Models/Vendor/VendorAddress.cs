using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Vendor
{
    /// <summary>
    /// 供应商地址表
    /// </summary>
    [Table("vendoraddress")]
    public class VendorAddress : BaseGuidEntity
    {
        /// <summary>
        /// 地址ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("AddressId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 供应商ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string VendorId { get; set; } = string.Empty;

        /// <summary>
        /// 地址类型 (1:收货地址 2:账单地址)
        /// </summary>
        public short AddressType { get; set; } = 1;

        /// <summary>
        /// 国家
        /// </summary>
        public short? Country { get; set; }

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
        /// 是否默认地址
        /// </summary>
        public bool IsDefault { get; set; } = false;

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
